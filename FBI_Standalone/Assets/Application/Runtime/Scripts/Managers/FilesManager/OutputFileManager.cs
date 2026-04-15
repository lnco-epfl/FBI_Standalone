using CsvHelper;
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public class OutputFileData
{
    public string Gender { get; set; }
    public int Age { get; set; }
    public string Language { get; set; }

    public string SequenceFile { get; set; }

    public string ConfigFile { get; set; }
    public float CameraDelay { get; set; }
    public float CameraDisplayDuration { get; set; }
    public string CameraID { get; set; }

    public double TimeSinceStart { get; set; }
    public string StepType { get; set; }
    public int StepCount { get; set; }

    public string Scene {  get; set; }

    public int LikertResponse { get; set; }
    public double LikertResponseTime { get; set; }

    public string QuestionResponse { get; set; }
    public double QuestionResponseTime { get; set; }


    public void ResetAll()
    {
        Language = string.Empty;
        Gender = string.Empty;
        Age = 0;

        SequenceFile = string.Empty;

        ConfigFile = string.Empty;

        Scene = string.Empty;

        TimeSinceStart = 0f;
        StepType = string.Empty;
        StepCount = 0;

        CameraDelay = 0f;
        CameraDisplayDuration = 0f;
        CameraID = string.Empty;

        LikertResponse = 0;
        LikertResponseTime = 0f;

        QuestionResponse = string.Empty;
        QuestionResponseTime = 0f;
    }

    public void Reset()
    {

        TimeSinceStart = 0f;
        StepType = string.Empty;
        StepCount = 0;

        LikertResponse = 0;
        LikertResponseTime = 0f;

        QuestionResponse = string.Empty;
        QuestionResponseTime = 0f;
    }
}

public class OutputFileManager : MonoBehaviour
{

    [SerializeField] private string outputFileName = "_Output.csv";

    private string uniqueIdentifier = string.Empty;
    private string timestamp;

    private string outputFolderPath;
    private string outputFilePath;
    public OutputFileData OutputFileData = new OutputFileData();


    #region Singelton
    private static OutputFileManager instance;

    public static OutputFileManager Instance { get { return instance; } }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); } else { instance = this; }
    }
    #endregion

    private void OnEnable()
    {
        ExperimentManager.Instance.OnInitialized += OnExperiementInitialize;
        ConfigFileManager.Instance.OnConfigLoaded += OnConfigLoaded;
    }



    private void OnDisable()
    {
        ExperimentManager.Instance.OnInitialized -= OnExperiementInitialize;
        ConfigFileManager.Instance.OnConfigLoaded -= OnConfigLoaded;
    }

    private void OnExperiementInitialize(bool isInitialize, Sequence sequence)
    {
        if(isInitialize)
        {
            Initialize(sequence.name);
            OutputFileData.SequenceFile = sequence != null ? sequence.name : string.Empty;
            OutputFileData.Gender = UIManager.Instance.SelectedGender;
            OutputFileData.Age = UIManager.Instance.SelectedAge;
        }
    }
    private void OnConfigLoaded(ConfigFile file)
    {
        OutputFileData.ConfigFile = file.configName;
    }

    public void Initialize(string name)
    {
        DateTime now = DateTime.Now;
        timestamp = now.ToString("yyyyMMdd_HHmmss");

        uniqueIdentifier = name;

        var newOutputFilePath = timestamp + "_" + uniqueIdentifier + outputFileName;

        outputFolderPath = Path.Combine(Application.dataPath, "..", "Output");
        outputFilePath = Path.Combine(outputFolderPath, newOutputFilePath);

        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        using (var writer = new StreamWriter(outputFilePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteHeader<OutputFileData>();
            csv.NextRecord();
        }

        EventFileManager.Log("$[OutputFileManager] initialized: { newOutputFilePath}");
        OutputFileData.ResetAll();
    }

    public void SaveOutputEntry()
    {
        using (var writer = new StreamWriter(outputFilePath, true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecord<OutputFileData>(OutputFileData);
            csv.NextRecord();
        }

        EventFileManager.Log($"[OutputFileManager] data saved: {outputFilePath}");

        OutputFileData.Reset();
    }


}