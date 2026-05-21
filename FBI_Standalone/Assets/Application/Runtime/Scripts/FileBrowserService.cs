using System;
using System.Threading;
using SFB;
using UnityEngine;

/// <summary>
/// Opens StandaloneFileBrowser dialogs on a background thread so the Unity main thread
/// (and VR camera streams) are never blocked.
/// Results are dispatched back to the main thread via MainThreadDispatcher.
/// </summary>
public static class FileBrowserService
{
    /// <summary>
    /// Opens a file picker dialog on a background thread.
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="directory">Initial directory</param>
    /// <param name="extension">File extension filter (e.g. "yaml")</param>
    /// <param name="onResult">Called on the main thread with the selected path, or null if cancelled</param>
    public static void OpenFile(string title, string directory, string extension, Action<string> onResult)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            string[] paths = null;

            try
            {
                paths = StandaloneFileBrowser.OpenFilePanel(title, directory, extension, false);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FileBrowserService] OpenFile error: {e.Message}");
            }

            string result = (paths != null && paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                ? paths[0]
                : null;

            MainThreadDispatcher.Instance.Enqueue(() => onResult?.Invoke(result));
        });
    }

    /// <summary>
    /// Opens a save dialog on a background thread.
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="directory">Initial directory</param>
    /// <param name="defaultName">Default file name</param>
    /// <param name="extension">File extension (e.g. "yaml")</param>
    /// <param name="onResult">Called on the main thread with the chosen path, or null if cancelled</param>
    public static void SaveFile(string title, string directory, string defaultName, string extension, Action<string> onResult)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            string path = null;

            try
            {
                path = StandaloneFileBrowser.SaveFilePanel(title, directory, defaultName, extension);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FileBrowserService] SaveFile error: {e.Message}");
            }

            string result = string.IsNullOrEmpty(path) ? null : path;

            MainThreadDispatcher.Instance.Enqueue(() => onResult?.Invoke(result));
        });
    }
}
