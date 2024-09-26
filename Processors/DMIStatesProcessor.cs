using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Models;
using DMISharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp.Processing;
using AdaptiveSpritesDMItool.Resources;
using AdaptiveSpritesDMItool.Helpers;

namespace AdaptiveSpritesDMItool.Processors
{
    internal static class DMIStatesProcessor
    {
        private static int maxIter = 0;
        private static int iter = 0;

        private static Dictionary<string, DMIFile> DMIFiles = new Dictionary<string, DMIFile>();

        private static ProgressBar progressBarProcess;
        private static Label statusMessage;

        //public static event Action<int, ConfigItem, string> ProcessFileWithConfigStarted;

        #region Initializers

        public static void InitializeNewData(List<string> filesPaths, int configsCount)
        {
            maxIter = 0;
            iter = 0;
            DMIFiles.Clear();
            foreach (var path in filesPaths)
            {
                if (!File.Exists(path))
                    continue;
                DMIFile file = new DMIFile(path);
                maxIter += file.States.Count;
                foreach (DMIState state in file.States)
                    maxIter += state.TotalFrames;
                string importPath = EnvironmentController.lastImportPath;
                if (importPath.Last() != '\\') importPath += '\\';
                string newPath = path.Replace(importPath, "");
                DMIFiles.TryAdd(newPath, file);
            }
            maxIter *= configsCount;
        }

        #endregion Initializers


        #region Processors

        public static void ProcessFilesWithConfig(ConfigItem config)
        {
            //List<Task> tasks = new List<Task>();
            Debug.WriteLine($"Process config {config.FileName} - path: {config.FilePath}");
            foreach (var path in DMIFiles.Keys)
            {
                //ProcessFileWithConfig(config, path);
                var task = StartProcess(config, path);
                //tasks.Add(task);
            }
            //await Task.WhenAll(tasks);
        }


        async static Task StartProcess(ConfigItem config, string path)
        {
            var progress = new Progress<int>(percent =>
            {
                progressBarProcess.Value = percent;
                statusMessage.Content = $"Processing file[{iter}/{maxIter}]";
                if (IsIterEnded())
                {
                    statusMessage.Content = "Completed!";
                }
            });

            //ProcessFileWithConfigStarted?.Invoke(iter, config, path);

            await Task.Run(() => ProcessFileWithConfig(progress, config, path));

            if (IsIterEnded())
            {
                progressBarProcess.Value = maxIter;
                statusMessage.Content = "Completed!";
            }
        }

        public static void ProcessFileWithConfig(IProgress<int> progress, ConfigItem config, string path)
        {
            DMIFile file = DMIFiles[path];
            int width = file.Metadata.FrameWidth;
            int height = file.Metadata.FrameHeight;
            DataPixelStorage dataPixelStorage = new DataPixelStorage(config.FilePath, width, height);

            foreach (DMIState state in file.States)
            {
                iter++;
                progress?.Report(iter);
                ProcessStateWithConfig(state, config, dataPixelStorage, path);
                //Thread.Sleep(200);
            }

            string fileExportPath = FilesSearcher.GetExportConfigPath(config.FileName, path);
            string? directoryExportPath = Path.GetDirectoryName(fileExportPath);

            if (directoryExportPath != null && 
                !Directory.Exists(directoryExportPath))
            {
                Directory.CreateDirectory(directoryExportPath);
            }

            if (StatesController.isOverrideToggle && File.Exists(fileExportPath))
            {
                Debug.WriteLine($"[{iter}/{maxIter}] SAVED {fileExportPath} - Override");
                file.Save(fileExportPath);
                return;
            }

            if (!File.Exists(fileExportPath))
            {
                Debug.WriteLine($"[{iter}/{maxIter}] SAVED {fileExportPath}");
                file.Save(fileExportPath);
                return;
            }

            DMIFile exportedDMIFile = new DMIFile(fileExportPath);
            bool isNeedSave = file.States.Count != exportedDMIFile.States.Count;
            foreach (DMIState exportedState in exportedDMIFile.States)
            {
                //if (!file.States.Any(s => s.Name == exportedState.Name))
                var existingState = file.States.FirstOrDefault(s => s.Name == exportedState.Name);
                if (existingState == null)
                {
                    file.AddState(exportedState);
                }
                else
                {
                    file.RemoveState(existingState);
                    file.AddState(exportedState);
                }
            }

            if (isNeedSave)
            {
                Debug.WriteLine($"[{iter}/{maxIter}] SAVED {fileExportPath} - Supplemented with states");
                file.SortStates();
                file.Save(fileExportPath);
            }
            Debug.WriteLine($"[{iter}/{maxIter}] NOT SAVED {fileExportPath}");
            //Thread.Sleep(200);
        }

        #endregion Processors


        #region Draw


        private static void ProcessStateWithConfig(DMIState state, ConfigItem config, DataPixelStorage dataPixelStorage, string path)
        {

            if (!StatesController.isOverrideToggle)
            {
                string exportPath = FilesSearcher.GetExportConfigPath(config.FileName, path);
                //Debug.WriteLine($"PROCESS {exportPath} \n\t--- {path}");
                if (File.Exists(exportPath) == false)
                {
                    EditStateWithConfig(state, config, dataPixelStorage);
                    return;
                }

                DMIFile exportedDMIFile = new DMIFile(exportPath);
                foreach (DMIState exportedState in exportedDMIFile.States)
                {
                    if (state.Name == exportedState.Name)
                    {
                        iter += state.TotalFrames;
                        return;
                    }
                }
            }
            EditStateWithConfig(state, config, dataPixelStorage);
        }

        private static void EditStateWithConfig(DMIState state, ConfigItem config, DataPixelStorage dataPixelStorage)
        {
            //Debug.WriteLine($"state: {state.Name}, --- {state.Frames} - {state.FrameCapacity} - {state.TotalFrames} - {state.Width} - {state.Height}");
            StateDirection[] stateDirections = StatesController.GetAllStateDirections(state.DirectionDepth);
            for (int i = 0; i < state.Frames; i++)
            {
                foreach (StateDirection direction in stateDirections)
                {
                    iter++;
                    Image<Rgba32>? img = state.GetFrame(direction, i);
                    if (img == null)
                        continue;
                    using Image<Rgba32>? imgCopy = img.Clone();
                    img.ProcessPixelRows(accessor =>
                    {
                        // Color is pixel-agnostic, but it's implicitly convertible to the Rgba32 pixel type
                        Rgba32 transparent = Color.Transparent;

                        for (int y = 0; y < accessor.Height; y++)
                        {
                            Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                            // pixelRow.Length has the same value as accessor.Width,
                            // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                            for (int x = 0; x < pixelRow.Length; x++)
                            {
                                var currentPoint = new System.Drawing.Point(x, y);
                                var storagePoint = dataPixelStorage.GetPointStorage(direction, currentPoint);
                                if (currentPoint.X == storagePoint.X && currentPoint.Y == storagePoint.Y)
                                    continue;

                                // Get a reference to the pixel at position x
                                ref Rgba32 pixel = ref pixelRow[x];

                                // Get current color
                                Rgba32 color;
                                if (storagePoint.X == -1 || storagePoint.Y == -1)
                                {
                                    color = transparent;
                                }
                                else
                                {
                                    color = imgCopy[storagePoint.X, storagePoint.Y];
                                }

                                // Set color
                                pixel = color;

                            }
                        }
                    });
                }
            }
        }

        #endregion Draw


        #region Updaters

        public static void UpdateProgressBar(ProgressBar progressBar, Label label)
        {
            progressBarProcess = progressBar;
            progressBarProcess.Maximum = maxIter;
            progressBarProcess.Value = 0;
            statusMessage = label;
        }

        #endregion Updaters


        #region Helpers

        public static bool IsIterEnded() => (iter >= maxIter - 5) || (iter == 0);

        #endregion Helpers
    }
}
