using AdaptiveSpritesDMItool.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace AdaptiveSpritesDMItool.Helpers
{
    internal static class ShowMessages
    {

        private static async void ShowMessageBox(string _title, string _content)
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = _title,
                Content = _content,
            };
            _ = await uiMessageBox.ShowDialogAsync();
        }


        #region Config

        public static void NoConfigSelected()
        {
            string title = "No config selected";
            string content =
                "The process has been cancelled." +
                "\nPlease upload the configs that will process the files, then select those that will participate in the process. ";
            ShowMessageBox(title, content);
        }

        public static void NotSavedConfigSelected()
        {
            string title = "Not saved config selected";
            string content =
                "One of the selected configs is not saved and does not have the required parameters for processing. " +
                "\nRemove the selection or save it on the \"Edit\" page.";
            ShowMessageBox(title, content);
        }

        #endregion Config

        public static void FileAlreadyLoaded()
        {
            string title = "File is already loaded";
            string content = "A file with a similar name has already been loaded. Operation cancelled. Change the name or load another file.";
            ShowMessageBox(title, content);
        }

        public static void NoDMIStatesFound()
        {
            string title = "No DMI states found";
            string content =
                "Please upload DMI files by selecting the folder of their content directory. All subfolders in this category will be processed as well. " +
                "\nThe final result will be uploaded to separate files and directories under the config name. " +
                "\nAll files will have the same names and states.";
            ShowMessageBox(title, content);
        }

        public static void ProcessInProgress()
        {
            string title = "Process in progress";
            string content =
                "The program is processing programs. " +
                "\nIt is not possible to start new processes, please wait until the end.";
            ShowMessageBox(title, content);
        }

        public static void NoDirectoryFound()
        {
            string title = "No directory found";
            string content =
                "Please write the correct full path to your directory. " +
                "\nIf the directory is in the build folder, then you only need to write its name, for example, " +
                "by writing \"Import\" you will get the directory where your build is located \"D:\\AdaptiveSpritesDMItool\\bin\\Debug\\net7.0-windows\\Import\"." +
                    $"\nThe default directory is \"{EnvironmentController.defaultImportPath}\"";
            ShowMessageBox(title, content);
        }

    }
}
