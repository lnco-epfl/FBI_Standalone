#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class CopyPluginsExtensionsWindowsPostBuild : IPostprocessBuildWithReport
{
    // Runs late in the post-build sequence.
    public int callbackOrder => 1000;

    // Relative to Unity project:
    private const string SourceFolderUnderAssets = "AzureKinectExamples/SDK/Kinect4AzureSDK/Plugins/extensions";

    public void OnPostprocessBuild(BuildReport report)
    {
        // Only handle Windows standalone players.
        if (report.summary.platform != BuildTarget.StandaloneWindows &&
            report.summary.platform != BuildTarget.StandaloneWindows64)
        {
            return;
        }

        // Build output
        string exePath = report.summary.outputPath;

        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            Debug.LogError($"Post-build copy failed: build output does not exist: {exePath}");
            return;
        }

        // Source path
        string sourceFolder = Path.GetFullPath(Path.Combine(Application.dataPath, SourceFolderUnderAssets));

        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogWarning($"Post-build copy skipped: source folder does not exist: {sourceFolder}");
            return;
        }

        // Windows player layout
        string buildFolder = Path.GetDirectoryName(exePath);
        string exeNameWithoutExtension = Path.GetFileNameWithoutExtension(exePath);
        string dataFolder = Path.Combine(buildFolder, exeNameWithoutExtension + "_Data");
        string pluginsFolder = Path.Combine(dataFolder, "Plugins/x86_64");

        if (!Directory.Exists(dataFolder))
        {
            Debug.LogError($"Post-build copy failed: data folder not found: {dataFolder}");
            return;
        }

        Directory.CreateDirectory(pluginsFolder);

        // Final destination
        string destinationFolder = Path.Combine(pluginsFolder, Path.GetFileName(sourceFolder));

        CopyDirectoryRecursive(sourceFolder, destinationFolder);

        Debug.Log($"Copied Plugins subfolder:\nFROM: {sourceFolder}\nTO:   {destinationFolder}");
    }

    private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (string filePath in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".meta"))
                continue;

            string destinationFilePath = Path.Combine(destinationDir, fileName);
            File.Copy(filePath, destinationFilePath, overwrite: true);
        }

        foreach (string subDirPath in Directory.GetDirectories(sourceDir))
        {
            string subDirName = Path.GetFileName(subDirPath);
            string destinationSubDirPath = Path.Combine(destinationDir, subDirName);
            CopyDirectoryRecursive(subDirPath, destinationSubDirPath);
        }
    }

}
#endif
