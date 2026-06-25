using System;
using System.IO;
using UnityEngine;

public class EventFileManager : MonoBehaviour
{
    [SerializeField] private string eventFileName = "_Events.txt";

    private string uniqueIdentifier = string.Empty;

    private bool echoToConsole = true;
    private bool addTimestamp = true;
    private bool breakOnError = false;

    private MemoryStream eventMemoryStream;
    private StreamWriter eventStreamer;
    private FileStream eventFileStream;

    private string outputFolderPath;

    private string timestamp;

    private enum EmessageType
    {
        Log,
        Warning,
        Error
    };

    #region Singelton
    private static EventFileManager instance;

    public static EventFileManager Instance { get { return instance; } }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); } else { instance = this; }

        Initialize();
    }
    #endregion


    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }

    public void Initialize()
    {
        DateTime now = DateTime.Now;
        timestamp = now.ToString("yyyyMMdd_HHmmss");

        outputFolderPath = Path.Combine(Application.dataPath, "..", "Output");

        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        eventMemoryStream = new MemoryStream();
        eventStreamer = new StreamWriter(eventMemoryStream);

    }


    private void WriteEventFile()
    {

        var newEventFileName = timestamp + eventFileName;

        byte[] bytes = eventMemoryStream.ToArray();

        eventFileStream = new FileStream(Path.Combine(outputFolderPath, newEventFileName), FileMode.OpenOrCreate);
        eventFileStream.Write(bytes, 0, bytes.Length);

        eventFileStream.Close();
    }

    void OnDestroy()
    {

        WriteEventFile();

        if (eventMemoryStream != null)
        {
            eventMemoryStream.Close();
            eventMemoryStream = null;
        }
        if (eventStreamer != null)
        {
            eventStreamer.Close();
            eventStreamer = null;
        }
        if (eventFileStream != null)
        {
            eventFileStream.Close();
            eventFileStream = null;
        }
    }

    public static void Log(string message)
    {
        string msg = "[Log] " + message;

        if (EventFileManager.Instance != null)
            EventFileManager.Instance.Write(EmessageType.Log, msg);
        else
            // Fallback if the debugging system hasn't been initialized yet.
            UnityEngine.Debug.Log(msg);
    }

    public static void Warning(string message)
    {
        string msg = "[Warning] " + message;

        if (EventFileManager.Instance != null)
            EventFileManager.Instance.Write(EmessageType.Warning, msg);
        else
            // Fallback if the debugging system hasn't been initialized yet.
            UnityEngine.Debug.LogWarning(msg);
    }

    public static void Error(string message)
    {
        string msg = "[Error] " + message;

        if (EventFileManager.Instance != null)
        {
            EventFileManager.Instance.Write(EmessageType.Error, msg);

            UnityEngine.Debug.LogError(msg);

            if (EventFileManager.Instance.breakOnError)
            {
                UnityEngine.Debug.Break();
            }
        }
    }
    private void Write(EmessageType type, string message)
    {

        var finalMessage = message;
        if (addTimestamp)
        {
            DateTime now = DateTime.Now;
            finalMessage = string.Format("[{0:HH:mm:ss}] {1}", now, message);
        }

        if (eventStreamer != null)
        {
            eventStreamer.WriteLine(finalMessage);
            eventStreamer.Flush();

            if (eventFileStream != null)
            {
                WriteEventFile();
            }

        }

        if (echoToConsole)
        {
            switch (type)
            {
                case EmessageType.Log:
                    UnityEngine.Debug.Log(message);
                    break;
                case EmessageType.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case EmessageType.Error:
                    UnityEngine.Debug.LogError(message);
                    break;
                default:
                    break;
            }
        }
    }
}
