using com.rfilkov.kinect;
using Eflatun.SceneReference;
using Intel.RealSense;
using SFB;
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

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text currentFileNameText;
    [SerializeField] private LocalizedString statusLocalizedText;

    [Header("Toolbar - File")]
    [SerializeField] private Button newConfigButton;
    [SerializeField] private Button openConfigButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button saveAsButton;

    [Header("Toolbar - Clipboard (Hamburger)")]
    [SerializeField] private HamburgerMenu hamburgerMenu;

    [Header("Point cloud")]
    [SerializeField] private GameObject pointCloudEntryPrefab;
    [SerializeField] private Transform pointCloudContainer;

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
    [SerializeField] private TMP_Dropdown sceneDropdown;
    [SerializeField] private List<SceneReference> availableScenes = new List<SceneReference>();


    public event Action<CanvasSetupPointCloudUI> OnCanvasSetupPointCloudUIDestroy;

    private string selectedConfig;
    private int selectedSceneIndex = 0;

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
        ClearPointCloudEntry();
        InitSceneDropdown();
        UpdateFileNameDisplay();

        ShortcutManager.Instance.DisableShortCut();

        SetStatus("No config loaded.", Color.grey);
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
        SceneLoaderManager.Instance.OnSceneLoaded += OnSceneLoaded;

        if (sceneDropdown != null)
            sceneDropdown.onValueChanged.AddListener(OnSceneDropdownValueChanged);
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

        displayCanvaUIToggle.onValueChanged.RemoveListener(OnDisplayCanvaUIValueChanged);

        canvaUIColorPickerButton.onClick.RemoveListener(OnCanvasUIColorPickerButtonPress);

        ConfigFileManager.Instance.OnConfigLoaded -= OnConfigLoaded;
        ConfigFileManager.Instance.OnConfigSaved -= OnConfigSaved;
        SceneLoaderManager.Instance.OnSceneLoaded -= OnSceneLoaded;

        if (sceneDropdown != null)
            sceneDropdown.onValueChanged.RemoveListener(OnSceneDropdownValueChanged);

        foreach (var entry in pointCloudEntries)
        {
            entry.OnDisplayToggleRequested -= OnDisplayToggleRequested;
        }
    }

    // ─── File actions ────────────────────────────────────────────────────────

    private void OnNewConfigButtonClick()
    {
        var paths = StandaloneFileBrowser.SaveFilePanel("New config", ConfigFileManager.Instance.GetRootPath(), "new_config", "yaml");

        if (string.IsNullOrEmpty(paths))
        {
            SetStatus("New config cancelled.", Color.grey);
            return;
        }

        string configName = System.IO.Path.GetFileNameWithoutExtension(paths);
        ConfigFileManager.Instance.CreateNew(configName);

        foreach (var entry in pointCloudEntries)
        {
            entry.ResetToDefaults();
            entry.SetInteractable(true);
        }

        foreach (var entry in pointCloudEntries)
            entry.ForceApplyAndSave();

        bool saved = ConfigFileManager.Instance.SaveAs(paths);

        if (saved)
        {
            selectedConfig = configName;
            UpdateFileNameDisplay();
            SetStatus($"Created {configName}", Color.green);
        }
        else
        {
            SetStatus("Failed to save file.", Color.red);
        }
    }

    private void OnOpenConfigButtonClick()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Open config", ConfigFileManager.Instance.GetRootPath(), "yaml", false);

        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
        {
            SetStatus("Open cancelled.", Color.grey);
            return;
        }

        string fullPath = paths[0];
        var config = ConfigFileManager.Instance.LoadFromPath(fullPath);

        if (config != null)
        {
            selectedConfig = config.configName;
            UpdateFileNameDisplay();

            foreach (var entry in pointCloudEntries)
                entry.SetInteractable(true);

            SetStatus($"Opened {selectedConfig}", Color.green);
        }
        else
        {
            SetStatus("Failed to read file.", Color.red);
        }
    }

    private void OnSaveButtonClick()
    {
        if (ConfigFileManager.Instance.CurrentConfig == null)
        {
            SetStatus("No config loaded.", Color.grey);
            return;
        }

        bool saved = ConfigFileManager.Instance.Save();
        SetStatus(saved ? $"Saved {selectedConfig}" : "Failed to save file.", saved ? Color.green : Color.red);
    }

    private void OnSaveAsButtonClick()
    {
        if (ConfigFileManager.Instance.CurrentConfig == null)
        {
            SetStatus("No config loaded.", Color.grey);
            return;
        }

        string defaultName = selectedConfig ?? "config";
        var path = StandaloneFileBrowser.SaveFilePanel("Save config as", ConfigFileManager.Instance.GetRootPath(), defaultName, "yaml");

        if (string.IsNullOrEmpty(path))
        {
            SetStatus("Save cancelled.", Color.grey);
            return;
        }

        bool saved = ConfigFileManager.Instance.SaveAs(path);

        if (saved)
        {
            selectedConfig = System.IO.Path.GetFileNameWithoutExtension(path);
            UpdateFileNameDisplay();
            SetStatus($"Saved as {selectedConfig}", Color.green);
        }
        else
        {
            SetStatus("Failed to save file.", Color.red);
        }
    }

    // ─── Clipboard actions ───────────────────────────────────────────────────

    private void OnCopyConfigButtonClick()
    {
        bool copied = ConfigFileManager.Instance.CopyToClipboard();
        SetStatus(copied ? "Config copied to clipboard." : "No config loaded.", copied ? Color.green : Color.grey);
    }

    private void OnPasteConfigButtonClick()
    {
        if (string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
        {
            SetStatus("Clipboard is empty.", Color.red);
            return;
        }

        var config = ConfigFileManager.Instance.PasteFromClipboard();

        if (config != null)
        {
            selectedConfig = config.configName;
            UpdateFileNameDisplay();

            foreach (var entry in pointCloudEntries)
                entry.SetInteractable(true);

            SetStatus("Config pasted from clipboard.", Color.green);
        }
        else
        {
            SetStatus("Clipboard content is invalid.", Color.red);
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void UpdateFileNameDisplay()
    {
        if (currentFileNameText == null) return;
        currentFileNameText.text = string.IsNullOrEmpty(selectedConfig) ? "" : $"{selectedConfig}";
    }

    private void ClearPointCloudEntry()
    {
        foreach (var entry in pointCloudEntries)
            entry.OnDisplayToggleRequested -= OnDisplayToggleRequested;

        pointCloudEntries.Clear();
        activeEntry = null;

        while (pointCloudContainer.childCount > 0)
            DestroyImmediate(pointCloudContainer.GetChild(0).gameObject);
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

        if (!desiredState && requestingEntry == activeEntry)
        {
            StartCoroutine(SwitchPointCloudCoroutine(activeEntry, null));
            return;
        }

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

        if (previous != null)
        {
            previous.ApplyDisplayState(false);
        }

        activeEntry = null;

        if (next != null)
        {
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

    private void OnResetXROriginButtonPress()
    {
        ResetXROrigin.Instance.ResetOrigin();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene)
    {
        //StartCoroutine(WaitForKinectManagerInitialization());

        ClearPointCloudEntry();
        SpawnPointCloudEntries();

        foreach (var entry in pointCloudEntries)
            entry.SetInteractable(true);
    }

    public IEnumerator WaitForKinectManagerInitialization()
    {
        yield return new WaitUntil(() => KinectManager.Instance.IsInitialized());
        ClearPointCloudEntry();
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
        if (value)
            WorldUIManager.Instance.DisplayText("Canva UI");
        else
            WorldUIManager.Instance.HideText();
    }

    private void OnCanvasUIColorPickerButtonPress()
    {
        if (flexibleColorPicker == null)
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

    // ─── Scene ───────────────────────────────────────────────────────────────

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
    }

    /// <summary>
    /// Auto-loads the scene as soon as the dropdown value changes.
    /// </summary>
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
        StartCoroutine(LoadScene());
    }

    // ─── Status ──────────────────────────────────────────────────────────────

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