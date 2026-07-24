using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


public class DisplayConfigFileManager : MonoBehaviour
{


    private string RootPath => Path.Combine(Application.dataPath, "..", "Input", "Configs", "Display");

    private ISerializer serializer;
    private IDeserializer deserializer;
    private DisplayConfigFile currentConfig;

    public DisplayConfigFile CurrentConfig { get => currentConfig; private set => currentConfig = value; }

    public event Action<DisplayConfigFile> OnConfigLoaded;
    public event Action<DisplayConfigFile> OnConfigSaved;
    public event Action<List<string>> OnFileListRefreshed;

    public static DisplayConfigFileManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

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

    public bool IsValideConfigName(string name) => GetAvailableConfigs().Contains(name);

    public DisplayConfigFile CreateNew(string configName)
    {
        var cfg = new DisplayConfigFile
        {
            configName = configName,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            stimulusDisplay = new UITransformData()
        };
        CurrentConfig = cfg;
        return cfg;
    }

    public DisplayConfigFile Load(string configName)
    {
        string path = GetPath(configName);
        if (!File.Exists(path))
        {
            EventFileManager.Log($"[DisplayConfigFileManager] File not found: {path}");
            return null;
        }

        try
        {
            string yaml = File.ReadAllText(path);
            CurrentConfig = deserializer.Deserialize<DisplayConfigFile>(yaml);
            EventFileManager.Log($"[DisplayConfigFileManager] Loaded: {path}");
            OnConfigLoaded?.Invoke(CurrentConfig);
            return CurrentConfig;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[DisplayConfigFileManager] Load error: {e.Message}\n{e.StackTrace}\n{e.InnerException?.Message}");
            return null;
        }
    }

    public DisplayConfigFile LoadFromPath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            EventFileManager.Log($"[DisplayConfigFileManager] File not found: {fullPath}");
            return null;
        }

        try
        {
            string yaml = File.ReadAllText(fullPath);
            CurrentConfig = deserializer.Deserialize<DisplayConfigFile>(yaml);
            CurrentConfig.configName = Path.GetFileNameWithoutExtension(fullPath);
            EventFileManager.Log($"[DisplayConfigFileManager] Loaded from path: {fullPath}");
            OnConfigLoaded?.Invoke(CurrentConfig);
            return CurrentConfig;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[DisplayConfigFileManager] Load error: {e.Message}\n{e.StackTrace}\n{e.InnerException?.Message}");
            return null;
        }
    }

    public bool Save(DisplayConfigFile config = null)
    {
        config ??= CurrentConfig;
        if (config == null) { EventFileManager.Log("[DisplayConfigFileManager] No config to save."); return false; }

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
            EventFileManager.Log($"[DisplayConfigFileManager] Save error: {e.Message}");
            return false;
        }
    }

    public bool SaveAs(string fullPath, DisplayConfigFile config = null)
    {
        config ??= CurrentConfig;
        if (config == null) { EventFileManager.Log("[DisplayConfigFileManager] No config to save."); return false; }

        try
        {
            string newName = Path.GetFileNameWithoutExtension(fullPath);
            config.configName = newName;
            config.lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string yaml = serializer.Serialize(config);
            File.WriteAllText(fullPath, yaml);
            CurrentConfig = config;
            OnConfigSaved?.Invoke(config);
            EventFileManager.Log($"[DisplayConfigFileManager] Saved as: {fullPath}");
            return true;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[DisplayConfigFileManager] SaveAs error: {e.Message}");
            return false;
        }
    }

    public bool CopyToClipboard(DisplayConfigFile config = null)
    {
        config ??= CurrentConfig;
        if (config == null) { EventFileManager.Log("[DisplayConfigFileManager] No config to copy."); return false; }

        try
        {
            string yaml = serializer.Serialize(config);
            GUIUtility.systemCopyBuffer = yaml;
            EventFileManager.Log("[DisplayConfigFileManager] Config copied to clipboard.");
            return true;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[DisplayConfigFileManager] CopyToClipboard error: {e.Message}");
            return false;
        }
    }

    public DisplayConfigFile PasteFromClipboard()
    {
        string yaml = GUIUtility.systemCopyBuffer;

        if (string.IsNullOrWhiteSpace(yaml))
        {
            EventFileManager.Log("[DisplayConfigFileManager] Clipboard is empty.");
            return null;
        }

        try
        {
            var config = deserializer.Deserialize<DisplayConfigFile>(yaml);
            CurrentConfig = config;
            OnConfigLoaded?.Invoke(CurrentConfig);
            EventFileManager.Log("[DisplayConfigFileManager] Config pasted from clipboard.");
            return CurrentConfig;
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[DisplayConfigFileManager] PasteFromClipboard error (invalid content): {e.Message}");
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

    public void SaveUICanvas(Transform t, Color backgroundColor, bool saveImmediately = true)
    {
        if (CurrentConfig == null)
        {

            CurrentConfig = new DisplayConfigFile
            {
                configName = "Untitled",
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        CurrentConfig.stimulusDisplay = new UITransformData(t);
        CurrentConfig.stimulusDisplay.backgroundColor = new SerializableColor(backgroundColor);

        if (saveImmediately) Save();
    }

    public void ApplyStep(LoadDisplayConfigStep step, Transform canvasTransform)
    {
        bool hasOverride = step.positionOverride != null || step.rotationOverride != null ||
                            step.scaleOverride != null || step.backgroundColorOverride != null;

        if (!string.IsNullOrEmpty(step.configName))
        {
            // configName given: it becomes the base, applied fully first.
            Load(step.configName);
            CurrentConfig?.stimulusDisplay?.ApplyTo(canvasTransform);

            var loadedColor = CurrentConfig?.stimulusDisplay?.backgroundColor?.ToColor();
            if (loadedColor.HasValue)
            {
                WorldUIManager.Instance.SetCurrentBackgoundColor(loadedColor.Value);
                WorldUIManager.Instance.BackgroundColor = loadedColor.Value;
            }
        }
        else if (!hasOverride)
        {
            EventFileManager.Log("[DisplayConfigFileManager] LoadDisplayConfig step has no configName and no overrides — nothing to apply.");
            return;
        }

        // Overrides (if any) are applied on top, field by field, whether or not a configName was given.
        if (step.positionOverride != null) canvasTransform.position = step.positionOverride.ToVector3();
        if (step.rotationOverride != null) canvasTransform.eulerAngles = step.rotationOverride.ToVector3();
        if (step.scaleOverride != null) canvasTransform.localScale = step.scaleOverride.ToVector3();

        if (step.backgroundColorOverride != null)
        {
            var color = step.backgroundColorOverride.ToColor();
            WorldUIManager.Instance.SetCurrentBackgoundColor(color);
            WorldUIManager.Instance.BackgroundColor = color;
        }
    }

    public string GetRootPath() => RootPath;

    private string GetPath(string configName) =>
        Path.Combine(RootPath, $"{configName}.yaml");
}