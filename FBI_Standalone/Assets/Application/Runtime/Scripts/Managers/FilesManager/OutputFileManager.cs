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

    public string CameraConfigFile { get; set; }
    public string DisplayConfigFile { get; set; }
    public string CameraDelays { get; set; }
    public float CameraDisplayDuration { get; set; }
    public string CameraIDs { get; set; }

    public bool AsInterpolation { get; set; }
    public bool AsDissolution { get; set; }

    public double TimeSinceStart { get; set; }
    public string StepType { get; set; }
    public int StepCount { get; set; }

    public string Scene {  get; set; }

    public int LikertResponse { get; set; }
    public double LikertResponseTime { get; set; }

    public string QuestionResponse { get; set; }
    public string QuestionResponseIndex { get; internal set; }
    public double QuestionResponseTime { get; set; }


    public void ResetAll()
    {
        Language = string.Empty;
        Gender = string.Empty;
        Age = 0;

        SequenceFile = string.Empty;

        CameraConfigFile = string.Empty;
        DisplayConfigFile = string.Empty;

        Scene = string.Empty;

        TimeSinceStart = 0f;
        StepType = string.Empty;
        StepCount = 0;

        CameraDelays = string.Empty;
        CameraDisplayDuration = 0f;
        CameraIDs = string.Empty;

        AsInterpolation = false;
        AsDissolution = false;

        LikertResponse = 0;
        LikertResponseTime = 0f;

        QuestionResponse = string.Empty;
        QuestionResponseTime = 0f;
        QuestionResponseIndex = string.Empty;

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
        CameraConfigFileManager.Instance.OnConfigLoaded += OnCameraConfigLoaded;
        DisplayConfigFileManager.Instance.OnConfigLoaded += OnDisplayConfigLoaded;
    }



    private void OnDisable()
    {
        ExperimentManager.Instance.OnInitialized -= OnExperiementInitialize;
        CameraConfigFileManager.Instance.OnConfigLoaded -= OnCameraConfigLoaded;
        DisplayConfigFileManager.Instance.OnConfigLoaded -= OnDisplayConfigLoaded;
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
    private void OnCameraConfigLoaded(CameraConfigFile file)
    {
        OutputFileData.CameraConfigFile = file.configName;
    }

    private void OnDisplayConfigLoaded(DisplayConfigFile file)
    {
        OutputFileData.DisplayConfigFile = file.configName;
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

        EventFileManager.Log($"[OutputFileManager] Initialized: { newOutputFilePath}");
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

        EventFileManager.Log($"[OutputFileManager] Data saved: {outputFilePath}");

        OutputFileData.Reset();
    }


}