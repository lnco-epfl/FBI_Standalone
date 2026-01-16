using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Gestionnaire de séquences remplaçant le ScriptableObject Sequences
/// Charge les séquences depuis des fichiers YAML
/// </summary>
public class SequencesManager : MonoBehaviour
{
    private static SequencesManager instance;

    public static SequencesManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("SequencesManager");
                instance = go.AddComponent<SequencesManager>();
                DontDestroyOnLoad(go);
                instance.Initialize();
            }
            return instance;
        }
    }

    [SerializeField]
    private string sequencesFolder = "Sequences";

    private Dictionary<string, Sequence> loadedSequences = new Dictionary<string, Sequence>();
    private List<Sequence> sequences = new List<Sequence>();

    public List<Sequence> Sequences => sequences;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        LoadAllSequences();
    }

    /// <summary>
    /// Charge toutes les séquences depuis le dossier YAML
    /// </summary>
    public void LoadAllSequences()
    {
        loadedSequences.Clear();
        sequences.Clear();

        string folderPath = Path.Combine(Application.streamingAssetsPath, sequencesFolder);

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

        Debug.Log($"Loaded {sequences.Count} sequences from YAML files");
    }

    /// <summary>
    /// Charge une séquence depuis un fichier YAML spécifique
    /// </summary>
    public Sequence LoadSequence(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        if (loadedSequences.ContainsKey(fileName))
        {
            Debug.LogWarning($"Sequence '{fileName}' already loaded");
            return loadedSequences[fileName];
        }

        Sequence sequence = SequenceYamlLoader.LoadSequenceFromYaml(filePath);

        if (sequence != null)
        {
            sequence.name = fileName;
            loadedSequences[fileName] = sequence;
            sequences.Add(sequence);
            Debug.Log($"Loaded sequence: {fileName}");
        }

        return sequence;
    }

    /// <summary>
    /// Récupère une séquence par son nom
    /// </summary>
    public Sequence GetSequence(string sequenceName)
    {
        if (loadedSequences.TryGetValue(sequenceName, out Sequence sequence))
        {
            return sequence;
        }

        Debug.LogWarning($"Sequence '{sequenceName}' not found");
        return null;
    }

    /// <summary>
    /// Sauvegarde une séquence dans un fichier YAML
    /// </summary>
    public void SaveSequence(Sequence sequence, string fileName = null)
    {
        if (sequence == null)
        {
            Debug.LogError("Cannot save null sequence");
            return;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = sequence.name;
        }

        string folderPath = Path.Combine(Application.streamingAssetsPath, sequencesFolder);
        string filePath = Path.Combine(folderPath, $"{fileName}.yaml");

        SequenceYamlLoader.SaveSequenceToYaml(sequence, filePath);

        if (!loadedSequences.ContainsKey(fileName))
        {
            loadedSequences[fileName] = sequence;
            sequences.Add(sequence);
        }
    }

    /// <summary>
    /// Recharge toutes les séquences depuis les fichiers YAML
    /// </summary>
    public void ReloadSequences()
    {
        LoadAllSequences();
    }

    /// <summary>
    /// Crée une nouvelle séquence vide
    /// </summary>
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