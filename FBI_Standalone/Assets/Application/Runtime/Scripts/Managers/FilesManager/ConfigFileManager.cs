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

    public bool Save(ConfigFile config = null)
    {
        config ??= CurrentConfig;
        if (config == null) { EventFileManager.Log("[ConfigFileManager] No config to save."); return false; }

        config.lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            string yaml = serializer.Serialize(config);
            File.WriteAllText(GetPath(config.configName), yaml);
            //Debug.Log($"[ConfigFileManager] Saved: {GetPath(config.configName)}");
            OnConfigSaved?.Invoke(config);
            return true;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[ConfigFileManager] Save error: {e.Message}");
            return false;
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

    private string GetPath(string configName) =>
        Path.Combine(RootPath, $"{configName}.yaml");
}