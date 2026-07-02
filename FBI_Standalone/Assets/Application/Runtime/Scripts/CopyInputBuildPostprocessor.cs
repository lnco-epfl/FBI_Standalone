#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class CopyInputBuildPostprocessor
{
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        Debug.Log(pathToBuiltProject);

        var basePath = Path.Combine(Application.dataPath, "..", "Input");

        string[] dirs = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories);

    

        foreach (string dir in dirs)
        {
            Debug.Log(dir);

            string[] pathParts = dir.Split(Path.DirectorySeparatorChar);

            var newPath = Path.Combine(Path.GetDirectoryName(pathToBuiltProject), pathParts[pathParts.Length - 3], pathParts[pathParts.Length - 2], pathParts[pathParts.Length - 1]);
            Debug.Log(newPath);
            FileUtil.ReplaceFile(dir, newPath);
        }   

    }
}
#endif