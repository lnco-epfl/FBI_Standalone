using Eflatun.SceneReference;
using Intel.RealSense;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.VFX;

public class CanvasSetupPointCloudUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text title;

    [Header("New Config")]
    [SerializeField] private TMP_InputField configNameInputField;
    [SerializeField] private Button createConfigButton;
    [SerializeField] private CanvasGroup newConfigCanvasGroup;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private LocalizedString statusLocalizedText;

    [Header("Load Config")]
    [SerializeField] private TMP_Dropdown loadConfigDropdown;
    [SerializeField] private Button loadConfigButton;
    [SerializeField] private CanvasGroup loadConfigCanvasGroup;

    [Header("Point cloud")]
    [SerializeField] private GameObject pointCloudEntryPrefab;
    [SerializeField] private Transform pointCloudContainer;
    [SerializeField] private Button saveButton;

    [Header("Scene")]
    [SerializeField] private SceneReference scene;

    public event Action<CanvasSetupPointCloudUI> OnCanvasSetupPointCloudUIDestroy;

    private string selectedConfig;
    private List<string> configs;
    private string newConfigName;

    private List<PointCloudUIEntry> pointCloudEntries = new List<PointCloudUIEntry>();

    private void Start()
    {
        StartCoroutine(LoadScene());
        RefreshList();
    }

    private void OnEnable()
    {
        closeButton.onClick.AddListener(OnButtonCloseClick);
        createConfigButton.onClick.AddListener(OnCreateConfigButtonClick);
        saveButton.onClick.AddListener(OnSaveButtonClick);
        loadConfigDropdown.onValueChanged.AddListener(OnLoadConfigDropdownValueChanged);
        loadConfigButton.onClick.AddListener(OnLoadConfigButtonClick);
        configNameInputField.onSubmit.AddListener(OnConfigNameInputFieldSubmit);
        configNameInputField.onDeselect.AddListener(OnConfigNameInputFieldSubmit);

        ConfigFileManager.Instance.OnConfigLoaded += OnConfigLoaded;
        ConfigFileManager.Instance.OnConfigSaved += OnConfigSaved;
        ConfigFileManager.Instance.OnFileListRefreshed += OnFileListRefreshed;
        SceneLoaderManager.Instance.OnSceneLoaded += OnSceneLoaded;
    }



    private void OnDisable()
    {
        closeButton.onClick.RemoveListener(OnButtonCloseClick);
        createConfigButton.onClick.RemoveListener(OnCreateConfigButtonClick);
        saveButton.onClick.RemoveListener(OnSaveButtonClick);
        loadConfigDropdown.onValueChanged.RemoveListener(OnLoadConfigDropdownValueChanged);
        loadConfigButton.onClick.RemoveListener(OnLoadConfigButtonClick);
        configNameInputField.onSubmit.RemoveListener(OnConfigNameInputFieldSubmit);
        configNameInputField.onDeselect.RemoveListener(OnConfigNameInputFieldSubmit);

        ConfigFileManager.Instance.OnConfigLoaded -= OnConfigLoaded;
        ConfigFileManager.Instance.OnConfigSaved -= OnConfigSaved;
        ConfigFileManager.Instance.OnFileListRefreshed -= OnFileListRefreshed;
        SceneLoaderManager.Instance.OnSceneLoaded -= OnSceneLoaded;

        foreach (var entry in pointCloudEntries)
        {
            entry.OnPositionChanged -= OnPointCloudPositionChanged;
            entry.OnRotationChanged -= OnPointCloudRotationChanged;
        }
    }

    private void SpawnPointCloudEntries()
    {
        var cameraIds = PointCloudManager.Instance.GetAvailableCameraIds();

        foreach (var id in cameraIds)
        {
            var go = Instantiate(pointCloudEntryPrefab, pointCloudContainer);
            go.name = $"PointCloudEntry_Camera{id}";

            var entry = go.GetComponent<PointCloudUIEntry>();
            entry.Init(id);
            entry.SetInteractable(false);

            entry.SetDisplayToggle(false);

            entry.OnPositionChanged += OnPointCloudPositionChanged;
            entry.OnRotationChanged += OnPointCloudRotationChanged;

            pointCloudEntries.Add(entry);
        }
    }

    private void OnButtonCloseClick()
    {

        SceneLoaderManager.Instance.LoadDefaultScene();

        OnCanvasSetupPointCloudUIDestroy.Invoke(this);
    }

    private void OnSaveButtonClick()
    {
        foreach (var entry in pointCloudEntries)
        {
            OnPointCloudPositionChanged(entry);
            OnPointCloudRotationChanged(entry);
        }
        ConfigFileManager.Instance.Save();
        SetStatus($"Saved {selectedConfig}");
    }

    private void OnPointCloudPositionChanged(PointCloudUIEntry entry)
    {
        var t = PointCloudManager.Instance.GetVisualEffectTransform(entry.CameraId);
        t.position = entry.Position;
        ConfigFileManager.Instance.SaveObjectTransform(entry.CameraId, t);
        PointCloudManager.Instance.SetVisualEffectTransform(t, entry.CameraId);
    }

    private void OnPointCloudRotationChanged(PointCloudUIEntry entry)
    {
        var t = PointCloudManager.Instance.GetVisualEffectTransform(entry.CameraId);
        t.rotation = Quaternion.Euler(entry.Rotation);
        ConfigFileManager.Instance.SaveObjectTransform(entry.CameraId, t);
        PointCloudManager.Instance.SetVisualEffectTransform(t, entry.CameraId);
    }


    private void OnLoadConfigButtonClick()
    {
        ConfigFileManager.Instance.Load(selectedConfig);
        SetStatus($"Loaded {selectedConfig}");

        foreach (var entry in pointCloudEntries)
            entry.SetInteractable(true);
    }

    private void OnLoadConfigDropdownValueChanged(int value)
    {
        selectedConfig = configs[value];
    }

    private void OnCreateConfigButtonClick()
    {
        if (string.IsNullOrEmpty(newConfigName))
        {
            SetStatus("Please enter a config name.");
            return;
        }

        ConfigFileManager.Instance.CreateNew(newConfigName);
        ConfigFileManager.Instance.Save();
        selectedConfig = newConfigName;

        foreach (var entry in pointCloudEntries)
        {
            var t = PointCloudManager.Instance.GetVisualEffectTransform(entry.CameraId);
            entry.SetPositionFields(t.position);
            entry.SetRotationFields(t.eulerAngles);
            entry.SetInteractable(true);
        }

        RefreshList();
        SetStatus($"Created {newConfigName}");
    }

    private void OnConfigNameInputFieldSubmit(string value)
    {
        newConfigName = value.Trim();
    }

    private void RefreshList()
    {
      
        configs = ConfigFileManager.Instance.GetAvailableConfigs();

        if(configs.Count > 0)
        {

            loadConfigDropdown.ClearOptions();
            loadConfigDropdown.AddOptions(configs);

            loadConfigDropdown.value = configs.IndexOf(selectedConfig);

            loadConfigCanvasGroup.interactable = true;
        }
        else
        {
            loadConfigCanvasGroup.interactable = false;
        }

    }

    private void OnConfigSaved(ConfigFile file)
    {
        
    }

    private void OnConfigLoaded(ConfigFile file)
    {
        for (int i = 0; i < pointCloudEntries.Count; i++)
        {
            if (i >= file.pointClouds.Count) break;

            var data = file.pointClouds[i];
            pointCloudEntries[i].SetPositionFields(data.position.ToVector3());
            pointCloudEntries[i].SetRotationFields(data.rotation.ToVector3());
        }
    }

    private void OnFileListRefreshed(List<string> list)
    {

    }


    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene)
    {

        SpawnPointCloudEntries();

    }

    public IEnumerator LoadScene()
    {

        Fader.Instance.FadeToBlack();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        yield return SceneLoaderManager.Instance.LoadAsyncScene(scene);

        Fader.Instance.FadeToClear();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

    }

    private void SetStatus(string message)
    {
        Debug.Log($"[CanvasSetupPointCloudUI] {message}");
        if (statusText)
        {
            statusText.text = statusLocalizedText.GetLocalizedString(message);
        }
    }
}
