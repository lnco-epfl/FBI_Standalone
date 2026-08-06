using Eflatun.SceneReference;
using PrimeTween;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coordinator for the Config Editor window. Owns everything that is NOT duplicated between the
/// two tabs: tab switching, the close button, the scene list/loading, and the shared 3D preview
/// (world space VR mirror canvas + bridge, static camera, XR view, avatar display, headset reset).
///
/// The UI widgets for scene dropdown / XR view toggle / static view fields / avatar toggle /
/// reset button are intentionally duplicated in <see cref="CameraConfigTabUI"/> and
/// <see cref="DisplayConfigTabUI"/> (one copy per tab). This script exposes the actual logic as
/// public methods and raises events so both tabs' duplicated widgets stay in sync with each other
/// without fighting over which one "owns" the value.
/// </summary>
public class ConfigEditorUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Button camerasTabButton;
    [SerializeField] private Button stimulusDisplayTabButton;
    [SerializeField] private CameraConfigTabUI cameraTab;
    [SerializeField] private DisplayConfigTabUI displayTab;

    [SerializeField] private Color ActiveColor;
    [SerializeField] private Color DeactiveColor; 

    [Header("Scene")]
    [SerializeField] private SceneReference scene;
    [SerializeField] private List<SceneReference> availableScenes = new List<SceneReference>();
    private int selectedSceneIndex = 0;

    [Header("Static View")]
    [SerializeField] private GameObject staticCameraPrefab;
    private Transform staticCamera;

    [Header("World Space Canvas (VR mirror)")]
    [SerializeField] private GameObject worldSpaceCanvasPrefab;
    [SerializeField] private float switchDelay = 1.5f;

    private WorldSpacePointCloudUI worldSpaceUI;
    private PointCloudUIBridge bridge;

    public event Action<ConfigEditorUI> OnConfigEditorDestroy;

    /// <summary>Fired for any status message that should show in both tabs' status bars
    /// (scene loading messages, essentially). Tab-specific messages (save/open/etc.) are raised
    /// directly by each tab, not through here.</summary>
    public event Action<string, Color> OnGlobalStatus;

    /// <summary>Fired when the scene list or the currently loaded scene changes, so both tabs can
    /// keep their own dropdown in sync.</summary>
    public event Action<List<string>, int> OnSceneListChanged;
    public event Action<int> OnSceneIndexChanged;

    /// <summary>Fired once a scene has finished loading (fade in/out done, static camera and
    /// world space canvas ready). Tabs use this to (re)spawn/reset whatever they own.</summary>
    public event Action OnSceneLoadCompleted;

    public event Action<bool> OnXRViewChanged;
    public event Action<bool> OnAvatarDisplayChanged;
    public event Action<Vector3> OnStaticCameraPositionChanged;
    public event Action<Vector3> OnStaticCameraRotationChanged;

    public PointCloudUIBridge Bridge => bridge;
    public WorldSpacePointCloudUI WorldSpaceUI => worldSpaceUI;
    public float SwitchDelay => switchDelay;

    private void Start()
    {
        OnCamerasTabButtonPress();

        ShortcutManager.Instance.DisableMainShortCut();
        ShortcutManager.Instance.EnableConfigShortCut();

        // NOTE: relies on Unity calling OnEnable on every object present in the scene before
        // Start runs on any of them, so cameraTab/displayTab have already subscribed to the
        // events above by the time LoadScene() raises them.
        StartCoroutine(LoadScene());
    }

    private void OnEnable()
    {
        closeButton.onClick.AddListener(OnButtonCloseClick);
        camerasTabButton.onClick.AddListener(OnCamerasTabButtonPress);
        stimulusDisplayTabButton.onClick.AddListener(OnStimulusDisplayTabButtonPress);
    }


    private void OnDisable()
    {
        closeButton.onClick.RemoveListener(OnButtonCloseClick);
        camerasTabButton.onClick.RemoveAllListeners();
        stimulusDisplayTabButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        if (worldSpaceUI != null) Destroy(worldSpaceUI.gameObject);
        if (bridge != null) Destroy(bridge.gameObject);
    }

    private void OnCamerasTabButtonPress()
    {
        SwitchTab(showCameras: true);

        stimulusDisplayTabButton.targetGraphic.color = DeactiveColor;
        camerasTabButton.targetGraphic.color = ActiveColor;
    }

    private void OnStimulusDisplayTabButtonPress()
    {
        SwitchTab(showCameras: false);

        stimulusDisplayTabButton.targetGraphic.color = ActiveColor;
        camerasTabButton.targetGraphic.color = DeactiveColor;
    }


    private void SwitchTab(bool showCameras)
    {
        cameraTab?.SetVisible(showCameras);
        displayTab?.SetVisible(!showCameras);
    }

    private void OnButtonCloseClick()
    {
        ShortcutManager.Instance.DisableConfigShortCut();
        ShortcutManager.Instance.EnableMainShortCut();

        SceneLoaderManager.Instance.LoadDefaultScene();
        OnConfigEditorDestroy?.Invoke(this);
    }

    // ----------------------------------------------------------------- Scene list & loading

    public List<string> GetAvailableSceneNames()
    {
        var names = new List<string>();
        for (int i = 0; i < availableScenes.Count; i++)
            names.Add(availableScenes[i].Name);
        return names;
    }

    public int SelectedSceneIndex => selectedSceneIndex;

    private string currentStatusMessage = "";
    private Color currentStatusColor = Color.white;

    /// <summary>The last status message/color broadcast, so a tab can re-sync its own status
    /// text immediately when it becomes visible (e.g. after switching tabs), instead of waiting
    /// for the next status-changing action.</summary>
    public string CurrentStatusMessage => currentStatusMessage;
    public Color CurrentStatusColor => currentStatusColor;

    /// <summary>Single entry point for status updates. Both tabs call this (instead of writing to
    /// their own status text directly) so the message always shows identically in both panels,
    /// regardless of which tab triggered it or which tab is currently visible.</summary>
    public void BroadcastStatus(string message, Color color)
    {
        currentStatusMessage = message;
        currentStatusColor = color;
        OnGlobalStatus?.Invoke(message, color);
        bridge?.MirrorStatus(message, color);
    }

    /// <summary>Called by either tab's scene dropdown when the user picks a scene.</summary>
    public void RequestLoadScene(int index)
    {
        if (availableScenes == null || availableScenes.Count == 0)
        {
            BroadcastStatus("No scenes available.", Color.red);
            return;
        }

        if (index < 0 || index >= availableScenes.Count)
        {
            BroadcastStatus("Invalid scene selection.", Color.red);
            return;
        }

        selectedSceneIndex = index;
        scene = availableScenes[index];
        bridge?.MirrorSceneDropdownSelection(index);
        OnSceneIndexChanged?.Invoke(index);

        StartCoroutine(LoadScene());
    }

    public IEnumerator LoadScene()
    {
        BroadcastStatus("Loading scene, please wait", Color.yellow);

        Fader.Instance.FadeToBlack();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        yield return SceneLoaderManager.Instance.LoadAsyncScene(scene);

        Fader.Instance.FadeToClear();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        BroadcastStatus("Scene loaded", Color.green);

        staticCamera = Instantiate(staticCameraPrefab).transform;
        OnStaticCameraPositionChanged?.Invoke(staticCamera.position);
        OnStaticCameraRotationChanged?.Invoke(staticCamera.rotation.eulerAngles);

        SpawnWorldSpaceCanvas();

        var names = GetAvailableSceneNames();
        OnSceneListChanged?.Invoke(names, selectedSceneIndex);
        bridge?.MirrorSceneDropdownOptions(names, selectedSceneIndex);

        isAvatarDisplayed = false;
        OnAvatarDisplayChanged?.Invoke(false);

        OnSceneLoadCompleted?.Invoke();

        Tween.Delay(1.0f, () => BroadcastStatus("No config loaded.", Color.grey));
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
            Debug.LogWarning("[ConfigEditorUI] worldSpaceCanvasPrefab non assigné — canvas WS ignoré.");
            return;
        }

        var worldSpaceCanvas = Instantiate(worldSpaceCanvasPrefab);
        worldSpaceCanvas.name = "WorldSpacePointCloudCanvas";

        worldSpaceUI = worldSpaceCanvas.GetComponent<WorldSpacePointCloudUI>();

        ResetWorldSpaceCanvasPositionAndRotation();

        if (worldSpaceUI == null)
        {
            Debug.LogError("[ConfigEditorUI] Prefab is missing WorldSpacePointCloudUI component!");
            Destroy(worldSpaceCanvas);
            return;
        }

        var bridgeGo = new GameObject("PointCloudUIBridge");

        bridge = bridgeGo.AddComponent<PointCloudUIBridge>();
        // NOTE: PointCloudUIBridge.Initialize's owner parameter must be typed as ConfigEditorUI now
        // (it used to be CanvasSetupPointCloudUI). ConfigEditorUI implements all the ApplyXFromWS
        // callbacks the bridge calls, forwarding to cameraTab/displayTab where relevant.
        bridge.Initialize(this, worldSpaceUI, switchDelay);

        worldSpaceUI.SetBridge(bridge);
        worldSpaceUI.gameObject.SetActive(false);

        isXRViewEnabled = false;
        OnXRViewChanged?.Invoke(false);
    }

    private void ResetWorldSpaceCanvasPositionAndRotation()
    {
        worldSpaceUI.transform.position = new Vector3(-1.0f, 1.0f, 1.0f);
        worldSpaceUI.transform.rotation = Quaternion.Euler(0.0f, -45.0f, 0.0f);
    }

    // ----------------------------------------------------------------- XR view / avatar / reset / static cam

    private bool isXRViewEnabled = false;
    private bool isAvatarDisplayed = false;

    /// <summary>Current state, so a tab can re-sync its own toggle immediately when it becomes
    /// visible, instead of waiting for the next SetXRViewEnabled/SetDisplayAvatar call.</summary>
    public bool IsXRViewEnabled => isXRViewEnabled;
    public bool IsAvatarDisplayed => isAvatarDisplayed;

    public void SetXRViewEnabled(bool value)
    {
        isXRViewEnabled = value;

        PlayerManager.Instance.DisplayControllers(value);
        worldSpaceUI.gameObject.SetActive(value);

        if (value) ResetWorldSpaceCanvasPositionAndRotation();

        OnXRViewChanged?.Invoke(value);
    }

    public void SetDisplayAvatar(bool value)
    {
        isAvatarDisplayed = value;

        PlayerManager.Instance.DisplayAvatar(value);
        OnAvatarDisplayChanged?.Invoke(value);
    }

    public void ResetHeadsetOrientation()
    {
        ResetXROrigin.Instance.ResetOrigin();
    }

    public Vector3 GetStaticCameraPosition() => staticCamera != null ? staticCamera.position : Vector3.zero;
    public Vector3 GetStaticCameraRotation() => staticCamera != null ? staticCamera.rotation.eulerAngles : Vector3.zero;

    public void SetStaticCameraPosition(Vector3 pos)
    {
        if (staticCamera == null) return;
        staticCamera.position = pos;
        OnStaticCameraPositionChanged?.Invoke(pos);
    }

    public void SetStaticCameraRotation(Vector3 euler)
    {
        if (staticCamera == null) return;
        staticCamera.rotation = Quaternion.Euler(euler);
        OnStaticCameraRotationChanged?.Invoke(euler);
    }

    // ----------------------------------------------------------------- Bridge callbacks (from VR headset editing)
    // These are called by PointCloudUIBridge when a value is changed from inside the headset.
    // Static camera / avatar are shared (handled here); canvas UI is Display-tab specific
    // (forwarded); point cloud entries are Camera-tab specific and paired/handled directly by
    // CameraConfigTabUI, not routed through here.

    public void ApplyStaticCameraPositionFromWS(Vector3 pos) => SetStaticCameraPosition(pos);
    public void ApplyStaticCameraRotationFromWS(Vector3 euler) => SetStaticCameraRotation(euler);
    public void ApplyAvatarToggleFromWS(bool value) => SetDisplayAvatar(value);

    public void ApplyCanvasUIPositionFromWS(Vector3 pos) => displayTab.ApplyCanvasUIPositionFromWS(pos);
    public void ApplyCanvasUIRotationFromWS(Vector3 euler) => displayTab.ApplyCanvasUIRotationFromWS(euler);
    public void ApplyCanvasUIColorFromWS(Color color) => displayTab.ApplyCanvasUIColorFromWS(color);
    public void ApplyDisplayCanvasUIFromWS(bool value) => displayTab.ApplyDisplayCanvasUIFromWS(value);
}