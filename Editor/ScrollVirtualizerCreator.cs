using UnityEngine;
using UnityEditor;
using System.IO;

namespace NoranDev.ScrollVirtualizer.Editor
{
    public static class ScrollVirtualizerCreator
    {
        /// <summary>
        /// Creates ScrollVirtualizer files in the selected folder.
        /// </summary>
        [MenuItem("Assets/Create/ScrollVirtualizer/Create ScrollVirtualizer Files...", false, 80)]
        private static void CreateScrollVirtualizerFiles()
        {
            var selectedPath = GetSelectedPath();
            ScrollVirtualizerCreatorWindow.ShowWindow(selectedPath);
        }

        /// <summary>
        /// Validates that a folder is selected for creating ScrollVirtualizer files.
        /// </summary>
        [MenuItem("Assets/Create/ScrollVirtualizer/Create ScrollVirtualizer Files...", true)]
        private static bool ValidateCreateScrollVirtualizerFiles()
        {
            var selectedPath = GetSelectedPath();
            return Directory.Exists(selectedPath);
        }

        /// <summary>
        /// Gets the path of the selected asset folder.
        /// </summary>
        private static string GetSelectedPath()
        {
            var path = "Assets";

            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }
                break;
            }

            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }

            return path;
        }
    }
}
