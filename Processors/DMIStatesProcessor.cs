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

namespace AdaptiveSpritesDMItool.Processors
{
    internal static class DMIStatesProcessor
    {
        private static int maxIter = 0;
        private static int iter = 0;

        private static Dictionary<string, DMIFile> DMIFiles = new Dictionary<string, DMIFile>();

        private static ProgressBar progressBarProcess;
        private static Label statusMessage;

        public static event Action<int, ConfigItem, string> ProcessFileWithConfigStarted;

        #region Initializers

        public static void InitializeNewData(List<string> filesPaths)
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
            //    // TODO: Improve the threads to run processing on several configs at once.
            //    //List<Task> tasks = new List<Task>();
            Debug.WriteLine($"Process config {config.FileName} - path: {config.FilePath}");
            foreach (var path in DMIFiles.Keys)
            {
                ProcessFileWithConfig(config, path);
                //var task = StartProcess(config, path);
                //tasks.Add(task);
            }
            //    //await Task.WhenAll(tasks);
        }


        //async static Task StartProcess(ConfigItem config, string path)
        //{
        //    var progress = new Progress<int>(percent =>
        //    {
        //        progressBarProcess.Value = percent;
        //        statusMessage.Content = $"Processing file[{iter}/{maxIter}] -- {percent}";
        //        if (IsIterEnded())
        //        {
        //            statusMessage.Content = "Completed!";
        //        }
        //    });

        //    ProcessFileWithConfigStarted?.Invoke(iter, config, path);

        //    await Task.Run(() => ProcessFileWithConfig(progress, config, path));
        //}

        public static void ProcessFileWithConfig(ConfigItem config, string path)
        {
            DMIFile file = DMIFiles[path];
            int width = file.Metadata.FrameWidth;
            int height = file.Metadata.FrameHeight;
            DataPixelStorage dataPixelStorage = new DataPixelStorage(config.FilePath, width, height);

            foreach (DMIState state in file.States)
            {
                //Debug.WriteLine($"state: {state.Name}, --- {state.Frames} - {state.FrameCapacity} - {state.TotalFrames} - {state.Width} - {state.Height}");
                StateDirection[] stateDirections = StatesController.GetAllStateDirections(state.DirectionDepth);
                for (int i = 0; i < state.Frames; i++)
                {
                    foreach (StateDirection direction in stateDirections)
                    {
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

            string configName = config.FileName.Replace(EnvironmentController.configFormat, "");
            string fileExportPath = Path.Combine(EnvironmentController.lastExportPath, configName, path);
            string? directoryExportPath = Path.GetDirectoryName(fileExportPath);

            if (directoryExportPath != null && 
                !Directory.Exists(directoryExportPath))
            {
                Directory.CreateDirectory(directoryExportPath);
            }

            if (StatesController.isOverrideToggle && File.Exists(fileExportPath))
            {
                Debug.WriteLine($"SAVED {fileExportPath} - Override");
                file.Save(fileExportPath);
                return;
            }

            if (!File.Exists(fileExportPath))
            {
                Debug.WriteLine($"SAVED {fileExportPath}");
                file.Save(fileExportPath);
                return;
            }

            Debug.WriteLine($"NOT SAVED {fileExportPath}");
        }

        #endregion Processors

        #region Draw

        private static void EditStateWithConfig(DMIState state, ConfigItem config)
        {
            StateDirection[] stateDirections = StatesController.GetAllStateDirections(state.DirectionDepth);

            foreach (StateDirection direction in stateDirections)
            {
                for (int i = 0; i < state.Frames; i++)
                {
                    iter++;

                    // !!!!!!!!!!! ЗДЕСЬ НУЖНО СДЕЛАТЬ ОБРАБОТЧИК НОРМАЛЬНЫЙ !!!!!!!!!!!!!!!!

                    using Image<Rgba32>? image = state.GetFrame(direction, i);
                    if (image == null) return;

                    image.Mutate(x => x
                         .Resize(image.Width / 2, image.Height / 2)
                         .Grayscale());

                    //image.ProcessPixelRows(accessor =>
                    //{
                    //    // Color is pixel-agnostic, but it's implicitly convertible to the Rgba32 pixel type
                    //    Rgba32 transparent = Color.Transparent;

                    //    for (int y = 0; y < accessor.Height; y++)
                    //    {
                    //        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    //        // pixelRow.Length has the same value as accessor.Width,
                    //        // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                    //        for (int x = 0; x < pixelRow.Length; x++)
                    //        {
                    //            // Get a reference to the pixel at position x
                    //            ref Rgba32 pixel = ref pixelRow[x];
                    //            if (pixel.A == 0)
                    //            {
                    //                // Overwrite the pixel referenced by 'ref Rgba32 pixel':
                    //                pixel = transparent;
                    //            }
                    //            pixel = Color.Red; // !!!! TEST
                    //        }
                    //    }
                    //});

                }
            }

        }


        #endregion Draw


        #region Helpers

        public static bool IsIterEnded() => (iter >= maxIter) || (iter == 0);

        #endregion Helpers












        /*

        private static int maxIter = 0;
        private static int iter = 0;

        private static Dictionary<string, DMIFile> DMIFiles = new Dictionary<string, DMIFile>();

        private static ProgressBar progressBarProcess;
        private static Label statusMessage;

        public static event Action<int, ConfigItem, string> ProcessFileWithConfigStarted;

        #region Initializers

        public static void InitializeNewData(List<string> filesPaths)
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
            // TODO: Improve the threads to run processing on several configs at once.
            //List<Task> tasks = new List<Task>();
            foreach (var path in DMIFiles.Keys)
            {
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
                statusMessage.Content = $"Processing file[{iter}/{maxIter}] -- {percent}";
                if (IsIterEnded())
                {
                    statusMessage.Content = "Completed!";
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
            //await Task.WhenAll();
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (StatesController.isOverrideToggle && File.Exists(filePath))
            {
                File.Delete(filePath);
                file.Save(filePath);
            }
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"SAVED {filePath}");
                file.Save(filePath);
            }    
        }

        #endregion Processors

        #region Draw

        private static void EditStateWithConfig(DMIState state, ConfigItem config)
        {
            StateDirection[] stateDirections = StatesController.GetAllStateDirections(state.DirectionDepth);

            foreach (StateDirection direction in stateDirections)
            {
                for (int i = 0; i < state.Frames; i++)
                {
                    iter++;

                    using Image<Rgba32>? image = state.GetFrame(direction, i).Clone();
                    if (image == null) return;

                    //image.ProcessPixelRows(accessor =>
                    //{
                    //    // Color is pixel-agnostic, but it's implicitly convertible to the Rgba32 pixel type
                    //    Rgba32 transparent = Color.Transparent;

                    //    for (int y = 0; y < accessor.Height; y++)
                    //    {
                    //        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    //        // pixelRow.Length has the same value as accessor.Width,
                    //        // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                    //        //for (int x = 0; x < pixelRow.Length; x++)
                    //        //{
                    //        //    // Get a reference to the pixel at position x
                    //        //    ref Rgba32 pixel = ref pixelRow[x];
                    //        //    if (pixel.A == 0)
                    //        //    {
                    //        //        // Overwrite the pixel referenced by 'ref Rgba32 pixel':
                    //        //        pixel = transparent;
                    //        //    }
                    //        //}

                    //        foreach (ref Rgba32 pixel in pixelRow)
                    //        {
                    //            if (pixel.A == 0)
                    //            {
                    //                // overwrite the pixel referenced by 'ref Rgba32 pixel':
                    //                pixel = transparent;
                    //            }
                    //        }
                    //    }
                    //});

                    state.SetFrame(image, direction, i);

                }
            }

        }


        #endregion Draw


        #region Helpers

        public static bool IsIterEnded() => (iter >= maxIter) || (iter == 0);

        #endregion Helpers





        */




    }
}
