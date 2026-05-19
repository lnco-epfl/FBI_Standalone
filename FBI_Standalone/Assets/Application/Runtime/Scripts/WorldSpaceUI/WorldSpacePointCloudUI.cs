using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldSpacePointCloudUI : MonoBehaviour
{
    [Header("Point Cloud Entries")]
    [SerializeField] private GameObject pointCloudEntryPrefab;
    [SerializeField] private Transform  pointCloudContainer;

    [Header("Canvas UI")]
    [SerializeField] private UISwitcher.UISwitcher displayCanvaUIToggle;
    [SerializeField] private Image  canvaUIAlphaPreview;
    [SerializeField] private Button canvaUIColorPickerButton;

    [SerializeField] private Slider canvaUIPositionXSlider;
    [SerializeField] private Slider canvaUIPositionYSlider;
    [SerializeField] private Slider canvaUIPositionZSlider;
    [SerializeField] private Slider canvaUIRotationXSlider;
    [SerializeField] private Slider canvaUIRotationYSlider;
    [SerializeField] private Slider canvaUIRotationZSlider;

    [Header("Canvas UI Slider Range")]
    [SerializeField] private float canvasPositionRange = 5f;
    [SerializeField] private float canvasRotationRange = 180f;

    [Header("Color Picker")]
    [SerializeField] private GameObject flexibleColorPickerPrefab;

    private PointCloudUIBridge              bridge;
    private FlexibleColorPicker             flexibleColorPicker;
    private List<WorldSpacePointCloudEntry> wsEntries = new List<WorldSpacePointCloudEntry>();

    private void Awake()
    {
        gameObject.SetActive(false);
        SetupCanvasUISliders();
    }

    private void SetupCanvasUISliders()
    {
        foreach (var s in new[] { canvaUIPositionXSlider, canvaUIPositionYSlider, canvaUIPositionZSlider })
        {
            if (s == null) continue;
            s.minValue = -canvasPositionRange;
            s.maxValue =  canvasPositionRange;
        }
        foreach (var s in new[] { canvaUIRotationXSlider, canvaUIRotationYSlider, canvaUIRotationZSlider })
        {
            if (s == null) continue;
            s.minValue = -canvasRotationRange;
            s.maxValue =  canvasRotationRange;
        }
    }

    private void OnEnable()
    {
        if (displayCanvaUIToggle) displayCanvaUIToggle.onValueChanged.AddListener(OnDisplayCanvaUIChanged);
        if (canvaUIColorPickerButton) canvaUIColorPickerButton.onClick.AddListener(OnColorPickerButtonPress);

        canvaUIPositionXSlider?.onValueChanged.AddListener(_ => OnCanvasUIPositionChanged());
        canvaUIPositionYSlider?.onValueChanged.AddListener(_ => OnCanvasUIPositionChanged());
        canvaUIPositionZSlider?.onValueChanged.AddListener(_ => OnCanvasUIPositionChanged());
        canvaUIRotationXSlider?.onValueChanged.AddListener(_ => OnCanvasUIRotationChanged());
        canvaUIRotationYSlider?.onValueChanged.AddListener(_ => OnCanvasUIRotationChanged());
        canvaUIRotationZSlider?.onValueChanged.AddListener(_ => OnCanvasUIRotationChanged());
    }

    private void OnDisable()
    {
        if (displayCanvaUIToggle) displayCanvaUIToggle.onValueChanged.RemoveListener(OnDisplayCanvaUIChanged);
        if (canvaUIColorPickerButton) canvaUIColorPickerButton.onClick.RemoveListener(OnColorPickerButtonPress);

        canvaUIPositionXSlider?.onValueChanged.RemoveAllListeners();
        canvaUIPositionYSlider?.onValueChanged.RemoveAllListeners();
        canvaUIPositionZSlider?.onValueChanged.RemoveAllListeners();
        canvaUIRotationXSlider?.onValueChanged.RemoveAllListeners();
        canvaUIRotationYSlider?.onValueChanged.RemoveAllListeners();
        canvaUIRotationZSlider?.onValueChanged.RemoveAllListeners();

        foreach (var entry in wsEntries)
            entry.OnDisplayToggleRequested -= OnEntryDisplayToggleRequested;
    }

    public void SetBridge(PointCloudUIBridge b)
    {
        bridge = b;
        gameObject.SetActive(true);
    }

    public void SetFollowTarget(Transform target) { }

    // Canvas UI callbacks

    private void OnDisplayCanvaUIChanged(bool v) => bridge?.RequestDisplayCanvasUI(v);

    private void OnCanvasUIPositionChanged() =>
        bridge?.RequestCanvasUIPosition(
            canvaUIPositionXSlider != null ? canvaUIPositionXSlider.value : 0f,
            canvaUIPositionYSlider != null ? canvaUIPositionYSlider.value : 0f,
            canvaUIPositionZSlider != null ? canvaUIPositionZSlider.value : 0f);

    private void OnCanvasUIRotationChanged() =>
        bridge?.RequestCanvasUIRotation(
            canvaUIRotationXSlider != null ? canvaUIRotationXSlider.value : 0f,
            canvaUIRotationYSlider != null ? canvaUIRotationYSlider.value : 0f,
            canvaUIRotationZSlider != null ? canvaUIRotationZSlider.value : 0f);

    private void OnColorPickerButtonPress()
    {
        if (flexibleColorPicker != null || flexibleColorPickerPrefab == null) return;

        var go = Instantiate(flexibleColorPickerPrefab, transform);
        flexibleColorPicker = go.GetComponent<FlexibleColorPicker>();
        flexibleColorPicker.color = canvaUIAlphaPreview != null ? canvaUIAlphaPreview.color : Color.white;

        var destroyer = go.GetComponent<DestroyOnButtonClick>();
        if (destroyer != null) destroyer.OnBeforeDestroy += () => flexibleColorPicker = null;

        flexibleColorPicker.onColorChange.AddListener(OnColorChanged);
    }

    private void OnColorChanged(Color color)
    {
        if (canvaUIAlphaPreview) canvaUIAlphaPreview.color = color;
        bridge?.RequestCanvasUIColor(color);
    }

    // Point Cloud Entries

    public void SpawnPointCloudEntries(List<int> cameraIds)
    {
        ClearEntries();
        foreach (var id in cameraIds)
        {
            var go    = Instantiate(pointCloudEntryPrefab, pointCloudContainer);
            go.name   = $"WSPointCloudEntry_Camera{id}";
            var entry = go.GetComponent<WorldSpacePointCloudEntry>();
            entry.Init(id);
            entry.SetInteractable(false);
            entry.SetDisplayToggle(false);
            entry.OnDisplayToggleRequested += OnEntryDisplayToggleRequested;
            wsEntries.Add(entry);
        }
    }

    public List<WorldSpacePointCloudEntry> GetEntries() => wsEntries;

    private void ClearEntries()
    {
        foreach (var entry in wsEntries) entry.OnDisplayToggleRequested -= OnEntryDisplayToggleRequested;
        wsEntries.Clear();
        while (pointCloudContainer != null && pointCloudContainer.childCount > 0)
            DestroyImmediate(pointCloudContainer.GetChild(0).gameObject);
    }

    private void OnEntryDisplayToggleRequested(WorldSpacePointCloudEntry entry, bool state)
        => bridge?.RequestDisplayToggle(entry.CameraId, state);

    // Mirror API

    public void MirrorEntryInteractable(bool interactable)
    {
        foreach (var e in wsEntries) e.SetInteractable(interactable);
    }

    public void MirrorEntryDisplayState(int cameraId, bool isOn)
    {
        var entry = wsEntries.Find(e => e.CameraId == cameraId);
        entry?.ApplyDisplayState(isOn);
    }

    public void MirrorEntryData(int index, Vector3 pos, Vector3 rot,
        float depthMin, float depthMax, bool flipX, bool flipY)
    {
        if (index < 0 || index >= wsEntries.Count) return;
        wsEntries[index].SetPositionFields(pos);
        wsEntries[index].SetRotationFields(rot);
        wsEntries[index].SetMinDepth(depthMin);
        wsEntries[index].SetMaxDepth(depthMax);
        wsEntries[index].SetFlip(flipX, flipY);
    }

    public void MirrorDisplayCanvasUIToggle(bool value) => displayCanvaUIToggle?.SetWithoutNotify(value);

    public void MirrorCanvasUIPosition(Vector3 pos)
    {
        canvaUIPositionXSlider?.SetValueWithoutNotify(pos.x);
        canvaUIPositionYSlider?.SetValueWithoutNotify(pos.y);
        canvaUIPositionZSlider?.SetValueWithoutNotify(pos.z);
    }

    public void MirrorCanvasUIRotation(Vector3 rot)
    {
        canvaUIRotationXSlider?.SetValueWithoutNotify(NormalizeAngle(rot.x));
        canvaUIRotationYSlider?.SetValueWithoutNotify(NormalizeAngle(rot.y));
        canvaUIRotationZSlider?.SetValueWithoutNotify(NormalizeAngle(rot.z));
    }

    public void MirrorCanvasUIColor(Color color)
    {
        if (canvaUIAlphaPreview) canvaUIAlphaPreview.color = color;
        if (flexibleColorPicker != null) flexibleColorPicker.color = color;
    }

    // Stubs for bridge compatibility
    public void MirrorStatus(string message, Color color) { }
    public void MirrorFileName(string name) { }
    public void MirrorSceneDropdownOptions(List<string> options, int selectedIndex) { }
    public void MirrorSceneDropdownSelection(int index) { }

    private static float NormalizeAngle(float angle)
    {
        while (angle >  180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
