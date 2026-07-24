using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// Content of the "Stimulus Display" tab in the Config Editor: the display config toolbar (New/
/// Open/Save/Save As/Copy/Paste), its own file name + status bar, the stimulus canvas fields
/// (position/rotation/background color), and this tab's copy of the shared preview widgets (scene
/// dropdown, XR view toggle, static view fields, avatar toggle, reset headset button) — all wired
/// to <see cref="ConfigEditorUI"/>, which holds the actual logic.
/// </summary>
public class DisplayConfigTabUI : MonoBehaviour
{
    [SerializeField] private ConfigEditorUI configEditor;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text currentFileNameText;
    [SerializeField] private LocalizedString statusLocalizedText;

    [Header("Toolbar - File")]
    [SerializeField] private Button newDisplayConfigButton;
    [SerializeField] private Button openDisplayConfigButton;
    [SerializeField] private Button saveDisplayConfigButton;
    [SerializeField] private Button saveAsDisplayConfigButton;

    [Header("Toolbar - Clipboard")]
    [SerializeField] private HamburgerMenu hamburgerMenu;

    [Header("Scene (duplicated widget, shared logic in ConfigEditorUI)")]
    [SerializeField] private TMP_Dropdown sceneDropdown;

    [Header("VR View (duplicated widget)")]
    [SerializeField] private UISwitcher.UISwitcher worldCanvasEditToggle;
    [SerializeField] private Button resetXROriginButton;

    [Header("Static View (duplicated widget)")]
    [SerializeField] private GameObject avatarToggleGameObject;
    private UISwitcher.UISwitcher avatarToggle;

    [SerializeField] private TMP_InputField positionXInputField;
    [SerializeField] private TMP_InputField positionYInputField;
    [SerializeField] private TMP_InputField positionZInputField;
    [SerializeField] private TMP_InputField rotationXInputField;
    [SerializeField] private TMP_InputField rotationYInputField;
    [SerializeField] private TMP_InputField rotationZInputField;

    [Header("Canva UI (Stimulus Display specific)")]
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

    private string selectedDisplayConfig;

    private void Awake()
    {
        avatarToggle = avatarToggleGameObject.GetComponent<UISwitcher.UISwitcher>();
    }

    private void Start()
    {
        UpdateDisplayConfigFileNameDisplay();
    }

    /// <summary>
    /// Shows or hides this tab via its CanvasGroup instead of SetActive, so this component (and
    /// its event subscriptions to ConfigEditorUI) stay alive and up to date even while the tab is
    /// hidden behind the other one.
    /// </summary>
    public void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void OnEnable()
    {
        newDisplayConfigButton.onClick.AddListener(OnNewDisplayConfigButtonClick);
        openDisplayConfigButton.onClick.AddListener(OnOpenDisplayConfigButtonClick);
        saveDisplayConfigButton.onClick.AddListener(OnSaveDisplayConfigButtonClick);
        saveAsDisplayConfigButton.onClick.AddListener(OnSaveAsDisplayConfigButtonClick);

        hamburgerMenu.OnCopyConfigClicked += OnCopyDisplayConfigButtonClick;
        hamburgerMenu.OnPasteConfigClicked += OnPasteDisplayConfigButtonClick;

        sceneDropdown.onValueChanged.AddListener(OnSceneDropdownValueChanged);

        worldCanvasEditToggle.onValueChanged.AddListener(configEditor.SetXRViewEnabled);
        resetXROriginButton.onClick.AddListener(configEditor.ResetHeadsetOrientation);

        positionXInputField.onValueChanged.AddListener((str) => ApplyStaticCameraPositionFromFields());
        positionYInputField.onValueChanged.AddListener((str) => ApplyStaticCameraPositionFromFields());
        positionZInputField.onValueChanged.AddListener((str) => ApplyStaticCameraPositionFromFields());

        rotationXInputField.onValueChanged.AddListener((str) => ApplyStaticCameraRotationFromFields());
        rotationYInputField.onValueChanged.AddListener((str) => ApplyStaticCameraRotationFromFields());
        rotationZInputField.onValueChanged.AddListener((str) => ApplyStaticCameraRotationFromFields());

        avatarToggle.onValueChanged.AddListener(configEditor.SetDisplayAvatar);

        displayCanvaUIToggle.onValueChanged.AddListener(OnDisplayCanvaUIValueChanged);
        canvaUIColorPickerButton.onClick.AddListener(OnCanvasUIColorPickerButtonPress);

        canvaUIPositionXInputField.onValueChanged.AddListener((str) => SetWorldUIPosition());
        canvaUIPositionYInputField.onValueChanged.AddListener((str) => SetWorldUIPosition());
        canvaUIPositionZInputField.onValueChanged.AddListener((str) => SetWorldUIPosition());
        canvaUIRotationXInputField.onValueChanged.AddListener((str) => SetWorldUIRotation());
        canvaUIRotationYInputField.onValueChanged.AddListener((str) => SetWorldUIRotation());
        canvaUIRotationZInputField.onValueChanged.AddListener((str) => SetWorldUIRotation());

        DisplayConfigFileManager.Instance.OnConfigLoaded += OnDisplayConfigLoaded;
        DisplayConfigFileManager.Instance.OnConfigSaved += OnDisplayConfigSaved;

        configEditor.OnGlobalStatus += SetStatus;
        configEditor.OnSceneListChanged += PopulateSceneDropdown;
        configEditor.OnSceneIndexChanged += HandleSceneIndexChanged;
        configEditor.OnSceneLoadCompleted += OnSceneLoadCompleted;
        configEditor.OnXRViewChanged += HandleXRViewChanged;
        configEditor.OnAvatarDisplayChanged += HandleAvatarDisplayChanged;
        configEditor.OnStaticCameraPositionChanged += SetStaticCameraPositionFields;
        configEditor.OnStaticCameraRotationChanged += SetStaticCameraRotationFields;

        // Re-sync this tab's own UI to the current authoritative state. Needed because this tab's
        // GameObject gets SetActive(false) while the other tab is shown, which unsubscribes it
        // (above) from these events for as long as it's hidden — without this, switching back to
        // this tab would show whatever was last known before it got hidden, not the current state.
        SetStatus(configEditor.CurrentStatusMessage, configEditor.CurrentStatusColor);
        PopulateSceneDropdown(configEditor.GetAvailableSceneNames(), configEditor.SelectedSceneIndex);
        HandleXRViewChanged(configEditor.IsXRViewEnabled);
        HandleAvatarDisplayChanged(configEditor.IsAvatarDisplayed);
        SetStaticCameraPositionFields(configEditor.GetStaticCameraPosition());
        SetStaticCameraRotationFields(configEditor.GetStaticCameraRotation());
    }

    private void OnDisable()
    {
        newDisplayConfigButton.onClick.RemoveListener(OnNewDisplayConfigButtonClick);
        openDisplayConfigButton.onClick.RemoveListener(OnOpenDisplayConfigButtonClick);
        saveDisplayConfigButton.onClick.RemoveListener(OnSaveDisplayConfigButtonClick);
        saveAsDisplayConfigButton.onClick.RemoveListener(OnSaveAsDisplayConfigButtonClick);

        hamburgerMenu.OnCopyConfigClicked -= OnCopyDisplayConfigButtonClick;
        hamburgerMenu.OnPasteConfigClicked -= OnPasteDisplayConfigButtonClick;

        sceneDropdown.onValueChanged.RemoveListener(OnSceneDropdownValueChanged);

        worldCanvasEditToggle.onValueChanged.RemoveListener(configEditor.SetXRViewEnabled);
        resetXROriginButton.onClick.RemoveListener(configEditor.ResetHeadsetOrientation);

        positionXInputField.onValueChanged.RemoveAllListeners();
        positionYInputField.onValueChanged.RemoveAllListeners();
        positionZInputField.onValueChanged.RemoveAllListeners();

        rotationXInputField.onValueChanged.RemoveAllListeners();
        rotationYInputField.onValueChanged.RemoveAllListeners();
        rotationZInputField.onValueChanged.RemoveAllListeners();

        avatarToggle.onValueChanged.RemoveListener(configEditor.SetDisplayAvatar);

        displayCanvaUIToggle.onValueChanged.RemoveListener(OnDisplayCanvaUIValueChanged);
        canvaUIColorPickerButton.onClick.RemoveListener(OnCanvasUIColorPickerButtonPress);

        canvaUIPositionXInputField.onValueChanged.RemoveAllListeners();
        canvaUIPositionYInputField.onValueChanged.RemoveAllListeners();
        canvaUIPositionZInputField.onValueChanged.RemoveAllListeners();
        canvaUIRotationXInputField.onValueChanged.RemoveAllListeners();
        canvaUIRotationYInputField.onValueChanged.RemoveAllListeners();
        canvaUIRotationZInputField.onValueChanged.RemoveAllListeners();

        DisplayConfigFileManager.Instance.OnConfigLoaded -= OnDisplayConfigLoaded;
        DisplayConfigFileManager.Instance.OnConfigSaved -= OnDisplayConfigSaved;

        configEditor.OnGlobalStatus -= SetStatus;
        configEditor.OnSceneListChanged -= PopulateSceneDropdown;
        configEditor.OnSceneIndexChanged -= HandleSceneIndexChanged;
        configEditor.OnSceneLoadCompleted -= OnSceneLoadCompleted;
        configEditor.OnXRViewChanged -= HandleXRViewChanged;
        configEditor.OnAvatarDisplayChanged -= HandleAvatarDisplayChanged;
        configEditor.OnStaticCameraPositionChanged -= SetStaticCameraPositionFields;
        configEditor.OnStaticCameraRotationChanged -= SetStaticCameraRotationFields;
    }

    // ----------------------------------------------------------------- Scene (duplicated widget)

    private void PopulateSceneDropdown(List<string> names, int selectedIndex)
    {
        if (sceneDropdown == null) return;

        sceneDropdown.ClearOptions();
        sceneDropdown.AddOptions(names);
        sceneDropdown.SetValueWithoutNotify(selectedIndex);
    }

    private void OnSceneDropdownValueChanged(int value)
    {
        configEditor.RequestLoadScene(value);
    }

    private void HandleSceneIndexChanged(int index) => sceneDropdown.SetValueWithoutNotify(index);
    private void HandleXRViewChanged(bool value) => worldCanvasEditToggle.SetWithoutNotify(value);
    private void HandleAvatarDisplayChanged(bool value) => avatarToggle.SetWithoutNotify(value);

    private void OnSceneLoadCompleted()
    {
        SetStaticCameraPositionFields(configEditor.GetStaticCameraPosition());
        SetStaticCameraRotationFields(configEditor.GetStaticCameraRotation());

        SetWorldUIInputField();
        canvaUIAlphaPreview.color = WorldUIManager.Instance.BackgroundColor;

        displayCanvaUIToggle.SetWithoutNotify(false);
        configEditor.Bridge?.MirrorDisplayCanvasUIToggle(false);
        configEditor.Bridge?.MirrorCanvasUIPosition(WorldUIManager.Instance.Position);
        configEditor.Bridge?.MirrorCanvasUIRotation(WorldUIManager.Instance.Rotation);
        configEditor.Bridge?.MirrorCanvasUIColor(WorldUIManager.Instance.BackgroundColor);
    }

    // ----------------------------------------------------------------- Static view (duplicated widget)

    private void SetStaticCameraPositionFields(Vector3 pos)
    {
        positionXInputField.SetTextWithoutNotify(pos.x.ToString("F3"));
        positionYInputField.SetTextWithoutNotify(pos.y.ToString("F3"));
        positionZInputField.SetTextWithoutNotify(pos.z.ToString("F3"));
    }

    private void SetStaticCameraRotationFields(Vector3 euler)
    {
        rotationXInputField.SetTextWithoutNotify(euler.x.ToString("F3"));
        rotationYInputField.SetTextWithoutNotify(euler.y.ToString("F3"));
        rotationZInputField.SetTextWithoutNotify(euler.z.ToString("F3"));
    }

    private void ApplyStaticCameraPositionFromFields()
    {
        configEditor.SetStaticCameraPosition(new Vector3(
            float.Parse(positionXInputField.text),
            float.Parse(positionYInputField.text),
            float.Parse(positionZInputField.text)));
    }

    private void ApplyStaticCameraRotationFromFields()
    {
        configEditor.SetStaticCameraRotation(new Vector3(
            float.Parse(rotationXInputField.text),
            float.Parse(rotationYInputField.text),
            float.Parse(rotationZInputField.text)));
    }

    // ----------------------------------------------------------------- Canva UI (Stimulus Display)

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
        configEditor.Bridge?.MirrorCanvasUIPosition(WorldUIManager.Instance.Position);
    }

    private void SetWorldUIRotation()
    {
        WorldUIManager.Instance.Rotation = new Vector3(
            float.Parse(canvaUIRotationXInputField.text),
            float.Parse(canvaUIRotationYInputField.text),
            float.Parse(canvaUIRotationZInputField.text));
        DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
        configEditor.Bridge?.MirrorCanvasUIRotation(WorldUIManager.Instance.Rotation);
    }

    private void OnDisplayCanvaUIValueChanged(bool value)
    {
        if (value) WorldUIManager.Instance.DisplayText("Canva UI");
        else WorldUIManager.Instance.HideText();
        configEditor.Bridge?.MirrorDisplayCanvasUIToggle(value);
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
        configEditor.Bridge?.MirrorCanvasUIColor(color);
    }

    // ----------------------------------------------------------------- Bridge callbacks (from VR headset editing)
    // Called by ConfigEditorUI, which forwards the bridge's canvas-UI-related callbacks here since
    // the stimulus canvas is this tab's data.

    public void ApplyCanvasUIPositionFromWS(Vector3 pos)
    {
        WorldUIManager.Instance.Position = pos;
        canvaUIPositionXInputField.SetTextWithoutNotify(pos.x.ToString("F3"));
        canvaUIPositionYInputField.SetTextWithoutNotify(pos.y.ToString("F3"));
        canvaUIPositionZInputField.SetTextWithoutNotify(pos.z.ToString("F3"));
        DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
    }

    public void ApplyCanvasUIRotationFromWS(Vector3 euler)
    {
        WorldUIManager.Instance.Rotation = euler;
        canvaUIRotationXInputField.SetTextWithoutNotify(euler.x.ToString("F3"));
        canvaUIRotationYInputField.SetTextWithoutNotify(euler.y.ToString("F3"));
        canvaUIRotationZInputField.SetTextWithoutNotify(euler.z.ToString("F3"));
        DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
    }

    public void ApplyCanvasUIColorFromWS(Color color)
    {
        canvaUIAlphaPreview.color = color;
        WorldUIManager.Instance.SetCurrentBackgoundColor(color);
        DisplayConfigFileManager.Instance.SaveUICanvas(WorldUIManager.Instance.transform, WorldUIManager.Instance.BackgroundColor);
    }

    public void ApplyDisplayCanvasUIFromWS(bool value)
    {
        displayCanvaUIToggle.SetWithoutNotify(value);
        if (value) WorldUIManager.Instance.DisplayText("Stimulus Display");
        else WorldUIManager.Instance.HideText();
    }

    // ----------------------------------------------------------------- Toolbar

    private void OnNewDisplayConfigButtonClick()
    {
        FileBrowserService.SaveFile("New display config", DisplayConfigFileManager.Instance.GetRootPath(), "new_display_config", "yaml", (paths) =>
        {
            if (string.IsNullOrEmpty(paths))
            {
                configEditor.BroadcastStatus("New config cancelled.", Color.grey);
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
                configEditor.BroadcastStatus($"Created {configName}", Color.green);
            }
            else
            {
                configEditor.BroadcastStatus("Failed to save file.", Color.red);
            }
        });
    }

    private void OnOpenDisplayConfigButtonClick()
    {
        FileBrowserService.OpenFile("Open display config", DisplayConfigFileManager.Instance.GetRootPath(), "yaml", (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                configEditor.BroadcastStatus("Open cancelled.", Color.grey);
                return;
            }

            var config = DisplayConfigFileManager.Instance.LoadFromPath(path);

            if (config != null)
            {
                selectedDisplayConfig = config.configName;
                UpdateDisplayConfigFileNameDisplay();
                configEditor.BroadcastStatus($"Opened {selectedDisplayConfig}", Color.green);
            }
            else
            {
                configEditor.BroadcastStatus("Failed to read file.", Color.red);
            }
        });
    }

    private void OnSaveDisplayConfigButtonClick()
    {
        if (DisplayConfigFileManager.Instance.CurrentConfig == null)
        {
            configEditor.BroadcastStatus("No config loaded.", Color.grey);
            return;
        }

        bool saved = DisplayConfigFileManager.Instance.Save();
        string msg = saved ? $"Saved {selectedDisplayConfig}" : "Failed to save file.";
        Color col = saved ? Color.green : Color.red;
        configEditor.BroadcastStatus(msg, col);
    }

    private void OnSaveAsDisplayConfigButtonClick()
    {
        if (DisplayConfigFileManager.Instance.CurrentConfig == null)
        {
            configEditor.BroadcastStatus("No config loaded.", Color.grey);
            return;
        }

        string defaultName = selectedDisplayConfig ?? "display_config";
        FileBrowserService.SaveFile("Save display config as", DisplayConfigFileManager.Instance.GetRootPath(), defaultName, "yaml", (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                configEditor.BroadcastStatus("Save cancelled.", Color.grey);
                return;
            }

            bool saved = DisplayConfigFileManager.Instance.SaveAs(path);

            if (saved)
            {
                selectedDisplayConfig = System.IO.Path.GetFileNameWithoutExtension(path);
                UpdateDisplayConfigFileNameDisplay();
                configEditor.BroadcastStatus($"Saved as {selectedDisplayConfig}", Color.green);
            }
            else
            {
                configEditor.BroadcastStatus("Failed to save file.", Color.red);
            }
        });
    }

    private void OnCopyDisplayConfigButtonClick()
    {
        bool copied = DisplayConfigFileManager.Instance.CopyToClipboard();
        string msg = copied ? "Config copied to clipboard." : "No config loaded.";
        Color col = copied ? Color.green : Color.grey;
        configEditor.BroadcastStatus(msg, col);
    }

    private void OnPasteDisplayConfigButtonClick()
    {
        if (string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
        {
            configEditor.BroadcastStatus("Clipboard is empty.", Color.red);
            return;
        }

        var config = DisplayConfigFileManager.Instance.PasteFromClipboard();

        if (config != null)
        {
            selectedDisplayConfig = config.configName;
            UpdateDisplayConfigFileNameDisplay();
            configEditor.BroadcastStatus("Config pasted from clipboard.", Color.green);
        }
        else
        {
            configEditor.BroadcastStatus("Clipboard content is invalid.", Color.red);
        }
    }

    private void UpdateDisplayConfigFileNameDisplay()
    {
        if (currentFileNameText == null) return;
        currentFileNameText.text = string.IsNullOrEmpty(selectedDisplayConfig) ? "" : selectedDisplayConfig;
    }

    // ----------------------------------------------------------------- DisplayConfigFileManager events

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

        configEditor.Bridge?.MirrorCanvasUIPosition(WorldUIManager.Instance.Position);
        configEditor.Bridge?.MirrorCanvasUIRotation(WorldUIManager.Instance.Rotation);
        configEditor.Bridge?.MirrorCanvasUIColor(color);
    }

    private void SetStatus(string message, Color color)
    {
        Debug.Log($"[DisplayConfigTabUI] {message}");
        if (statusText)
        {
            statusText.text = statusLocalizedText.GetLocalizedString("• " + message);
            statusText.color = color;
        }
    }
}