﻿using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdaptiveSpritesDMItool.Helpers
{
    internal static class FilesSearcher
    {

        #region Directory
        public static List<string> GetDirectories(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
                return Directory.GetDirectories(path, searchPattern).ToList();

            //return System.IO.Directory.GetDirectories(path, "*", SearchOption.AllDirectories);

            var directories = new List<string>(GetDirectories(path, searchPattern));

            for (var i = 0; i < directories.Count; i++)
                directories.AddRange(GetDirectories(directories[i], searchPattern));

            return directories;
        }

        private static List<string> GetDirectories(string path, string searchPattern)
        {
            try
            {
                return Directory.GetDirectories(path, searchPattern).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<string>();
            }
        }

        public static List<string> GetDirectoriesWithoutOrigPath(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.AllDirectories)
        {
            var directories = GetDirectories(path, searchPattern, searchOption);
            var newDirectories = new List<string>();
            foreach (var directory in directories)
            {
                string newDirectory = directory.Replace(path + "\\", "");
                newDirectories.Add(newDirectory);
            }
            newDirectories.Add("");
            return newDirectories;
        }

        #endregion Directory


        #region Path
        public static string GetExportConfigPath(string configFileName, string path)
        {
            //if(fileExportPath.Last() != '\\') fileExportPath += '\\';
            string pathPart = path.Replace(EnvironmentController.lastImportPath, "");
            if (pathPart.First() == '\\') pathPart = pathPart.Remove(0, 1);
            string configName = configFileName.Replace(EnvironmentController.configFormat, "");
            string exportPath = Path.Combine(EnvironmentController.lastExportPath, configName, pathPart);
            return exportPath;
        }

        #endregion Path
    }
}
