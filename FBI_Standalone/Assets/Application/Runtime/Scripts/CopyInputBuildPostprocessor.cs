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

        var basePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Input"));

        string[] dirs = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories);



        foreach (string dir in dirs)
        {
            var normalizedDir = Path.GetFullPath(dir);
            Debug.Log(normalizedDir);

            string relativePath = Path.GetRelativePath(basePath, normalizedDir);

            var newPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(pathToBuiltProject), "Input", relativePath));
            Debug.Log(newPath);

            var newDir = Path.GetDirectoryName(newPath);
            if (!Directory.Exists(newDir))
            {
                Directory.CreateDirectory(newDir);
            }

            FileUtil.ReplaceFile(normalizedDir, newPath);
        }

    }
}
#endif