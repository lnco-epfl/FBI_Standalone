using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class CameraConfigFileManager : MonoBehaviour
{
    public static CameraConfigFileManager Instance { get; private set; }

    private string RootPath => Path.Combine(Application.dataPath, "..", "Input", "Configs", "Camera");

    private ISerializer serializer;
    private IDeserializer deserializer;
    private CameraConfigFile currentConfig;

    public CameraConfigFile CurrentConfig { get => currentConfig; private set => currentConfig = value; }

    public event Action<CameraConfigFile> OnConfigLoaded;
    public event Action<CameraConfigFile> OnConfigSaved;
    public event Action<List<string>> OnFileListRefreshed;

    private void Awake()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; ;

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
        var configs = CameraConfigFileManager.Instance.GetAvailableConfigs();

        if (configs.Contains(name))
        {
            return true;
        }

        return false;

    }

    public CameraConfigFile CreateNew(string configName)
    {
        var cfg = new CameraConfigFile
        {
            configName = configName,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        CurrentConfig = cfg;
        return cfg;
    }

    public CameraConfigFile Load(string configName)
    {

        string path = GetPath(configName);
        if (!File.Exists(path))
        {
            EventFileManager.Log($"[CameraConfigFileManager] File not found: {path}");
            return null;
        }

        try
        {
            string yaml = File.ReadAllText(path);
            CurrentConfig = deserializer.Deserialize<CameraConfigFile>(yaml);
            EventFileManager.Log($"[CameraConfigFileManager] Loaded: {path}");
            OnConfigLoaded?.Invoke(CurrentConfig);
            return CurrentConfig;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[CameraConfigFileManager] Load error: {e.Message}\n{e.StackTrace}\n{e.InnerException?.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads a config directly from a full file path (used with StandaloneFileBrowser).
    /// </summary>
    public CameraConfigFile LoadFromPath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            EventFileManager.Log($"[CameraConfigFileManager] File not found: {fullPath}");
            return null;
        }

        try
        {
            string yaml = File.ReadAllText(fullPath);
            CurrentConfig = deserializer.Deserialize<CameraConfigFile>(yaml);
            CurrentConfig.configName = Path.GetFileNameWithoutExtension(fullPath);
            EventFileManager.Log($"[CameraConfigFileManager] Loaded from path: {fullPath}");
            OnConfigLoaded?.Invoke(CurrentConfig);
            return CurrentConfig;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[CameraConfigFileManager] Load error: {e.Message}\n{e.StackTrace}\n{e.InnerException?.Message}");
            return null;
        }
    }

    public bool Save(CameraConfigFile config = null)
    {
        config ??= CurrentConfig;
        if (config == null) { EventFileManager.Log("[CameraConfigFileManager] No config to save."); return false; }

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
            EventFileManager.Log($"[CameraConfigFileManager] Save error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Saves the current config to a specific full path (used with StandaloneFileBrowser Save As dialog).
    /// Updates the current config name to the new file name.
    /// </summary>
    public bool SaveAs(string fullPath, CameraConfigFile config = null)
    {
        config ??= CurrentConfig;
        if (config == null) { EventFileManager.Log("[CameraConfigFileManager] No config to save."); return false; }

        try
        {
            string newName = Path.GetFileNameWithoutExtension(fullPath);
            config.configName = newName;
            config.lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string yaml = serializer.Serialize(config);
            File.WriteAllText(fullPath, yaml);
            CurrentConfig = config;
            OnConfigSaved?.Invoke(config);
            EventFileManager.Log($"[CameraConfigFileManager] Saved as: {fullPath}");
            return true;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[CameraConfigFileManager] SaveAs error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Copies the current config serialized as YAML to the system clipboard.
    /// </summary>
    public bool CopyToClipboard(CameraConfigFile config = null)
    {
        config ??= CurrentConfig;
        if (config == null) { EventFileManager.Log("[CameraConfigFileManager] No config to copy."); return false; }

        try
        {
            string yaml = serializer.Serialize(config);
            GUIUtility.systemCopyBuffer = yaml;
            EventFileManager.Log("[CameraConfigFileManager] Config copied to clipboard.");
            return true;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[CameraConfigFileManager] CopyToClipboard error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Pastes a config from the system clipboard (YAML format).
    /// Does NOT save to disk automatically — call Save() afterwards if needed.
    /// Returns null if the clipboard is empty or contains invalid data.
    /// </summary>
    public CameraConfigFile PasteFromClipboard()
    {
        string yaml = GUIUtility.systemCopyBuffer;

        if (string.IsNullOrWhiteSpace(yaml))
        {
            EventFileManager.Log("[CameraConfigFileManager] Clipboard is empty.");
            return null;
        }

        try
        {
            var config = deserializer.Deserialize<CameraConfigFile>(yaml);
            CurrentConfig = config;
            OnConfigLoaded?.Invoke(CurrentConfig);
            EventFileManager.Log("[CameraConfigFileManager] Config pasted from clipboard.");
            return CurrentConfig;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[CameraConfigFileManager] PasteFromClipboard error (invalid content): {e.Message}");
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
        if (CurrentConfig == null) { EventFileManager.Log("[CameraConfigFileManager] No active config."); return; }

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
        if (CurrentConfig == null) { EventFileManager.Log("[CameraConfigFileManager] No active config."); return; }

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
        if (CurrentConfig == null) { EventFileManager.Log("[CameraConfigFileManager] No active config."); return; }

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
        if (CurrentConfig == null) { EventFileManager.Log("[CameraConfigFileManager] No active config."); return; }

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
        if (CurrentConfig == null) { EventFileManager.Log("[CameraConfigFileManager] No active config."); return; }

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

    public void SaveReferencePoint(int cameraID, Vector3 value, bool saveImmediately = true)
    {
        if (CurrentConfig == null) { EventFileManager.Log("[CameraConfigFileManager] No active config."); return; }

        var existing = CurrentConfig.pointClouds.Find(o => o.ID == cameraID);
        if (existing != null)
        {
            existing.referencePoint = new SerializableVector3(value);
        }
        else
        {
            var data = new ObjectTransformData(cameraID);
            data.referencePoint = new SerializableVector3(value);
            CurrentConfig.pointClouds.Add(data);
        }

        if (saveImmediately) Save();
    }

    public string GetRootPath() => RootPath;

    private string GetPath(string configName) =>
        Path.Combine(RootPath, $"{configName}.yaml");
}