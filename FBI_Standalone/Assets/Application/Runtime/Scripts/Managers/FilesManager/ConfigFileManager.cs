using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class ConfigFileManager : MonoBehaviour
{
    public static ConfigFileManager Instance { get; private set; }

    private string configFolder = "Configs";

    private string RootPath => Path.Combine(Application.dataPath, "..", "Input", configFolder);

    private ISerializer serializer;
    private IDeserializer deserializer;
    private ConfigFile currentConfig;

    public ConfigFile CurrentConfig { get => currentConfig; private set => currentConfig = value; }

    public event Action<ConfigFile> OnConfigLoaded;
    public event Action<ConfigFile> OnConfigSaved;
    public event Action<List<string>> OnFileListRefreshed;

    private void Awake()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
        deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

        currentConfig = null;

        if (!Directory.Exists(RootPath))
        {
            Directory.CreateDirectory(RootPath);
        }
    }

    public List<string> GetAvailableConfigs()
    {
        var files = Directory.GetFiles(RootPath, "*.yaml");
        var names = new List<string>();
        foreach (var f in files)
            names.Add(Path.GetFileNameWithoutExtension(f));
        OnFileListRefreshed?.Invoke(names);
        return names;
    }

    public bool IsValideConfigName(string name)
    {
        var configs = ConfigFileManager.Instance.GetAvailableConfigs();

        if (configs.Contains(name))
        {
            return true;
        }

        return false;

    }

    public ConfigFile CreateNew(string configName)
    {
        var cfg = new ConfigFile
        {
            configName = configName,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        CurrentConfig = cfg;
        return cfg;
    }

    public ConfigFile Load(string configName)
    {

        string path = GetPath(configName);
        if (!File.Exists(path))
        {
            EventFileManager.Log($"[ConfigFileManager] File not found: {path}");
            return null;
        }

        try
        {
            string yaml = File.ReadAllText(path);
            CurrentConfig = deserializer.Deserialize<ConfigFile>(yaml);
            EventFileManager.Log($"[ConfigFileManager] Loaded: {path}");
            OnConfigLoaded?.Invoke(CurrentConfig);
            return CurrentConfig;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[ConfigFileManager] Load error: {e.Message}\n{e.StackTrace}\n{e.InnerException?.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads a config directly from a full file path (used with StandaloneFileBrowser).
    /// </summary>
    public ConfigFile LoadFromPath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            EventFileManager.Log($"[ConfigFileManager] File not found: {fullPath}");
            return null;
        }

        try
        {
            string yaml = File.ReadAllText(fullPath);
            CurrentConfig = deserializer.Deserialize<ConfigFile>(yaml);
            CurrentConfig.configName = Path.GetFileNameWithoutExtension(fullPath);
            EventFileManager.Log($"[ConfigFileManager] Loaded from path: {fullPath}");
            OnConfigLoaded?.Invoke(CurrentConfig);
            return CurrentConfig;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[ConfigFileManager] Load error: {e.Message}\n{e.StackTrace}\n{e.InnerException?.Message}");
            return null;
        }
    }

    public bool Save(ConfigFile config = null)
    {
        config ??= CurrentConfig;
        if (config == null) { EventFileManager.Log("[ConfigFileManager] No config to save."); return false; }

        config.lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            string yaml = serializer.Serialize(config);
            File.WriteAllText(GetPath(config.configName), yaml);
            OnConfigSaved?.Invoke(config);
            return true;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[ConfigFileManager] Save error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Saves the current config to a specific full path (used with StandaloneFileBrowser Save As dialog).
    /// Updates the current config name to the new file name.
    /// </summary>
    public bool SaveAs(string fullPath, ConfigFile config = null)
    {
        config ??= CurrentConfig;
        if (config == null) { EventFileManager.Log("[ConfigFileManager] No config to save."); return false; }

        try
        {
            string newName = Path.GetFileNameWithoutExtension(fullPath);
            config.configName = newName;
            config.lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string yaml = serializer.Serialize(config);
            File.WriteAllText(fullPath, yaml);
            CurrentConfig = config;
            OnConfigSaved?.Invoke(config);
            EventFileManager.Log($"[ConfigFileManager] Saved as: {fullPath}");
            return true;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[ConfigFileManager] SaveAs error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Copies the current config serialized as YAML to the system clipboard.
    /// </summary>
    public bool CopyToClipboard(ConfigFile config = null)
    {
        config ??= CurrentConfig;
        if (config == null) { EventFileManager.Log("[ConfigFileManager] No config to copy."); return false; }

        try
        {
            string yaml = serializer.Serialize(config);
            GUIUtility.systemCopyBuffer = yaml;
            EventFileManager.Log("[ConfigFileManager] Config copied to clipboard.");
            return true;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[ConfigFileManager] CopyToClipboard error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Pastes a config from the system clipboard (YAML format).
    /// Does NOT save to disk automatically — call Save() afterwards if needed.
    /// Returns null if the clipboard is empty or contains invalid data.
    /// </summary>
    public ConfigFile PasteFromClipboard()
    {
        string yaml = GUIUtility.systemCopyBuffer;

        if (string.IsNullOrWhiteSpace(yaml))
        {
            EventFileManager.Log("[ConfigFileManager] Clipboard is empty.");
            return null;
        }

        try
        {
            var config = deserializer.Deserialize<ConfigFile>(yaml);
            CurrentConfig = config;
            OnConfigLoaded?.Invoke(CurrentConfig);
            EventFileManager.Log("[ConfigFileManager] Config pasted from clipboard.");
            return CurrentConfig;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[ConfigFileManager] PasteFromClipboard error (invalid content): {e.Message}");
            return null;
        }
    }

    public bool Delete(string configName)
    {
        string path = GetPath(configName);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        GetAvailableConfigs();
        return true;
    }

    public void SaveObjectTransform(int cameraID, Transform t, bool saveImmediately = true)
    {
        if (CurrentConfig == null) { EventFileManager.Log("[ConfigFileManager] No active config."); return; }

        var existing = CurrentConfig.pointClouds.Find(o => o.ID == cameraID);
        if (existing != null)
        {
            existing.position = new SerializableVector3(t.position);
            existing.rotation = new SerializableVector3(t.eulerAngles);
            existing.scale = new SerializableVector3(t.localScale);
        }
        else
        {
            CurrentConfig.pointClouds.Add(new ObjectTransformData(cameraID, t));
        }

        if (saveImmediately) Save();
    }

    public void SaveDepthMax(int cameraID, float value, bool saveImmediately = true)
    {
        if (CurrentConfig == null) { EventFileManager.Log("[ConfigFileManager] No active config."); return; }

        var existing = CurrentConfig.pointClouds.Find(o => o.ID == cameraID);
        if (existing != null)
        {
            existing.depthMax = value;
        }
        else
        {
            var data = new ObjectTransformData(cameraID);
            data.depthMax = value;
            CurrentConfig.pointClouds.Add(data);
        }

        if (saveImmediately) Save();
    }

    public void SaveDepthMin(int cameraID, float value, bool saveImmediately = true)
    {
        if (CurrentConfig == null) { EventFileManager.Log("[ConfigFileManager] No active config."); return; }

        var existing = CurrentConfig.pointClouds.Find(o => o.ID == cameraID);
        if (existing != null)
        {
            existing.depthMin = value;
        }
        else
        {
            var data = new ObjectTransformData(cameraID);
            data.depthMin = value;
            CurrentConfig.pointClouds.Add(data);
        }

        if (saveImmediately) Save();
    }

    public void SaveFlip(int cameraID, bool flipX, bool flipY, bool saveImmediately = true)
    {
        if (CurrentConfig == null) { EventFileManager.Log("[ConfigFileManager] No active config."); return; }

        var existing = CurrentConfig.pointClouds.Find(o => o.ID == cameraID);
        if (existing != null)
        {
            existing.scale.x = flipX ? -1 : 1;
            existing.scale.y = flipY ? -1 : 1;
        }
        else
        {
            var data = new ObjectTransformData(cameraID);
            data.scale.x = flipX ? -1 : 1;
            data.scale.y = flipY ? -1 : 1;
            CurrentConfig.pointClouds.Add(data);
        }

        if (saveImmediately) Save();
    }

    public void SaveClamp(int cameraID, float xMin, float xMax, float yMin, float yMax, bool saveImmediately = true)
    {
        if (CurrentConfig == null) { EventFileManager.Log("[ConfigFileManager] No active config."); return; }

        var existing = CurrentConfig.pointClouds.Find(o => o.ID == cameraID);
        if (existing != null)
        {
            existing.clampXMin = xMin;
            existing.clampXMax = xMax;
            existing.clampYMin = yMin;
            existing.clampYMax = yMax;
        }
        else
        {
            var data = new ObjectTransformData(cameraID);
            data.clampXMin = xMin;
            data.clampXMax = xMax;
            data.clampYMin = yMin;
            data.clampYMax = yMax;
            CurrentConfig.pointClouds.Add(data);
        }

        if (saveImmediately) Save();
    }

    public void SaveUICanvas(Transform t, Color backgroundColor, bool saveImmediately = true)
    {
        if (CurrentConfig == null) { EventFileManager.Log("[ConfigFileManager] No active config."); return; }

        CurrentConfig.stimulusDisplay = new UITransformData(t);
        CurrentConfig.stimulusDisplay.backgroundColor = new SerializableColor(backgroundColor);

        if (saveImmediately) Save();
    }

    /// <summary>
    /// Returns the root folder path where configs are stored.
    /// Used to open StandaloneFileBrowser dialogs in the right location.
    /// </summary>
    public string GetRootPath() => RootPath;

    private string GetPath(string configName) =>
        Path.Combine(RootPath, $"{configName}.yaml");
}
