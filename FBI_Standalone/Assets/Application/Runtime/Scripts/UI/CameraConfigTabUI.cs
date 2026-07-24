using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// Content of the "Cameras" tab in the Config Editor: the camera config toolbar (New/Open/Save/
/// Save As/Copy/Paste), its own file name + status bar, the per-camera panels (transform, depth,
/// clamp, mirror), and this tab's copy of the shared preview widgets (scene dropdown, XR view
/// toggle, static view fields, avatar toggle, reset headset button) — all wired to
/// <see cref="ConfigEditorUI"/>, which holds the actual logic.
/// </summary>
public class CameraConfigTabUI : MonoBehaviour
{
    [SerializeField] private ConfigEditorUI configEditor;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text currentFileNameText;
    [SerializeField] private LocalizedString statusLocalizedText;

    [Header("Toolbar - File")]
    [SerializeField] private Button newConfigButton;
    [SerializeField] private Button openConfigButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button saveAsButton;

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

    [Header("Point cloud")]
    [SerializeField] private GameObject pointCloudEntryPrefab;
    [SerializeField] private Transform pointCloudContainer;

    private string selectedConfig;

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
        ClearPointCloudEntry();
        UpdateFileNameDisplay();
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
        newConfigButton.onClick.AddListener(OnNewConfigButtonClick);
        openConfigButton.onClick.AddListener(OnOpenConfigButtonClick);
        saveButton.onClick.AddListener(OnSaveButtonClick);
        saveAsButton.onClick.AddListener(OnSaveAsButtonClick);

        hamburgerMenu.OnCopyConfigClicked += OnCopyConfigButtonClick;
        hamburgerMenu.OnPasteConfigClicked += OnPasteConfigButtonClick;

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

        CameraConfigFileManager.Instance.OnConfigLoaded += OnConfigLoaded;
        CameraConfigFileManager.Instance.OnConfigSaved += OnConfigSaved;

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
        newConfigButton.onClick.RemoveListener(OnNewConfigButtonClick);
        openConfigButton.onClick.RemoveListener(OnOpenConfigButtonClick);
        saveButton.onClick.RemoveListener(OnSaveButtonClick);
        saveAsButton.onClick.RemoveListener(OnSaveAsButtonClick);

        hamburgerMenu.OnCopyConfigClicked -= OnCopyConfigButtonClick;
        hamburgerMenu.OnPasteConfigClicked -= OnPasteConfigButtonClick;

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

        CameraConfigFileManager.Instance.OnConfigLoaded -= OnConfigLoaded;
        CameraConfigFileManager.Instance.OnConfigSaved -= OnConfigSaved;

        configEditor.OnGlobalStatus -= SetStatus;
        configEditor.OnSceneListChanged -= PopulateSceneDropdown;
        configEditor.OnSceneIndexChanged -= HandleSceneIndexChanged;
        configEditor.OnSceneLoadCompleted -= OnSceneLoadCompleted;
        configEditor.OnXRViewChanged -= HandleXRViewChanged;
        configEditor.OnAvatarDisplayChanged -= HandleAvatarDisplayChanged;
        configEditor.OnStaticCameraPositionChanged -= SetStaticCameraPositionFields;
        configEditor.OnStaticCameraRotationChanged -= SetStaticCameraRotationFields;

        foreach (var entry in pointCloudEntries)
            entry.OnDisplayToggleRequested -= OnDisplayToggleRequested;
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

        ClearPointCloudEntry();
        SpawnPointCloudEntries();

        foreach (var entry in pointCloudEntries)
            entry.SetInteractable(true);

        configEditor.Bridge?.MirrorEntryInteractable(true);
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

    // ----------------------------------------------------------------- Toolbar

    private void OnNewConfigButtonClick()
    {
        FileBrowserService.SaveFile("New config", CameraConfigFileManager.Instance.GetRootPath(), "new_config", "yaml", (paths) =>
        {
            if (string.IsNullOrEmpty(paths))
            {
                configEditor.BroadcastStatus("New config cancelled.", Color.grey);
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
                configEditor.Bridge?.MirrorFileName(configName);
                configEditor.BroadcastStatus($"Created {configName}", Color.green);
                configEditor.Bridge?.MirrorEntryInteractable(true);
            }
            else
            {
                configEditor.BroadcastStatus("Failed to save file.", Color.red);
            }
        });
    }

    private void OnOpenConfigButtonClick()
    {
        FileBrowserService.OpenFile("Open config", CameraConfigFileManager.Instance.GetRootPath(), "yaml", (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                configEditor.BroadcastStatus("Open cancelled.", Color.grey);
                return;
            }

            var config = CameraConfigFileManager.Instance.LoadFromPath(path);

            if (config != null)
            {
                selectedConfig = config.configName;
                UpdateFileNameDisplay();

                foreach (var entry in pointCloudEntries)
                    entry.SetInteractable(true);

                configEditor.Bridge?.MirrorFileName(selectedConfig);
                configEditor.BroadcastStatus($"Opened {selectedConfig}", Color.green);
                configEditor.Bridge?.MirrorEntryInteractable(true);
            }
            else
            {
                configEditor.BroadcastStatus("Failed to read file.", Color.red);
            }
        });
    }

    private void OnSaveButtonClick()
    {
        if (CameraConfigFileManager.Instance.CurrentConfig == null)
        {
            configEditor.BroadcastStatus("No config loaded.", Color.grey);
            return;
        }

        bool saved = CameraConfigFileManager.Instance.Save();
        string msg = saved ? $"Saved {selectedConfig}" : "Failed to save file.";
        Color col = saved ? Color.green : Color.red;
        configEditor.BroadcastStatus(msg, col);
    }

    private void OnSaveAsButtonClick()
    {
        if (CameraConfigFileManager.Instance.CurrentConfig == null)
        {
            configEditor.BroadcastStatus("No config loaded.", Color.grey);
            return;
        }

        string defaultName = selectedConfig ?? "config";
        FileBrowserService.SaveFile("Save config as", CameraConfigFileManager.Instance.GetRootPath(), defaultName, "yaml", (path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                configEditor.BroadcastStatus("Save cancelled.", Color.grey);
                return;
            }

            bool saved = CameraConfigFileManager.Instance.SaveAs(path);

            if (saved)
            {
                selectedConfig = System.IO.Path.GetFileNameWithoutExtension(path);
                UpdateFileNameDisplay();
                configEditor.Bridge?.MirrorFileName(selectedConfig);
                configEditor.BroadcastStatus($"Saved as {selectedConfig}", Color.green);
            }
            else
            {
                configEditor.BroadcastStatus("Failed to save file.", Color.red);
            }
        });
    }

    private void OnCopyConfigButtonClick()
    {
        bool copied = CameraConfigFileManager.Instance.CopyToClipboard();
        string msg = copied ? "Config copied to clipboard." : "No config loaded.";
        Color col = copied ? Color.green : Color.grey;
        configEditor.BroadcastStatus(msg, col);
    }

    private void OnPasteConfigButtonClick()
    {
        if (string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
        {
            configEditor.BroadcastStatus("Clipboard is empty.", Color.red);
            return;
        }

        var config = CameraConfigFileManager.Instance.PasteFromClipboard();

        if (config != null)
        {
            selectedConfig = config.configName;
            UpdateFileNameDisplay();

            foreach (var entry in pointCloudEntries)
                entry.SetInteractable(true);

            configEditor.Bridge?.MirrorFileName(selectedConfig);
            configEditor.BroadcastStatus("Config pasted from clipboard.", Color.green);
            configEditor.Bridge?.MirrorEntryInteractable(true);
        }
        else
        {
            configEditor.BroadcastStatus("Clipboard content is invalid.", Color.red);
        }
    }

    private void UpdateFileNameDisplay()
    {
        if (currentFileNameText == null) return;
        currentFileNameText.text = string.IsNullOrEmpty(selectedConfig) ? "" : selectedConfig;
    }

    // ----------------------------------------------------------------- Point cloud entries

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
        if (configEditor.WorldSpaceUI != null)
        {
            configEditor.WorldSpaceUI.SpawnPointCloudEntries(cameraIds);
            configEditor.Bridge?.PairEntries(pointCloudEntries, configEditor.WorldSpaceUI.GetEntries());
        }
    }

    private void OnDisplayToggleRequested(PointCloudUIEntry requestingEntry, bool desiredState)
    {
        if (isSwitching)
        {
            Debug.Log("[CameraConfigTabUI] Switch already in progress, ignoring request.");
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
        configEditor.Bridge?.MirrorEntryData(i, pos, rot, e.DepthMin, e.DepthMax, e.FlipX, e.FlipY);
    }

    private void OnEntryMaxDepthChanged(int cameraId, float value)
    {
        int i = pointCloudEntries.FindIndex(e => e.CameraId == cameraId);
        if (i < 0) return;
        var e = pointCloudEntries[i];
        configEditor.Bridge?.MirrorEntryData(i, e.Position, e.Rotation, e.DepthMin, value, e.FlipX, e.FlipY);
    }

    private void OnEntryMinDepthChanged(int cameraId, float value)
    {
        int i = pointCloudEntries.FindIndex(e => e.CameraId == cameraId);
        if (i < 0) return;
        var e = pointCloudEntries[i];
        configEditor.Bridge?.MirrorEntryData(i, e.Position, e.Rotation, value, e.DepthMax, e.FlipX, e.FlipY);
    }

    private void OnEntryFlipChanged(int cameraId, bool flipX, bool flipY)
    {
        int i = pointCloudEntries.FindIndex(e => e.CameraId == cameraId);
        if (i < 0) return;
        var e = pointCloudEntries[i];
        configEditor.Bridge?.MirrorEntryData(i, e.Position, e.Rotation, e.DepthMin, e.DepthMax, flipX, flipY);
    }

    private void OnEntryClampChanged(int cameraId, float xMin, float xMax, float yMin, float yMax)
    {
        int i = pointCloudEntries.FindIndex(e => e.CameraId == cameraId);
        if (i < 0) return;
        configEditor.Bridge?.MirrorEntryClamp(i, xMin, xMax, yMin, yMax);
    }

    private IEnumerator SwitchPointCloudCoroutine(PointCloudUIEntry previous, PointCloudUIEntry next)
    {
        isSwitching = true;

        if (previous != null)
        {
            previous.ApplyDisplayState(false);
            configEditor.Bridge?.MirrorEntryDisplayState(previous.CameraId, false);
        }

        activeEntry = null;

        if (next != null)
        {
            yield return new WaitForSeconds(configEditor.SwitchDelay);
            next.ApplyDisplayState(true);
            configEditor.Bridge?.MirrorEntryDisplayState(next.CameraId, true);
            activeEntry = next;
        }

        isSwitching = false;
    }

    // ----------------------------------------------------------------- CameraConfigFileManager events

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

            configEditor.Bridge?.MirrorEntryData(i,
                data.position.ToVector3(),
                data.rotation.ToVector3(),
                data.depthMin,
                data.depthMax,
                data.scale.x == -1,
                data.scale.y == -1);

            configEditor.Bridge?.MirrorEntryClamp(i, data.clampXMin, data.clampXMax, data.clampYMin, data.clampYMax);

            PointCloudManager.Instance.SetPointcloudConfig(PointCloudManager.Instance.GetPointCloud(pointCloudEntries[i].CameraId), file);
        }
    }

    private void SetStatus(string message, Color color)
    {
        Debug.Log($"[CameraConfigTabUI] {message}");
        if (statusText)
        {
            statusText.text = statusLocalizedText.GetLocalizedString("• " + message);
            statusText.color = color;
        }
    }
}