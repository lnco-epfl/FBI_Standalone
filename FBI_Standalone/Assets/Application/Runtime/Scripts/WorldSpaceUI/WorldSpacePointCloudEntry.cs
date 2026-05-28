using System;
using com.rfilkov.kinect;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class WorldSpacePointCloudEntry : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private LocalizedString titleLocalizedString;
    [SerializeField] private UISwitcher.UISwitcher displayPointCloudToggle;

    [Header("Transform")]
    [SerializeField] private Slider positionXSlider;
    [SerializeField] private Slider positionYSlider;
    [SerializeField] private Slider positionZSlider;
    [SerializeField] private Slider rotationXSlider;
    [SerializeField] private Slider rotationYSlider;
    [SerializeField] private Slider rotationZSlider;

    [Header("Transform Range")]
    [SerializeField] private float positionRange = 5f;
    [SerializeField] private float rotationRange = 180f;

    [Header("Camera Settings")]
    [SerializeField] private Slider cameraDepthMaxSlider;
    [SerializeField] private Slider cameraDepthMinSlider;
    [SerializeField] private Toggle flipXToggle;
    [SerializeField] private Toggle flipYToggle;

    [Header("Clamp")]
    [SerializeField] private Slider clampXMinSlider;
    [SerializeField] private Slider clampXMaxSlider;
    [SerializeField] private Slider clampYMinSlider;
    [SerializeField] private Slider clampYMaxSlider;

    public event Action<WorldSpacePointCloudEntry, bool> OnDisplayToggleRequested;

    public int CameraId { get; private set; }

    private PointCloudUIEntry pairedOverlayEntry;
    private Kinect4AzureInterface kinectInterface;

    private void Awake()
    {
        if (displayPointCloudToggle == null)
        {
            displayPointCloudToggle = GetComponentInChildren<UISwitcher.UISwitcher>();
        }    
    }

    public void Init(int cameraId)
    {
        CameraId = cameraId;

        if (titleText && titleLocalizedString != null)
        {
            titleText.text = titleLocalizedString.GetLocalizedString(cameraId);
        }

        SetupPositionSliders();
        SetupRotationSliders();

        var sensorData = KinectManager.Instance != null && KinectManager.Instance.IsInitialized() ? KinectManager.Instance.GetSensorData(cameraId - 1) : null;

        if (sensorData?.sensorInterface != null)
        {
            kinectInterface = (Kinect4AzureInterface)sensorData.sensorInterface;

            if (cameraDepthMaxSlider) { cameraDepthMaxSlider.minValue = 0f; cameraDepthMaxSlider.maxValue = 10f; cameraDepthMaxSlider.value = kinectInterface.maxDepthDistance; }
            if (cameraDepthMinSlider) { cameraDepthMinSlider.minValue = 0f; cameraDepthMinSlider.maxValue = 10f; cameraDepthMinSlider.value = kinectInterface.minDepthDistance; }
        }
    }

    private void SetupPositionSliders()
    {
        foreach (var s in new[] { positionXSlider, positionYSlider, positionZSlider })
        {
            if (s == null) continue;
            s.minValue = -positionRange;
            s.maxValue =  positionRange;
        }
    }

    private void SetupRotationSliders()
    {
        foreach (var s in new[] { rotationXSlider, rotationYSlider, rotationZSlider })
        {
            if (s == null) continue;
            s.minValue = -rotationRange;
            s.maxValue =  rotationRange;
        }
    }

    public void SetPairedEntry(PointCloudUIEntry overlayEntry) => pairedOverlayEntry = overlayEntry;

    private void OnEnable()
    {
        positionXSlider?.onValueChanged.AddListener(_ => OnTransformChanged());
        positionYSlider?.onValueChanged.AddListener(_ => OnTransformChanged());
        positionZSlider?.onValueChanged.AddListener(_ => OnTransformChanged());
        rotationXSlider?.onValueChanged.AddListener(_ => OnTransformChanged());
        rotationYSlider?.onValueChanged.AddListener(_ => OnTransformChanged());
        rotationZSlider?.onValueChanged.AddListener(_ => OnTransformChanged());

        cameraDepthMaxSlider?.onValueChanged.AddListener(OnDepthMaxChanged);
        cameraDepthMinSlider?.onValueChanged.AddListener(OnDepthMinChanged);

        flipXToggle?.onValueChanged.AddListener(OnFlipXChanged);
        flipYToggle?.onValueChanged.AddListener(OnFlipYChanged);

        clampXMinSlider?.onValueChanged.AddListener(_ => OnClampChanged());
        clampXMaxSlider?.onValueChanged.AddListener(_ => OnClampChanged());
        clampYMinSlider?.onValueChanged.AddListener(_ => OnClampChanged());
        clampYMaxSlider?.onValueChanged.AddListener(_ => OnClampChanged());

        displayPointCloudToggle?.onValueChanged.AddListener(OnDisplayToggleChanged);
    }

    private void OnDisable()
    {
        positionXSlider?.onValueChanged.RemoveAllListeners();
        positionYSlider?.onValueChanged.RemoveAllListeners();
        positionZSlider?.onValueChanged.RemoveAllListeners();
        rotationXSlider?.onValueChanged.RemoveAllListeners();
        rotationYSlider?.onValueChanged.RemoveAllListeners();
        rotationZSlider?.onValueChanged.RemoveAllListeners();

        cameraDepthMaxSlider?.onValueChanged.RemoveAllListeners();
        cameraDepthMinSlider?.onValueChanged.RemoveAllListeners();

        flipXToggle?.onValueChanged.RemoveListener(OnFlipXChanged);
        flipYToggle?.onValueChanged.RemoveListener(OnFlipYChanged);

        clampXMinSlider?.onValueChanged.RemoveAllListeners();
        clampXMaxSlider?.onValueChanged.RemoveAllListeners();
        clampYMinSlider?.onValueChanged.RemoveAllListeners();
        clampYMaxSlider?.onValueChanged.RemoveAllListeners();

        displayPointCloudToggle?.onValueChanged.RemoveListener(OnDisplayToggleChanged);
    }

    private void OnTransformChanged()
    {
        if (pairedOverlayEntry == null) return;

        pairedOverlayEntry.SetPositionFields(Position);
        pairedOverlayEntry.SetRotationFields(Rotation);

        var t = PointCloudManager.Instance.GetPointCloud(CameraId).transform;
        t.position = Position;
        t.rotation = Quaternion.Euler(Rotation);

        ConfigFileManager.Instance.SaveObjectTransform(CameraId, t);
    }

    private void OnDepthMaxChanged(float value)
    {
        if (kinectInterface != null) kinectInterface.maxDepthDistance = value;
        ConfigFileManager.Instance.SaveDepthMax(CameraId, value);
        pairedOverlayEntry?.SetMaxDepth(value);
    }

    private void OnDepthMinChanged(float value)
    {
        if (kinectInterface != null) kinectInterface.minDepthDistance = value;
        ConfigFileManager.Instance.SaveDepthMin(CameraId, value);
        pairedOverlayEntry?.SetMinDepth(value);
    }

    private void OnFlipXChanged(bool isOn)
    {
        bool flipY = flipYToggle != null && flipYToggle.isOn;
        ApplyFlip(isOn, flipY);
        ConfigFileManager.Instance.SaveFlip(CameraId, isOn, flipY);
        pairedOverlayEntry?.SetFlip(isOn, flipY);
    }

    private void OnFlipYChanged(bool isOn)
    {
        bool flipX = flipXToggle != null && flipXToggle.isOn;
        ApplyFlip(flipX, isOn);
        ConfigFileManager.Instance.SaveFlip(CameraId, flipX, isOn);
        pairedOverlayEntry?.SetFlip(flipX, isOn);
    }

    private void OnClampChanged()
    {
        float xMin = clampXMinSlider != null ? clampXMinSlider.value : 0f;
        float xMax = clampXMaxSlider != null ? clampXMaxSlider.value : 1f;
        float yMin = clampYMinSlider != null ? clampYMinSlider.value : 0f;
        float yMax = clampYMaxSlider != null ? clampYMaxSlider.value : 1f;
        ConfigFileManager.Instance.SaveClamp(CameraId, xMin, xMax, yMin, yMax);
        pairedOverlayEntry?.SetClamp(xMin, xMax, yMin, yMax);
    }

    private void OnDisplayToggleChanged(bool isOn) => OnDisplayToggleRequested?.Invoke(this, isOn);

    // Mirror API

    public void SetPositionFields(Vector3 position)
    {
        positionXSlider?.SetValueWithoutNotify(position.x);
        positionYSlider?.SetValueWithoutNotify(position.y);
        positionZSlider?.SetValueWithoutNotify(position.z);
    }

    public void SetRotationFields(Vector3 rotation)
    {
        // Remap 0-360 to -180/+180 for slider range
        rotationXSlider?.SetValueWithoutNotify(NormalizeAngle(rotation.x));
        rotationYSlider?.SetValueWithoutNotify(NormalizeAngle(rotation.y));
        rotationZSlider?.SetValueWithoutNotify(NormalizeAngle(rotation.z));
    }

    public void SetMinDepth(float depthMin)
    {
        if (kinectInterface != null) kinectInterface.minDepthDistance = depthMin;
        cameraDepthMinSlider?.SetValueWithoutNotify(depthMin);
        cameraDepthMinSlider?.GetComponent<SliderToText>()?.UpdateText(depthMin);
    }

    public void SetMaxDepth(float depthMax)
    {
        if (kinectInterface != null) kinectInterface.maxDepthDistance = depthMax;
        cameraDepthMaxSlider?.SetValueWithoutNotify(depthMax);
        cameraDepthMaxSlider?.GetComponent<SliderToText>()?.UpdateText(depthMax);
    }

    public void SetFlip(bool flipX, bool flipY)
    {
        flipXToggle?.SetIsOnWithoutNotify(flipX);
        flipXToggle?.GetComponent<ToggleButton>()?.OnValueChanged(flipX);
        flipYToggle?.SetIsOnWithoutNotify(flipY);
        flipYToggle?.GetComponent<ToggleButton>()?.OnValueChanged(flipY);
        ApplyFlip(flipX, flipY);
    }

    public void SetClamp(float xMin, float xMax, float yMin, float yMax)
    {
        clampXMinSlider?.SetValueWithoutNotify(xMin);
        clampXMaxSlider?.SetValueWithoutNotify(xMax);
        clampYMinSlider?.SetValueWithoutNotify(yMin);
        clampYMaxSlider?.SetValueWithoutNotify(yMax);
    }

    public void ApplyDisplayState(bool isOn)
    {
        displayPointCloudToggle?.SetWithoutNotify(isOn);
        var pointCloud = PointCloudManager.Instance.GetPointCloud(CameraId);
        if (pointCloud == null) return;

        if (isOn)
        {
            pointCloud.DisplayMain();
        }
        else
        {
            pointCloud.HideMain();
        }
    }

    public void SetDisplayToggle(bool isOn) => displayPointCloudToggle?.SetWithoutNotify(isOn);

    public void SetInteractable(bool interactable)
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg == null) return;
        cg.interactable   = interactable;
        cg.blocksRaycasts = interactable;
    }

    private Vector3 Position => new Vector3(
        positionXSlider != null ? positionXSlider.value : 0f,
        positionYSlider != null ? positionYSlider.value : 0f,
        positionZSlider != null ? positionZSlider.value : 0f);

    private Vector3 Rotation => new Vector3(
        rotationXSlider != null ? rotationXSlider.value : 0f,
        rotationYSlider != null ? rotationYSlider.value : 0f,
        rotationZSlider != null ? rotationZSlider.value : 0f);

    private static float NormalizeAngle(float angle)
    {
        while (angle >  180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private void ApplyFlip(bool flipX, bool flipY)
    {
        var pointCloud = PointCloudManager.Instance.GetPointCloud(CameraId);
        if (pointCloud == null) return;
        Transform t  = pointCloud.transform;
        Vector3 scale = t.localScale;
        scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);
        scale.y = Mathf.Abs(scale.y) * (flipY ? -1f : 1f);
        t.localScale = scale;
    }

}
