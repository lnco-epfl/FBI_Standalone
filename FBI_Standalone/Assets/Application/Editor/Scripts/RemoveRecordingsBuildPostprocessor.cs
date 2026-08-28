using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Callbacks;
using UnityEngine;

public static class RemoveRecordingsBuildPostprocessor
{
    private const string ConnectedSensorDefine = "CONNECTED_SENSOR"; // Sensor_Windows profile
    private const string PlayRecordingDefine = "PLAY_RECORDING";     // Recording_Windows profile

    private const string RecordingsFolderName = "Recordings";

    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        string[] defines = GetActiveScriptingDefines(target);

        bool isConnectedSensor = defines.Contains(ConnectedSensorDefine);
        bool isPlayRecording = defines.Contains(PlayRecordingDefine);

        if (isPlayRecording && !isConnectedSensor)
        {
            Debug.Log("[RemoveRecordingsPostProcess] 'PLAY_RECORDING' is active, keeping Recordings folder.");
            return;
        }

        if (!isConnectedSensor)
        {
            Debug.LogWarning("[RemoveRecordingsPostProcess] Neither 'CONNECTED_SENSOR' nor 'PLAY_RECORDING' found in active defines, keeping Recordings folder.");
            return;
        }

        string recordingsPath = GetRecordingsPathInBuild(pathToBuiltProject);

        if (!Directory.Exists(recordingsPath))
        {
            Debug.LogWarning($"[RemoveRecordingsPostProcess] Recordings folder not found in build output: {recordingsPath}");
            return;
        }

        try
        {
            Directory.Delete(recordingsPath, true);

            string metaFile = recordingsPath + ".meta";
            if (File.Exists(metaFile))
            {
                File.Delete(metaFile);
            }

            Debug.Log($"[RemoveRecordingsPostProcess] 'CONNECTED_SENSOR' is active, removed Recordings folder from build: {recordingsPath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"[RemoveRecordingsPostProcess] Failed to delete Recordings folder ({recordingsPath}): {e.Message}");
        }
    }

    private static string[] GetActiveScriptingDefines(BuildTarget target)
    {
        BuildProfile activeProfile = BuildProfile.GetActiveBuildProfile();

        if (activeProfile != null && activeProfile.scriptingDefines != null && activeProfile.scriptingDefines.Length > 0)
        {
            return activeProfile.scriptingDefines;
        }

        BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
        NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
        string definesString = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

        return string.IsNullOrEmpty(definesString) ? Array.Empty<string>() : definesString.Split(';', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string GetRecordingsPathInBuild(string pathToBuiltProject)
    {
        string buildFolder = Path.GetDirectoryName(pathToBuiltProject);
        string dataFolderName = Path.GetFileNameWithoutExtension(pathToBuiltProject) + "_Data";

        return Path.Combine(buildFolder, dataFolderName, "StreamingAssets", RecordingsFolderName);
    }
}
