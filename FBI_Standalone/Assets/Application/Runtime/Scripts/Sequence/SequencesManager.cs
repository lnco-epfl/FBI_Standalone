using UnityEngine;
using System.Collections.Generic;
using System.IO;


public class SequencesManager : MonoBehaviour
{
    private static SequencesManager instance;
    public static SequencesManager Instance { get { return instance; } }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); }
        else
        {
            instance = this;
        }

        Initialize();
    }

    [SerializeField]
    private string sequencesFolder = "Sequences";

    private Dictionary<string, Sequence> loadedSequences = new Dictionary<string, Sequence>();
    private List<Sequence> sequences = new List<Sequence>();

    public List<Sequence> Sequences => sequences;

    public void Initialize()
    {
        LoadAllSequences();
    }

    public void LoadAllSequences()
    {
        loadedSequences.Clear();
        sequences.Clear();

        string folderPath = Path.Combine(Application.dataPath, "..", "Input", sequencesFolder);

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"Sequences folder not found: {folderPath}");
            Directory.CreateDirectory(folderPath);
            return;
        }

        string[] yamlFiles = Directory.GetFiles(folderPath, "*.yaml", SearchOption.AllDirectories);
        string[] ymlFiles = Directory.GetFiles(folderPath, "*.yml", SearchOption.AllDirectories);

        var allFiles = new List<string>();
        allFiles.AddRange(yamlFiles);
        allFiles.AddRange(ymlFiles);

        foreach (string file in allFiles)
        {
            LoadSequence(file);
        }

        EventFileManager.Log($"[SequencesManager] Loaded {sequences.Count} sequences from YAML files");
    }

    public Sequence LoadSequence(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        if (loadedSequences.ContainsKey(fileName))
        {
            EventFileManager.Warning($"[SequencesManager] Sequence '{fileName}' already loaded");
            return loadedSequences[fileName];
        }

        Sequence sequence = SequenceYamlLoader.Instance.LoadSequenceFromYaml(filePath);

        if (sequence != null)
        {
            sequence.name = fileName;
            loadedSequences[fileName] = sequence;
            sequences.Add(sequence);
            EventFileManager.Log($"[SequencesManager] Loaded sequence: {fileName}");
        }

        return sequence;
    }

    public Sequence GetSequence(string sequenceName)
    {
        if (loadedSequences.TryGetValue(sequenceName, out Sequence sequence))
        {
            return sequence;
        }

        EventFileManager.Warning($"[SequencesManager] Sequence '{sequenceName}' not found");
        return null;
    }

    public void ReloadSequences()
    {
        LoadAllSequences();
    }

    public Sequence CreateNewSequence(string name)
    {
        Sequence newSequence = ScriptableObject.CreateInstance<Sequence>();
        newSequence.name = name;
        return newSequence;
    }

#if UNITY_EDITOR
    [ContextMenu("Load All Sequences")]
    public void EditorLoadAllSequences()
    {
        LoadAllSequences();
    }

    [ContextMenu("Print Loaded Sequences")]
    public void PrintLoadedSequences()
    {
        Debug.Log($"Total sequences loaded: {sequences.Count}");
        foreach (var seq in sequences)
        {
            Debug.Log($"- {seq.name} ({seq.steps.Count} steps)");
        }
    }
#endif
}