#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class CopyInputBuildPostprocessor
{
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {


        StringBuilder stringBuilder = new StringBuilder();
            
        stringBuilder.Append($"Path : {pathToBuiltProject}");

        var basePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Input"));

        string[] dirs = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories);



        foreach (string dir in dirs)
        {
            var normalizedDir = Path.GetFullPath(dir);
   
            stringBuilder.AppendLine($"File Full Path : {pathToBuiltProject}");

            string[] pathParts = normalizedDir.Split(Path.DirectorySeparatorChar);

            var newPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(pathToBuiltProject), pathParts[pathParts.Length - 3], pathParts[pathParts.Length - 2], pathParts[pathParts.Length - 1]));

            stringBuilder.AppendLine($"File New Path : {newPath}");

            var newDir = Path.GetDirectoryName(newPath);
            if (!Directory.Exists(newDir))
            {
                stringBuilder.AppendLine($"New Directory : {newDir}");
                Directory.CreateDirectory(newDir);
            }

            FileUtil.ReplaceFile(normalizedDir, newPath);
        }

        Debug.Log(stringBuilder.ToString());

    }
}
#endif