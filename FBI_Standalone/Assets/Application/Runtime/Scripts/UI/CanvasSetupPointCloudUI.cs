using com.rfilkov.kinect;
using Eflatun.SceneReference;
using Intel.RealSense;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
    [SerializeField] private Image dotImage;
    [SerializeField] private LocalizedString statusLocalizedText;

    [Header("Load Config")]
    [SerializeField] private TMP_Dropdown loadConfigDropdown;
    [SerializeField] private Button loadConfigButton;
    [SerializeField] private CanvasGroup loadConfigCanvasGroup;

    [Header("Point cloud")]
    [SerializeField] private GameObject pointCloudEntryPrefab;
    [SerializeField] private Transform pointCloudContainer;
    [SerializeField] private Button saveButton;

    [Header("Point Cloud Switch")]
    [Tooltip("Delay in seconds between disabling the previous point cloud and enabling the next one, to allow textures to initialize.")]
    [SerializeField] private float switchDelay = 1.5f;

    [Header("VRView")]
    [SerializeField] private Button resetXROriginButton;

    [Header("Static View")]
    [SerializeField] private GameObject avatarToggleGameObject;
    private UISwitcher.UISwitcher avatarToggle; 

    [SerializeField] private TMP_InputField positionXInputField;
    [SerializeField] private TMP_InputField positionYInputField;
    [SerializeField] private TMP_InputField positionZInputField;
    [SerializeField] private TMP_InputField rotationXInputField;
    [SerializeField] private TMP_InputField rotationYInputField;
    [SerializeField] private TMP_InputField rotationZInputField;

    [SerializeField] private GameObject staticCameraPrefab;
    private Transform staticCamera;

    [Header("Scene")]
    [SerializeField] private SceneReference scene;



    public event Action<CanvasSetupPointCloudUI> OnCanvasSetupPointCloudUIDestroy;

    private string selectedConfig;
    private List<string> configs;
    private string newConfigName;

    private List<PointCloudUIEntry> pointCloudEntries = new List<PointCloudUIEntry>();

    /// <summary>
    /// The entry whose point cloud is currently active (visible). Null if none.
    /// </summary>
    private PointCloudUIEntry activeEntry;

    /// <summary>
    /// Prevents the user from switching while a switch transition is already in progress.
    /// </summary>
    private bool isSwitching;

    private void Awake()
    {
        avatarToggle = avatarToggleGameObject.GetComponent<UISwitcher.UISwitcher>();
    }


    private void Start()
    {
        StartCoroutine(LoadScene());
        RefreshList();
        ClearPointCloudEntry();
        
        ShortcutManager.Instance.DisableShortCut();

        
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

        resetXROriginButton.onClick.AddListener(OnResetXROriginButtonPress);

        positionXInputField.onValueChanged.AddListener((str) => SetStaticCameraPosition());
        positionYInputField.onValueChanged.AddListener((str) => SetStaticCameraPosition());
        positionZInputField.onValueChanged.AddListener((str) => SetStaticCameraPosition());

        rotationXInputField.onValueChanged.AddListener((str) => SetStaticCameraRotation());
        rotationYInputField.onValueChanged.AddListener((str) => SetStaticCameraRotation());
        rotationZInputField.onValueChanged.AddListener((str) => SetStaticCameraRotation());

        avatarToggle.onValueChanged.AddListener(OnAvatarToggleValueChanged);

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

        resetXROriginButton.onClick.RemoveListener(OnResetXROriginButtonPress);

        positionXInputField.onValueChanged.RemoveAllListeners();
        positionYInputField.onValueChanged.RemoveAllListeners();
        positionZInputField.onValueChanged.RemoveAllListeners();
                                           
        rotationXInputField.onValueChanged.RemoveAllListeners();
        rotationYInputField.onValueChanged.RemoveAllListeners();
        rotationZInputField.onValueChanged.RemoveAllListeners();

        avatarToggle.onValueChanged.RemoveListener(OnAvatarToggleValueChanged);

        ConfigFileManager.Instance.OnConfigLoaded -= OnConfigLoaded;
        ConfigFileManager.Instance.OnConfigSaved -= OnConfigSaved;
        ConfigFileManager.Instance.OnFileListRefreshed -= OnFileListRefreshed;
        SceneLoaderManager.Instance.OnSceneLoaded -= OnSceneLoaded;

        foreach (var entry in pointCloudEntries)
        {
            entry.OnDisplayToggleRequested -= OnDisplayToggleRequested;
        }
    }

    private void ClearPointCloudEntry()
    {

        while (pointCloudContainer.childCount > 0)
        {
            DestroyImmediate(pointCloudContainer.GetChild(0).gameObject);
        }
      
    }

    [ContextMenu("SpawnPointCloudEntries")]
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

            entry.OnDisplayToggleRequested += OnDisplayToggleRequested;

            pointCloudEntries.Add(entry);
        }
    }

    /// <summary>
    /// Handles exclusive toggle switching with a delay to allow texture initialization.
    /// </summary>
    private void OnDisplayToggleRequested(PointCloudUIEntry requestingEntry, bool desiredState)
    {
        if (isSwitching)
        {
            Debug.Log("[CanvasSetupPointCloudUI] Switch already in progress, ignoring request.");
            return;
        }

        // Turning off the currently active entry.
        if (!desiredState && requestingEntry == activeEntry)
        {
            StartCoroutine(SwitchPointCloudCoroutine(activeEntry, null));
            return;
        }

        // Turning on a new entry (exclusive: deactivate the current one first).
        if (desiredState && requestingEntry != activeEntry)
        {
            StartCoroutine(SwitchPointCloudCoroutine(activeEntry, requestingEntry));
        }
    }

    /// <summary>
    /// Coroutine that disables the previous point cloud, waits for the switch delay,
    /// then enables the next one.
    /// </summary>
    private IEnumerator SwitchPointCloudCoroutine(PointCloudUIEntry previous, PointCloudUIEntry next)
    {
        isSwitching = true;

        // Disable the previously active point cloud.
        if (previous != null)
        {
            previous.ApplyDisplayState(false);
        }

        activeEntry = null;

        if (next != null)
        {
            // Wait for textures / VFX to properly reset before activating the new camera.
            yield return new WaitForSeconds(switchDelay);

            next.ApplyDisplayState(true);
            activeEntry = next;
        }

        isSwitching = false;
    }

    private void OnButtonCloseClick()
    {
        ShortcutManager.Instance.EnableShortCut();
        SceneLoaderManager.Instance.LoadDefaultScene();
        OnCanvasSetupPointCloudUIDestroy.Invoke(this);
    }

    private void OnSaveButtonClick()
    {
        ConfigFileManager.Instance.Save();
        SetStatus($"Saved {selectedConfig}", Color.green);
    }

    private void OnLoadConfigButtonClick()
    {
        ConfigFileManager.Instance.Load(selectedConfig);
        SetStatus($"Loaded {selectedConfig}", Color.green);

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
            SetStatus("Please enter a config name.", Color.red);
            return;
        }

        ConfigFileManager.Instance.CreateNew(newConfigName);

        selectedConfig = newConfigName;


        foreach (var entry in pointCloudEntries)
        {
            entry.ResetToDefaults();    
            entry.SetInteractable(true);
        }

        foreach (var entry in pointCloudEntries)
            entry.ForceApplyAndSave();

        ConfigFileManager.Instance.Save();

        RefreshList();
        SetStatus($"Created {newConfigName}", Color.green);
    }

    private void OnConfigNameInputFieldSubmit(string value)
    {
        newConfigName = value.Trim();
    }

    private void RefreshList()
    {
        configs = ConfigFileManager.Instance.GetAvailableConfigs();

        if (configs.Count > 0)
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

    private void OnConfigLoaded(ConfigFile file, bool loadConfigIntoPointCloud)
    {
        for (int i = 0; i < pointCloudEntries.Count; i++)
        {
            if (i >= file.pointClouds.Count) break;

            var data = file.pointClouds[i];
            pointCloudEntries[i].SetPositionFields(data.position.ToVector3());
            pointCloudEntries[i].SetRotationFields(data.rotation.ToVector3());

            pointCloudEntries[i].SetMinDepth(data.depthMin);
            pointCloudEntries[i].SetMaxDepth(data.depthMax);
            pointCloudEntries[i].SetFlip(data.scale.x == -1, data.scale.y == -1);
        }
    }

    private void OnFileListRefreshed(List<string> list)
    {
    }

    private void OnResetXROriginButtonPress()
    {
        ResetXROrigin.Instance.ResetOrigin();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene)
    {
        StartCoroutine(WaitForKinectManagerInitialization());
    }

    public IEnumerator WaitForKinectManagerInitialization()
    {
        yield return new WaitUntil(() => KinectManager.Instance.IsInitialized());
        SpawnPointCloudEntries();
    }

    public IEnumerator LoadScene()
    {
        SetStatus("Loading config scene", Color.yellow);

        Fader.Instance.FadeToBlack();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        yield return SceneLoaderManager.Instance.LoadAsyncScene(scene);

        Fader.Instance.FadeToClear();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        SetStatus("Config scene loaded", Color.green);

        staticCamera = Instantiate(staticCameraPrefab).transform;

        SetStaticCameraInputField();
        avatarToggle.SetWithoutNotify(false);


    }

    private void SetStaticCameraInputField()
    {
        positionXInputField.SetTextWithoutNotify(staticCamera.position.x.ToString());
        positionYInputField.SetTextWithoutNotify(staticCamera.position.y.ToString());
        positionZInputField.SetTextWithoutNotify(staticCamera.position.z.ToString());

        rotationXInputField.SetTextWithoutNotify(staticCamera.rotation.eulerAngles.x.ToString());
        rotationYInputField.SetTextWithoutNotify(staticCamera.rotation.eulerAngles.y.ToString());
        rotationZInputField.SetTextWithoutNotify(staticCamera.rotation.eulerAngles.z.ToString());
    }

    private void SetStaticCameraPosition()
    {
        staticCamera.position = new Vector3(float.Parse(positionXInputField.text), float.Parse(positionYInputField.text), float.Parse(positionZInputField.text));
    }

    private void SetStaticCameraRotation()
    {
        staticCamera.rotation = Quaternion.Euler(float.Parse(rotationXInputField.text), float.Parse(rotationYInputField.text), float.Parse(rotationZInputField.text));
    }

    private void OnAvatarToggleValueChanged(bool value)
    {
        PlayerManager.Instance.DisplayAvatar(value);
    }

    private void SetStatus(string message, Color color)
    {
        Debug.Log($"[CanvasSetupPointCloudUI] {message}");
        if (statusText)
        {
            statusText.text = statusLocalizedText.GetLocalizedString(message);
        }

        if(dotImage)
        {
            dotImage.color = color;
        }
    }
}