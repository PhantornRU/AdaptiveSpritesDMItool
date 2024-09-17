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

namespace AdaptiveSpritesDMItool.Processors
{
    internal static class DMIStatesProcessor
    {
        private static int maxIter = 0;
        private static int iter = 0;

        public static bool isReadyForNewProcess = true;

        private static Dictionary<string, DMIFile> DMIFiles = new Dictionary<string, DMIFile>();

        private static ProgressBar progressBarProcess;
        private static Label statusMessage;

        public static event Action<int, ConfigItem, string> ProcessFileWithConfigStarted;


        #region Initializers

        public static void InitializeNewData(List<string> filesPaths)
        {
            isReadyForNewProcess = false;
            maxIter = 0;
            iter = 0;
            DMIFiles.Clear();
            foreach (var path in filesPaths)
            {
                if (!File.Exists(path))
                    continue;
                DMIFile file = new DMIFile(path);
                maxIter += file.States.Count;
                string newPath = path.Replace(EnvironmentController.lastImportPath + "\\", "");
                DMIFiles.TryAdd(newPath, file);
            }
        }

        public static void UpdateProgressBar(ProgressBar progressBar, Label label)
        {
            progressBarProcess = progressBar;
            progressBarProcess.Maximum = maxIter;
            progressBarProcess.Value = 0;
            statusMessage = label;
        }

        #endregion Initializers


        #region Processors

        public static void ProcessFilesWithConfig(ConfigItem config)
        {
            foreach (var path in DMIFiles.Keys)
            {
                var task = StartProcess(config, path);
            }
        }

        async static Task StartProcess(ConfigItem config, string path)
        {
            var progress = new Progress<int>(percent =>
            {
                progressBarProcess.Value = percent;
                statusMessage.Content = $"Processing file[{iter}/{maxIter}]";
                if (iter >= maxIter-1)
                {
                    statusMessage.Content = "Completed!";
                    isReadyForNewProcess = true;
                }
            });

            ProcessFileWithConfigStarted?.Invoke(iter, config, path);

            await Task.Run(() => ProcessFileWithConfig(progress, config, path));
        }

        public static void ProcessFileWithConfig(IProgress<int> progress, ConfigItem config, string path)
        {
            DMIFile file = DMIFiles[path];
            string configName = config.FileName.Replace(EnvironmentController.configFormat, "");
            string filePath  = Path.Combine(EnvironmentController.lastExportPath, configName, path);
            string? directoryPath = Path.GetDirectoryName(filePath);
            Debug.WriteLine($"Processing file[{iter}/{maxIter}]: {filePath}");
            if (directoryPath == null)
                return;
            Debug.WriteLine($"Processing file[{iter}/{maxIter}]: {filePath} - START");
            foreach (DMIState state in file.States)
            {
                iter++;
                progress?.Report(iter);
                EditStateWithConfig(state, config);
                //Task.Delay(1000);
                Thread.Sleep(500);
            }
            Debug.WriteLine($"Processing file[{iter}/{maxIter}]: {filePath} - END");
            //if (!Directory.Exists(directoryPath))
            //{
            //    Directory.CreateDirectory(directoryPath);
            //}
            //if (StatesController.isOverrideToggle && File.Exists(filePath))
            //{
            //    File.Delete(filePath);
            //    file.Save(filePath);
            //}
            //if(!File.Exists(filePath))
            //    file.Save(filePath);
        }

        #endregion Processors

        #region Draw

        private static void EditStateWithConfig(DMIState state, ConfigItem config)
        {
            StateDirection[] stateDirections = StatesController.GetAllStateDirections(state.DirectionDepth);

            foreach (StateDirection direction in stateDirections)
            {
                Debug.WriteLine($"EditStateWithConfig[{iter}]: {state.Name} {direction}");
                for (int i = 0; i < state.Frames; i++)
                {
                    using Image<Rgba32>? image = state.GetFrame(direction, i);
                    if (image == null) return;

                    image.ProcessPixelRows(accessor =>
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
                                // Get a reference to the pixel at position x
                                ref Rgba32 pixel = ref pixelRow[x];
                                if (pixel.A == 0)
                                {
                                    // Overwrite the pixel referenced by 'ref Rgba32 pixel':
                                    pixel = transparent;
                                }
                            }
                        }
                    });
                }
            }
        }


        #endregion Draw
    }
}
