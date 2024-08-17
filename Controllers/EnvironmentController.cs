using AdaptiveSpritesDMItool.Models;
using DMISharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class EnvironmentController
    {
        public static DataImageState dataImageState;
        public static DataImageState dataImageStateOverlay;

        public static int widthImage;
        public static int heightImage;


        public static void LoadEnvironment()
        {
            //Debug.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            string path = "TestImages";


            string fullpath = $"{path}/testBodyHuman.dmi";
            using DMIFile file = new DMIFile(fullpath);

            DMIState currentState = file.States.First();
            dataImageState = new DataImageState(currentState);

            Debug.WriteLine($"Loaded {file}({file.States.Count}).");

            widthImage = currentState.Width;
            heightImage = currentState.Height;

            Debug.WriteLine($"Image - Width: {widthImage}; Height: {heightImage}");


            // Overlay Preview File
            string fullpathOverlay = $"{path}/testClothingOveralls.dmi";
            using DMIFile fileOverlay = new DMIFile(fullpathOverlay);

            DMIState currentStateOverlay = fileOverlay.States.First();
            dataImageStateOverlay = new DataImageState(currentStateOverlay);

            Debug.WriteLine($"Loaded {fileOverlay}({fileOverlay.States.Count}).");
        }

        public static WriteableBitmap GetEnvironmentImage(StateDirection _stateDirection, bool _isEdited = false)
        {
            WriteableBitmap bitmap = dataImageState.GetBMPstate(_stateDirection, _isEdited);
            return bitmap;
        }

        public static WriteableBitmap GetEnvironmentImageOverlay(StateDirection _stateDirection, bool _isEdited = false)
        {
            WriteableBitmap bitmapOverlay = dataImageStateOverlay.GetBMPstate(_stateDirection, _isEdited);
            return bitmapOverlay;
        }
    }
}
