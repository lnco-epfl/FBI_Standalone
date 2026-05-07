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


    [Header("Canva UI")]
    [SerializeField] private UISwitcher.UISwitcher displayCanvaUIToggle;

    [SerializeField] private Button canvaUIColorPickerButton;
    [SerializeField] private Image canvaUIAlphaPreview;

    [SerializeField] private TMP_InputField canvaUIPositionXInputField;
    [SerializeField] private TMP_InputField canvaUIPositionYInputField;
    [SerializeField] private TMP_InputField canvaUIPositionZInputField;
    [SerializeField] private TMP_InputField canvaUIRotationXInputField;
    [SerializeField] private TMP_InputField canvaUIRotationYInputField;
    [SerializeField] private TMP_InputField canvaUIRotationZInputField;

    [SerializeField] private GameObject flexibleColorPickerPrefab;
    private FlexibleColorPicker flexibleColorPicker;

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

        displayCanvaUIToggle.onValueChanged.AddListener(OnDisplayCanvaUIValueChanged);

        canvaUIColorPickerButton.onClick.AddListener(OnCanvasUIColorPickerButtonPress);

        canvaUIPositionXInputField.onValueChanged.AddListener((str) => SetWorldUIPosition());
        canvaUIPositionYInputField.onValueChanged.AddListener((str) => SetWorldUIPosition());
        canvaUIPositionZInputField.onValueChanged.AddListener((str) => SetWorldUIPosition());
        canvaUIRotationXInputField.onValueChanged.AddListener((str) => SetWorldUIRotation());
        canvaUIRotationYInputField.onValueChanged.AddListener((str) => SetWorldUIRotation());
        canvaUIRotationZInputField.onValueChanged.AddListener((str) => SetWorldUIRotation());

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

        displayCanvaUIToggle.onValueChanged.AddListener(OnDisplayCanvaUIValueChanged);

        canvaUIColorPickerButton.onClick.AddListener(OnCanvasUIColorPickerButtonPress);

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

        if (file.UICanvas != null)
        {
            file.UICanvas.ApplyTo(WorldUIManager.Instance.transform);

            var color = file.UICanvas.backgroundColor?.ToColor() ?? Color.black;
            WorldUIManager.Instance.SetCurrentBackgoundColor(color);
            WorldUIManager.Instance.BackgroundColor = color;
            canvaUIAlphaPreview.color = color;

            SetWorldUIInputField(); 
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
        SetWorldUIInputField();

        canvaUIAlphaPreview.color = WorldUIManager.Instance.BackgroundColor;

        avatarToggle.SetWithoutNotify(false);
        displayCanvaUIToggle.SetWithoutNotify(false);

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

    private void SetWorldUIInputField()
    {
        canvaUIPositionXInputField.SetTextWithoutNotify(WorldUIManager.Instance.Position.x.ToString());
        canvaUIPositionYInputField.SetTextWithoutNotify(WorldUIManager.Instance.Position.y.ToString());
        canvaUIPositionZInputField.SetTextWithoutNotify(WorldUIManager.Instance.Position.z.ToString());

        canvaUIRotationXInputField.SetTextWithoutNotify(WorldUIManager.Instance.Rotation.x.ToString());
        canvaUIRotationYInputField.SetTextWithoutNotify(WorldUIManager.Instance.Rotation.y.ToString());
        canvaUIRotationZInputField.SetTextWithoutNotify(WorldUIManager.Instance.Rotation.z.ToString());
    }

    private void SetWorldUIPosition()
    {
        WorldUIManager.Instance.Position = new Vector3(
            float.Parse(canvaUIPositionXInputField.text),
            float.Parse(canvaUIPositionYInputField.text),
            float.Parse(canvaUIPositionZInputField.text)
        );
        ConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
    }

    private void SetWorldUIRotation()
    {
        WorldUIManager.Instance.Rotation = new Vector3(
            float.Parse(canvaUIRotationXInputField.text),
            float.Parse(canvaUIRotationYInputField.text),
            float.Parse(canvaUIRotationZInputField.text)
        );
        ConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
    }

    private void OnDisplayCanvaUIValueChanged(bool value)
    {
        if(value)
        {
            WorldUIManager.Instance.DisplayText("Canva UI");
        }
        else
        {
            WorldUIManager.Instance.HideText();
        }
        
    }

    private void OnCanvasUIColorPickerButtonPress()
    {

        if(flexibleColorPicker == null)
        {
            var gameObject = GameObject.Instantiate(flexibleColorPickerPrefab, this.transform);

            flexibleColorPicker = gameObject.GetComponent<FlexibleColorPicker>();
            flexibleColorPicker.color = WorldUIManager.Instance.BackgroundColor;

            DestroyOnButtonClick destroyer = gameObject.GetComponent<DestroyOnButtonClick>();
            destroyer.OnBeforeDestroy += HandleBeforeDestroy;

            flexibleColorPicker.onColorChange.AddListener(OnCanvasUIColorChanged);
        }

    }

    private void HandleBeforeDestroy()
    {
        flexibleColorPicker = null;
    }

    private void OnCanvasUIColorChanged(Color color)
    {
        canvaUIAlphaPreview.color = color;
        WorldUIManager.Instance.SetCurrentBackgoundColor(color);
        ConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
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