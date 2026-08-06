using com.rfilkov.kinect;
using Eflatun.SceneReference;
using Intel.RealSense;
using PrimeTween;
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
using static com.rfilkov.kinect.Kinect4AzureInterface;
using static UnityEngine.EventSystems.EventTrigger;


public class CanvasSetupPointCloudUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text title;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text currentFileNameText;
    [SerializeField] private LocalizedString statusLocalizedText;

    [Header("Toolbar - File (Camera config)")]
    [SerializeField] private Button newConfigButton;
    [SerializeField] private Button openConfigButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button saveAsButton;

    [Header("Toolbar - Clipboard (Hamburger, Camera config)")]
    [SerializeField] private HamburgerMenu hamburgerMenu;

    [Header("Tabs")]
    [SerializeField] private Button camerasTabButton;
    [SerializeField] private Button stimulusDisplayTabButton;
    [SerializeField] private GameObject camerasTabRoot;
    [SerializeField] private GameObject stimulusDisplayTabRoot;

    [Header("Toolbar - File (Display config)")]
    [SerializeField] private Button newDisplayConfigButton;
    [SerializeField] private Button openDisplayConfigButton;
    [SerializeField] private Button saveDisplayConfigButton;
    [SerializeField] private Button saveAsDisplayConfigButton;
    [SerializeField] private TMP_Text displayConfigStatusText;
    [SerializeField] private TMP_Text currentDisplayConfigFileNameText;
    [SerializeField] private LocalizedString displayConfigStatusLocalizedText;

    [Header("Toolbar - Clipboard (Hamburger, Display config)")]
    [SerializeField] private HamburgerMenu displayConfigHamburgerMenu;

    [Header("Point cloud")]
    [SerializeField] private GameObject pointCloudEntryPrefab;
    [SerializeField] private Transform pointCloudContainer;

    [Header("Point Cloud Switch")]
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
    [SerializeField] private TMP_Dropdown sceneDropdown;
    [SerializeField] private List<SceneReference> availableScenes = new List<SceneReference>();

    [Header("World Space Canvas")]
    [SerializeField] private GameObject worldSpaceCanvasPrefab;
    [SerializeField] private UISwitcher.UISwitcher worldCanvasEditToggle;

    private WorldSpacePointCloudUI worldSpaceUI;
    private PointCloudUIBridge bridge;

    public event Action<CanvasSetupPointCloudUI> OnCanvasSetupPointCloudUIDestroy;

    private string selectedConfig;
    private string selectedDisplayConfig;
    private int selectedSceneIndex = 0;

    private List<PointCloudUIEntry> pointCloudEntries = new List<PointCloudUIEntry>();

    /// <summary>The entry whose point cloud is currently active (visible). Null if none.</summary>
    private PointCloudUIEntry activeEntry;

    /// <summary>Prevents the user from switching while a switch transition is already in progress.</summary>
    private bool isSwitching;


    private void Awake()
    {
        avatarToggle = avatarToggleGameObject.GetComponent<UISwitcher.UISwitcher>();
    }

    private void Start()
    {
        StartCoroutine(LoadScene());
        ClearPointCloudEntry();
        InitSceneDropdown();
        UpdateFileNameDisplay();
        UpdateDisplayConfigFileNameDisplay();
        SwitchTab(showCameras: true);

        ShortcutManager.Instance.DisableMainShortCut();

    }

    private void OnEnable()
    {
        closeButton.onClick.AddListener(OnButtonCloseClick);

        newConfigButton.onClick.AddListener(OnNewConfigButtonClick);
        openConfigButton.onClick.AddListener(OnOpenConfigButtonClick);
        saveButton.onClick.AddListener(OnSaveButtonClick);
        saveAsButton.onClick.AddListener(OnSaveAsButtonClick);

        hamburgerMenu.OnCopyConfigClicked += OnCopyConfigButtonClick;
        hamburgerMenu.OnPasteConfigClicked += OnPasteConfigButtonClick;

        resetXROriginButton.onClick.AddListener(OnResetXROriginButtonPress);

        positionXInputField.onValueChanged.AddListener((str) => SetStaticCameraPosition());
        positionYInputField.onValueChanged.AddListener((str) => SetStaticCameraPosition());
        positionZInputField.onValueChanged.AddListener((str) => SetStaticCameraPosition());

        rotationXInputField.onValueChanged.AddListener((str) => SetStaticCameraRotation());
        rotationYInputField.onValueChanged.AddListener((str) => SetStaticCameraRotation());
        rotationZInputField.onValueChanged.AddListener((str) => SetStaticCameraRotation());

        avatarToggle.onValueChanged.AddListener(OnAvatarToggleValueChanged);

        worldCanvasEditToggle.onValueChanged.AddListener(OnWorldCanvasEditToggleChanged);

        displayCanvaUIToggle.onValueChanged.AddListener(OnDisplayCanvaUIValueChanged);
        canvaUIColorPickerButton.onClick.AddListener(OnCanvasUIColorPickerButtonPress);

        canvaUIPositionXInputField.onValueChanged.AddListener((str) => SetWorldUIPosition());
        canvaUIPositionYInputField.onValueChanged.AddListener((str) => SetWorldUIPosition());
        canvaUIPositionZInputField.onValueChanged.AddListener((str) => SetWorldUIPosition());
        canvaUIRotationXInputField.onValueChanged.AddListener((str) => SetWorldUIRotation());
        canvaUIRotationYInputField.onValueChanged.AddListener((str) => SetWorldUIRotation());
        canvaUIRotationZInputField.onValueChanged.AddListener((str) => SetWorldUIRotation());

        CameraConfigFileManager.Instance.OnConfigLoaded += OnConfigLoaded;
        CameraConfigFileManager.Instance.OnConfigSaved += OnConfigSaved;
        SceneLoaderManager.Instance.OnSceneLoaded += OnSceneLoaded;

        sceneDropdown.onValueChanged.AddListener(OnSceneDropdownValueChanged);

        camerasTabButton.onClick.AddListener(() => SwitchTab(showCameras: true));
        stimulusDisplayTabButton.onClick.AddListener(() => SwitchTab(showCameras: false));

        newDisplayConfigButton.onClick.AddListener(OnNewDisplayConfigButtonClick);
        openDisplayConfigButton.onClick.AddListener(OnOpenDisplayConfigButtonClick);
        saveDisplayConfigButton.onClick.AddListener(OnSaveDisplayConfigButtonClick);
        saveAsDisplayConfigButton.onClick.AddListener(OnSaveAsDisplayConfigButtonClick);

        displayConfigHamburgerMenu.OnCopyConfigClicked += OnCopyDisplayConfigButtonClick;
        displayConfigHamburgerMenu.OnPasteConfigClicked += OnPasteDisplayConfigButtonClick;

        DisplayConfigFileManager.Instance.OnConfigLoaded += OnDisplayConfigLoaded;
        DisplayConfigFileManager.Instance.OnConfigSaved += OnDisplayConfigSaved;
    }

    private void OnDisable()
    {
        closeButton.onClick.RemoveListener(OnButtonCloseClick);

        newConfigButton.onClick.RemoveListener(OnNewConfigButtonClick);
        openConfigButton.onClick.RemoveListener(OnOpenConfigButtonClick);
        saveButton.onClick.RemoveListener(OnSaveButtonClick);
        saveAsButton.onClick.RemoveListener(OnSaveAsButtonClick);

        hamburgerMenu.OnCopyConfigClicked -= OnCopyConfigButtonClick;
        hamburgerMenu.OnPasteConfigClicked -= OnPasteConfigButtonClick;

        resetXROriginButton.onClick.RemoveListener(OnResetXROriginButtonPress);

        positionXInputField.onValueChanged.RemoveAllListeners();
        positionYInputField.onValueChanged.RemoveAllListeners();
        positionZInputField.onValueChanged.RemoveAllListeners();

        rotationXInputField.onValueChanged.RemoveAllListeners();
        rotationYInputField.onValueChanged.RemoveAllListeners();
        rotationZInputField.onValueChanged.RemoveAllListeners();

        avatarToggle.onValueChanged.RemoveListener(OnAvatarToggleValueChanged);

        worldCanvasEditToggle.onValueChanged.RemoveListener(OnWorldCanvasEditToggleChanged);

        displayCanvaUIToggle.onValueChanged.RemoveListener(OnDisplayCanvaUIValueChanged);
        canvaUIColorPickerButton.onClick.RemoveListener(OnCanvasUIColorPickerButtonPress);

        canvaUIPositionXInputField.onValueChanged.RemoveAllListeners();
        canvaUIPositionYInputField.onValueChanged.RemoveAllListeners();
        canvaUIPositionZInputField.onValueChanged.RemoveAllListeners();
        canvaUIRotationXInputField.onValueChanged.RemoveAllListeners();
        canvaUIRotationYInputField.onValueChanged.RemoveAllListeners();
        canvaUIRotationZInputField.onValueChanged.RemoveAllListeners();

        CameraConfigFileManager.Instance.OnConfigLoaded -= OnConfigLoaded;
        CameraConfigFileManager.Instance.OnConfigSaved -= OnConfigSaved;
        SceneLoaderManager.Instance.OnSceneLoaded -= OnSceneLoaded;

        sceneDropdown.onValueChanged.RemoveListener(OnSceneDropdownValueChanged);

        camerasTabButton.onClick.RemoveAllListeners();
        stimulusDisplayTabButton.onClick.RemoveAllListeners();

        newDisplayConfigButton.onClick.RemoveListener(OnNewDisplayConfigButtonClick);
        openDisplayConfigButton.onClick.RemoveListener(OnOpenDisplayConfigButtonClick);
        saveDisplayConfigButton.onClick.RemoveListener(OnSaveDisplayConfigButtonClick);
        saveAsDisplayConfigButton.onClick.RemoveListener(OnSaveAsDisplayConfigButtonClick);

        displayConfigHamburgerMenu.OnCopyConfigClicked -= OnCopyDisplayConfigButtonClick;
        displayConfigHamburgerMenu.OnPasteConfigClicked -= OnPasteDisplayConfigButtonClick;

        DisplayConfigFileManager.Instance.OnConfigLoaded -= OnDisplayConfigLoaded;
        DisplayConfigFileManager.Instance.OnConfigSaved -= OnDisplayConfigSaved;

        foreach (var entry in pointCloudEntries)
            entry.OnDisplayToggleRequested -= OnDisplayToggleRequested;
    }

    private void OnDestroy()
    {
        if (worldSpaceUI != null) Destroy(worldSpaceUI.gameObject);
        if (bridge != null) Destroy(bridge.gameObject);
    }


    private void SpawnWorldSpaceCanvas()
    {
        if (worldSpaceUI != null)
        {
            Destroy(worldSpaceUI.gameObject);
            worldSpaceUI = null;
        }
        if (bridge != null)
        {
            Destroy(bridge.gameObject);
            bridge = null;
        }

        if (worldSpaceCanvasPrefab == null)
        {
            Debug.LogWarning("[CanvasSetupPointCloudUI] worldSpaceCanvasPrefab non assigné — canvas WS ignoré.");
            return;
        }

        var worldSpaceCanvas = Instantiate(worldSpaceCanvasPrefab);
        worldSpaceCanvas.name = "WorldSpacePointCloudCanvas";

        worldSpaceUI = worldSpaceCanvas.GetComponent<WorldSpacePointCloudUI>();

        ResetWorldSpaceCanvasPositionAndRotation();

        if (worldSpaceUI == null)
        {
            Debug.LogError("[CanvasSetupPointCloudUI] Prefab is missing WorldSpacePointCloudUI component!");
            Destroy(worldSpaceCanvas);
            return;
        }

        var bridgeGo = new GameObject("PointCloudUIBridge");

        bridge = bridgeGo.AddComponent<PointCloudUIBridge>();
        //bridge.Initialize(this, worldSpaceUI, switchDelay);

        worldSpaceUI.SetBridge(bridge);
        worldSpaceUI.gameObject.SetActive(false);

        if (worldCanvasEditToggle)
        {
            worldCanvasEditToggle.SetWithoutNotify(false);
        }
    }

    private void ResetWorldSpaceCanvasPositionAndRotation()
    {
        worldSpaceUI.transform.position = new Vector3(-1.0f, 1.0f, 1.0f);
        worldSpaceUI.transform.rotation = Quaternion.Euler(0.0f, -45.0f, 0.0f);
    }

    private void OnNewConfigButtonClick()
    {
        FileBrowserService.SaveFile("New config", CameraConfigFileManager.Instance.GetRootPath(), "new_config", "yaml", (paths) =>
        {
            if (string.IsNullOrEmpty(paths))
            {
                SetStatus("New config cancelled.", Color.grey);
                bridge?.MirrorStatus("New config cancelled.", Color.grey);
                return;
            }

            string configName = System.IO.Path.GetFileNameWithoutExtension(paths);
            CameraConfigFileManager.Instance.CreateNew(configName);

            foreach (var entry in pointCloudEntries)
            {
                entry.ResetToDefaults();
                entry.SetInteractable(true);
            }

            foreach (var entry in pointCloudEntries)
                entry.ForceApplyAndSave();

            bool saved = CameraConfigFileManager.Instance.SaveAs(paths);

            if (saved)
            {
                selectedConfig = configName;
                UpdateFileNameDisplay();
                SetStatus($"Created {configName}", Color.green);
                bridge?.MirrorFileName(configName);
                bridge?.MirrorStatus($"Created {configName}", Color.green);
                bridge?.MirrorEntryInteractable(true);
            }
            else
            {
                SetStatus("Failed to save file.", Color.red);
                bridge?.MirrorStatus("Failed to save file.", Color.red);
            }
        });


    }

    private void OnOpenConfigButtonClick()
    {
        FileBrowserService.OpenFile("Open config", CameraConfigFileManager.Instance.GetRootPath(), "yaml", (path) =>
        {
            if (path == null || path.Length == 0 || string.IsNullOrEmpty(path))
            {
                SetStatus("Open cancelled.", Color.grey);
                bridge?.MirrorStatus("Open cancelled.", Color.grey);
                return;
            }

            var config = CameraConfigFileManager.Instance.LoadFromPath(path);

            if (config != null)
            {
                selectedConfig = config.configName;
                UpdateFileNameDisplay();

                foreach (var entry in pointCloudEntries)
                    entry.SetInteractable(true);

                SetStatus($"Opened {selectedConfig}", Color.green);
                bridge?.MirrorFileName(selectedConfig);
                bridge?.MirrorStatus($"Opened {selectedConfig}", Color.green);
                bridge?.MirrorEntryInteractable(true);


            }
            else
            {
                SetStatus("Failed to read file.", Color.red);
                bridge?.MirrorStatus("Failed to read file.", Color.red);
            }
        });


    }

    private void OnSaveButtonClick()
    {
        if (CameraConfigFileManager.Instance.CurrentConfig == null)
        {
            SetStatus("No config loaded.", Color.grey);
            bridge?.MirrorStatus("No config loaded.", Color.grey);
            return;
        }

        bool saved = CameraConfigFileManager.Instance.Save();
        string msg = saved ? $"Saved {selectedConfig}" : "Failed to save file.";
        Color col = saved ? Color.green : Color.red;
        SetStatus(msg, col);
        bridge?.MirrorStatus(msg, col);
    }

    private void OnSaveAsButtonClick()
    {
        if (CameraConfigFileManager.Instance.CurrentConfig == null)
        {
            SetStatus("No config loaded.", Color.grey);
            bridge?.MirrorStatus("No config loaded.", Color.grey);
            return;
        }

        string defaultName = selectedConfig ?? "config";
        FileBrowserService.SaveFile("Save config as", CameraConfigFileManager.Instance.GetRootPath(), defaultName, "yaml", (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("Save cancelled.", Color.grey);
                bridge?.MirrorStatus("Save cancelled.", Color.grey);
                return;
            }

            bool saved = CameraConfigFileManager.Instance.SaveAs(path);

            if (saved)
            {
                selectedConfig = System.IO.Path.GetFileNameWithoutExtension(path);
                UpdateFileNameDisplay();
                SetStatus($"Saved as {selectedConfig}", Color.green);
                bridge?.MirrorFileName(selectedConfig);
                bridge?.MirrorStatus($"Saved as {selectedConfig}", Color.green);
            }
            else
            {
                SetStatus("Failed to save file.", Color.red);
                bridge?.MirrorStatus("Failed to save file.", Color.red);
            }
        });


    }


    private void OnCopyConfigButtonClick()
    {
        bool copied = CameraConfigFileManager.Instance.CopyToClipboard();
        string msg = copied ? "Config copied to clipboard." : "No config loaded.";
        Color col = copied ? Color.green : Color.grey;
        SetStatus(msg, col);
        bridge?.MirrorStatus(msg, col);
    }

    private void OnPasteConfigButtonClick()
    {
        if (string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
        {
            SetStatus("Clipboard is empty.", Color.red);
            bridge?.MirrorStatus("Clipboard is empty.", Color.red);
            return;
        }

        var config = CameraConfigFileManager.Instance.PasteFromClipboard();

        if (config != null)
        {
            selectedConfig = config.configName;
            UpdateFileNameDisplay();

            foreach (var entry in pointCloudEntries)
                entry.SetInteractable(true);

            SetStatus("Config pasted from clipboard.", Color.green);
            bridge?.MirrorFileName(selectedConfig);
            bridge?.MirrorStatus("Config pasted from clipboard.", Color.green);
            bridge?.MirrorEntryInteractable(true);
        }
        else
        {
            SetStatus("Clipboard content is invalid.", Color.red);
            bridge?.MirrorStatus("Clipboard content is invalid.", Color.red);
        }
    }


    private void UpdateFileNameDisplay()
    {
        if (currentFileNameText == null) return;
        currentFileNameText.text = string.IsNullOrEmpty(selectedConfig) ? "" : selectedConfig;
    }

    private void SwitchTab(bool showCameras)
    {
        if (camerasTabRoot != null) camerasTabRoot.SetActive(showCameras);
        if (stimulusDisplayTabRoot != null) stimulusDisplayTabRoot.SetActive(!showCameras);
    }

    private void OnNewDisplayConfigButtonClick()
    {
        FileBrowserService.SaveFile("New display config", DisplayConfigFileManager.Instance.GetRootPath(), "new_display_config", "yaml", (paths) =>
        {
            if (string.IsNullOrEmpty(paths))
            {
                SetDisplayConfigStatus("New config cancelled.", Color.grey);
                return;
            }

            string configName = System.IO.Path.GetFileNameWithoutExtension(paths);
            DisplayConfigFileManager.Instance.CreateNew(configName);
            DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor, saveImmediately: false);

            bool saved = DisplayConfigFileManager.Instance.SaveAs(paths);

            if (saved)
            {
                selectedDisplayConfig = configName;
                UpdateDisplayConfigFileNameDisplay();
                SetDisplayConfigStatus($"Created {configName}", Color.green);
            }
            else
            {
                SetDisplayConfigStatus("Failed to save file.", Color.red);
            }
        });
    }

    private void OnOpenDisplayConfigButtonClick()
    {
        FileBrowserService.OpenFile("Open display config", DisplayConfigFileManager.Instance.GetRootPath(), "yaml", (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                SetDisplayConfigStatus("Open cancelled.", Color.grey);
                return;
            }

            var config = DisplayConfigFileManager.Instance.LoadFromPath(path);

            if (config != null)
            {
                selectedDisplayConfig = config.configName;
                UpdateDisplayConfigFileNameDisplay();
                SetDisplayConfigStatus($"Opened {selectedDisplayConfig}", Color.green);
            }
            else
            {
                SetDisplayConfigStatus("Failed to read file.", Color.red);
            }
        });
    }

    private void OnSaveDisplayConfigButtonClick()
    {
        if (DisplayConfigFileManager.Instance.CurrentConfig == null)
        {
            SetDisplayConfigStatus("No config loaded.", Color.grey);
            return;
        }

        bool saved = DisplayConfigFileManager.Instance.Save();
        string msg = saved ? $"Saved {selectedDisplayConfig}" : "Failed to save file.";
        Color col = saved ? Color.green : Color.red;
        SetDisplayConfigStatus(msg, col);
    }

    private void OnSaveAsDisplayConfigButtonClick()
    {
        if (DisplayConfigFileManager.Instance.CurrentConfig == null)
        {
            SetDisplayConfigStatus("No config loaded.", Color.grey);
            return;
        }

        string defaultName = selectedDisplayConfig ?? "display_config";
        FileBrowserService.SaveFile("Save display config as", DisplayConfigFileManager.Instance.GetRootPath(), defaultName, "yaml", (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                SetDisplayConfigStatus("Save cancelled.", Color.grey);
                return;
            }

            bool saved = DisplayConfigFileManager.Instance.SaveAs(path);

            if (saved)
            {
                selectedDisplayConfig = System.IO.Path.GetFileNameWithoutExtension(path);
                UpdateDisplayConfigFileNameDisplay();
                SetDisplayConfigStatus($"Saved as {selectedDisplayConfig}", Color.green);
            }
            else
            {
                SetDisplayConfigStatus("Failed to save file.", Color.red);
            }
        });
    }

    private void OnCopyDisplayConfigButtonClick()
    {
        bool copied = DisplayConfigFileManager.Instance.CopyToClipboard();
        string msg = copied ? "Config copied to clipboard." : "No config loaded.";
        Color col = copied ? Color.green : Color.grey;
        SetDisplayConfigStatus(msg, col);
    }

    private void OnPasteDisplayConfigButtonClick()
    {
        if (string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
        {
            SetDisplayConfigStatus("Clipboard is empty.", Color.red);
            return;
        }

        var config = DisplayConfigFileManager.Instance.PasteFromClipboard();

        if (config != null)
        {
            selectedDisplayConfig = config.configName;
            UpdateDisplayConfigFileNameDisplay();
            SetDisplayConfigStatus("Config pasted from clipboard.", Color.green);
        }
        else
        {
            SetDisplayConfigStatus("Clipboard content is invalid.", Color.red);
        }
    }

    private void UpdateDisplayConfigFileNameDisplay()
    {
        if (currentDisplayConfigFileNameText == null) return;
        currentDisplayConfigFileNameText.text = string.IsNullOrEmpty(selectedDisplayConfig) ? "" : selectedDisplayConfig;
    }

    private void SetDisplayConfigStatus(string message, Color color)
    {
        Debug.Log($"[CanvasSetupPointCloudUI] {message}");
        if (displayConfigStatusText)
        {
            displayConfigStatusText.text = displayConfigStatusLocalizedText.GetLocalizedString("• " + message);
            displayConfigStatusText.color = color;
        }
    }

    private void ClearPointCloudEntry()
    {
        foreach (var entry in pointCloudEntries)
        {
            entry.OnDisplayToggleRequested -= OnDisplayToggleRequested;
            entry.OnTransformChanged -= OnEntryTransformChanged;
            entry.OnMaxDepthChanged -= OnEntryMaxDepthChanged;
            entry.OnMinDepthChanged -= OnEntryMinDepthChanged;
            entry.OnFlipChanged -= OnEntryFlipChanged;
            entry.OnClampChanged -= OnEntryClampChanged;
        }

        pointCloudEntries.Clear();
        activeEntry = null;

        while (pointCloudContainer.childCount > 0)
            DestroyImmediate(pointCloudContainer.GetChild(0).gameObject);
    }

    [ContextMenu("SpawnPointCloudEntries")]
    private void SpawnPointCloudEntries()
    {
        var cameraIds = new List<int>() { 1, 2 };

        foreach (var id in cameraIds)
        {
            PointCloudManager.Instance.SpawnPointCloud(id, 0.0f, null);

            var go = Instantiate(pointCloudEntryPrefab, pointCloudContainer);
            go.name = $"PointCloudEntry_Camera{id}";

            var entry = go.GetComponent<PointCloudUIEntry>();
            entry.Init(id);
            entry.SetInteractable(false);
            entry.SetDisplayToggle(false);

            entry.OnDisplayToggleRequested += OnDisplayToggleRequested;
            entry.OnTransformChanged += OnEntryTransformChanged;
            entry.OnMaxDepthChanged += OnEntryMaxDepthChanged;
            entry.OnMinDepthChanged += OnEntryMinDepthChanged;
            entry.OnFlipChanged += OnEntryFlipChanged;
            entry.OnClampChanged += OnEntryClampChanged;
            pointCloudEntries.Add(entry);
        }

        // Spawner les entrées WS et les pairer
        if (worldSpaceUI != null)
        {
            worldSpaceUI.SpawnPointCloudEntries(cameraIds);
            bridge?.PairEntries(pointCloudEntries, worldSpaceUI.GetEntries());
        }
    }

    private void OnDisplayToggleRequested(PointCloudUIEntry requestingEntry, bool desiredState)
    {
        if (isSwitching)
        {
            Debug.Log("[CanvasSetupPointCloudUI] Switch already in progress, ignoring request.");
            return;
        }

        if (!desiredState && requestingEntry == activeEntry)
        {
            StartCoroutine(SwitchPointCloudCoroutine(activeEntry, null));
            return;
        }

        if (desiredState && requestingEntry != activeEntry)
            StartCoroutine(SwitchPointCloudCoroutine(activeEntry, requestingEntry));
    }

    private void OnEntryTransformChanged(int cameraId, Vector3 pos, Vector3 rot)
    {
        int i = pointCloudEntries.FindIndex(e => e.CameraId == cameraId);
        if (i < 0) return;
        var e = pointCloudEntries[i];
        bridge?.MirrorEntryData(i, pos, rot, e.DepthMin, e.DepthMax, e.FlipX, e.FlipY);
    }

    private void OnEntryMaxDepthChanged(int cameraId, float value)
    {
        int i = pointCloudEntries.FindIndex(e => e.CameraId == cameraId);
        if (i < 0) return;
        var e = pointCloudEntries[i];
        bridge?.MirrorEntryData(i, e.Position, e.Rotation, e.DepthMin, value, e.FlipX, e.FlipY);
    }

    private void OnEntryMinDepthChanged(int cameraId, float value)
    {
        int i = pointCloudEntries.FindIndex(e => e.CameraId == cameraId);
        if (i < 0) return;
        var e = pointCloudEntries[i];
        bridge?.MirrorEntryData(i, e.Position, e.Rotation, value, e.DepthMax, e.FlipX, e.FlipY);
    }

    private void OnEntryFlipChanged(int cameraId, bool flipX, bool flipY)
    {
        int i = pointCloudEntries.FindIndex(e => e.CameraId == cameraId);
        if (i < 0) return;
        var e = pointCloudEntries[i];
        bridge?.MirrorEntryData(i, e.Position, e.Rotation, e.DepthMin, e.DepthMax, flipX, flipY);
    }

    private void OnEntryClampChanged(int cameraId, float xMin, float xMax, float yMin, float yMax)
    {
        int i = pointCloudEntries.FindIndex(e => e.CameraId == cameraId);
        if (i < 0) return;
        bridge?.MirrorEntryClamp(i, xMin, xMax, yMin, yMax);
    }
    private IEnumerator SwitchPointCloudCoroutine(PointCloudUIEntry previous, PointCloudUIEntry next)
    {
        isSwitching = true;

        if (previous != null)
        {
            previous.ApplyDisplayState(false);
            bridge?.MirrorEntryDisplayState(previous.CameraId, false);
        }

        activeEntry = null;

        if (next != null)
        {
            yield return new WaitForSeconds(switchDelay);
            next.ApplyDisplayState(true);
            bridge?.MirrorEntryDisplayState(next.CameraId, true);
            activeEntry = next;
        }

        isSwitching = false;
    }

    private void OnButtonCloseClick()
    {
        ShortcutManager.Instance.EnableMainShortCut();
        SceneLoaderManager.Instance.LoadDefaultScene();
        OnCanvasSetupPointCloudUIDestroy.Invoke(this);
    }

    private void OnConfigSaved(CameraConfigFile file) { }

    private void OnConfigLoaded(CameraConfigFile file)
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
            pointCloudEntries[i].SetClamp(data.clampXMin, data.clampXMax, data.clampYMin, data.clampYMax);


            bridge?.MirrorEntryData(i,
                data.position.ToVector3(),
                data.rotation.ToVector3(),
                data.depthMin,
                data.depthMax,
                data.scale.x == -1,
                data.scale.y == -1);

            bridge?.MirrorEntryClamp(i, data.clampXMin, data.clampXMax, data.clampYMin, data.clampYMax);

            PointCloudManager.Instance.SetPointcloudConfig(PointCloudManager.Instance.GetPointCloud(pointCloudEntries[i].CameraId), file);
        }
    }

    private void OnDisplayConfigSaved(DisplayConfigFile file) { }

    private void OnDisplayConfigLoaded(DisplayConfigFile file)
    {
        if (file.stimulusDisplay == null) return;

        file.stimulusDisplay.ApplyTo(WorldUIManager.Instance.transform);

        var color = file.stimulusDisplay.backgroundColor?.ToColor() ?? Color.black;
        WorldUIManager.Instance.SetCurrentBackgoundColor(color);
        WorldUIManager.Instance.BackgroundColor = color;
        canvaUIAlphaPreview.color = color;

        SetWorldUIInputField();

        selectedDisplayConfig = file.configName;
        UpdateDisplayConfigFileNameDisplay();

        bridge?.MirrorCanvasUIPosition(WorldUIManager.Instance.Position);
        bridge?.MirrorCanvasUIRotation(WorldUIManager.Instance.Rotation);
        bridge?.MirrorCanvasUIColor(color);
    }

    private void OnResetXROriginButtonPress()
    {
        ResetXROrigin.Instance.ResetOrigin();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene)
    {
        /*ClearPointCloudEntry();
        SpawnPointCloudEntries();

        foreach (var entry in pointCloudEntries)
            entry.SetInteractable(true);

        bridge?.MirrorEntryInteractable(true);*/
    }

    public IEnumerator LoadScene()
    {
        SetStatus("Loading scene, please wait", Color.yellow);
        bridge?.MirrorStatus("Loading scene, please wait", Color.yellow);

        Fader.Instance.FadeToBlack();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        yield return SceneLoaderManager.Instance.LoadAsyncScene(scene);

        Fader.Instance.FadeToClear();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        SetStatus("Scene loaded", Color.green);

        staticCamera = Instantiate(staticCameraPrefab).transform;

        SetStaticCameraInputField();
        SetWorldUIInputField();

        canvaUIAlphaPreview.color = WorldUIManager.Instance.BackgroundColor;

        SpawnWorldSpaceCanvas();

        ClearPointCloudEntry();
        SpawnPointCloudEntries();

        foreach (var entry in pointCloudEntries)
            entry.SetInteractable(true);

        bridge?.MirrorEntryInteractable(true);

        avatarToggle.SetWithoutNotify(false);

        displayCanvaUIToggle.SetWithoutNotify(false);

        bridge?.MirrorStatus("Scene loaded", Color.green);
        bridge?.MirrorDisplayCanvasUIToggle(false);
        bridge?.MirrorCanvasUIPosition(WorldUIManager.Instance.Position);
        bridge?.MirrorCanvasUIRotation(WorldUIManager.Instance.Rotation);
        bridge?.MirrorCanvasUIColor(WorldUIManager.Instance.BackgroundColor);

        Tween.Delay(1.0f, () => SetStatus("No config loaded.", Color.grey));
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
        if (staticCamera == null) return;
        staticCamera.position = new Vector3(
            float.Parse(positionXInputField.text),
            float.Parse(positionYInputField.text),
            float.Parse(positionZInputField.text));
    }

    private void SetStaticCameraRotation()
    {
        if (staticCamera == null) return;
        staticCamera.rotation = Quaternion.Euler(
            float.Parse(rotationXInputField.text),
            float.Parse(rotationYInputField.text),
            float.Parse(rotationZInputField.text));
    }


    private void OnAvatarToggleValueChanged(bool value)
    {
        PlayerManager.Instance.DisplayAvatar(value);
    }

    private void OnWorldCanvasEditToggleChanged(bool value)
    {
        PlayerManager.Instance.DisplayControllers(value);
        worldSpaceUI.gameObject.SetActive(value);

        if (value)
        {
            ResetWorldSpaceCanvasPositionAndRotation();
        }

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
            float.Parse(canvaUIPositionZInputField.text));
        DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
        bridge?.MirrorCanvasUIPosition(WorldUIManager.Instance.Position);
    }

    private void SetWorldUIRotation()
    {
        WorldUIManager.Instance.Rotation = new Vector3(
            float.Parse(canvaUIRotationXInputField.text),
            float.Parse(canvaUIRotationYInputField.text),
            float.Parse(canvaUIRotationZInputField.text));
        DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
        bridge?.MirrorCanvasUIRotation(WorldUIManager.Instance.Rotation);
    }

    private void OnDisplayCanvaUIValueChanged(bool value)
    {
        if (value) WorldUIManager.Instance.DisplayText("Canva UI");
        else WorldUIManager.Instance.HideText();
        bridge?.MirrorDisplayCanvasUIToggle(value);
    }

    private void OnCanvasUIColorPickerButtonPress()
    {
        if (flexibleColorPicker == null)
        {
            var go = Instantiate(flexibleColorPickerPrefab, this.transform);
            flexibleColorPicker = go.GetComponent<FlexibleColorPicker>();
            flexibleColorPicker.color = WorldUIManager.Instance.BackgroundColor;

            var destroyer = go.GetComponent<DestroyOnButtonClick>();
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
        DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
        bridge?.MirrorCanvasUIColor(color);
    }


    /// <summary>Position caméra statique venant du WS canvas.</summary>
    public void ApplyStaticCameraPositionFromWS(Vector3 pos)
    {
        if (staticCamera == null) return;
        staticCamera.position = pos;
        positionXInputField.SetTextWithoutNotify(pos.x.ToString("F3"));
        positionYInputField.SetTextWithoutNotify(pos.y.ToString("F3"));
        positionZInputField.SetTextWithoutNotify(pos.z.ToString("F3"));
    }

    /// <summary>Rotation caméra statique venant du WS canvas.</summary>
    public void ApplyStaticCameraRotationFromWS(Vector3 euler)
    {
        if (staticCamera == null) return;
        staticCamera.rotation = Quaternion.Euler(euler);
        rotationXInputField.SetTextWithoutNotify(euler.x.ToString("F3"));
        rotationYInputField.SetTextWithoutNotify(euler.y.ToString("F3"));
        rotationZInputField.SetTextWithoutNotify(euler.z.ToString("F3"));
    }

    /// <summary>Avatar toggle venant du WS canvas.</summary>
    public void ApplyAvatarToggleFromWS(bool value)
    {
        PlayerManager.Instance.DisplayAvatar(value);
        avatarToggle.SetWithoutNotify(value);
    }

    /// <summary>Position World UI venant du WS canvas.</summary>
    public void ApplyCanvasUIPositionFromWS(Vector3 pos)
    {
        WorldUIManager.Instance.Position = pos;
        canvaUIPositionXInputField.SetTextWithoutNotify(pos.x.ToString("F3"));
        canvaUIPositionYInputField.SetTextWithoutNotify(pos.y.ToString("F3"));
        canvaUIPositionZInputField.SetTextWithoutNotify(pos.z.ToString("F3"));
        DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
    }

    /// <summary>Rotation World UI venant du WS canvas.</summary>
    public void ApplyCanvasUIRotationFromWS(Vector3 euler)
    {
        WorldUIManager.Instance.Rotation = euler;
        canvaUIRotationXInputField.SetTextWithoutNotify(euler.x.ToString("F3"));
        canvaUIRotationYInputField.SetTextWithoutNotify(euler.y.ToString("F3"));
        canvaUIRotationZInputField.SetTextWithoutNotify(euler.z.ToString("F3"));
        DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
    }

    /// <summary>Couleur World UI venant du WS canvas.</summary>
    public void ApplyCanvasUIColorFromWS(Color color)
    {
        canvaUIAlphaPreview.color = color;
        WorldUIManager.Instance.SetCurrentBackgoundColor(color);
        DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
    }

    /// <summary>Display toggle World UI venant du WS canvas.</summary>
    public void ApplyDisplayCanvasUIFromWS(bool value)
    {
        displayCanvaUIToggle.SetWithoutNotify(value);
        if (value) WorldUIManager.Instance.DisplayText("Stimulus Display");
        else WorldUIManager.Instance.HideText();
    }


    private void InitSceneDropdown()
    {
        if (sceneDropdown == null) return;

        sceneDropdown.ClearOptions();
        var names = new List<string>();
        for (int i = 0; i < availableScenes.Count; i++)
            names.Add(availableScenes[i].Name);

        sceneDropdown.AddOptions(names);
        sceneDropdown.value = 0;
        selectedSceneIndex = 0;

        bridge?.MirrorSceneDropdownOptions(names, 0);
    }

    private void OnSceneDropdownValueChanged(int value)
    {
        selectedSceneIndex = value;

        if (availableScenes == null || availableScenes.Count == 0)
        {
            SetStatus("No scenes available.", Color.red);
            return;
        }

        if (selectedSceneIndex < 0 || selectedSceneIndex >= availableScenes.Count)
        {
            SetStatus("Invalid scene selection.", Color.red);
            return;
        }

        scene = availableScenes[selectedSceneIndex];
        bridge?.MirrorSceneDropdownSelection(value);
        StartCoroutine(LoadScene());
    }


    private void SetStatus(string message, Color color)
    {
        Debug.Log($"[CanvasSetupPointCloudUI] {message}");
        if (statusText)
        {
            statusText.text = statusLocalizedText.GetLocalizedString("• " + message);
            statusText.color = color;
        }
    }
}