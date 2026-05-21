using System;
using com.rfilkov.kinect;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using YamlDotNet.Core.Tokens;
using static com.rfilkov.kinect.KinectInterop;

public class PointCloudUIEntry : MonoBehaviour
{

    [Header("Title")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private LocalizedString titleLocalizedString;
    [SerializeField] private UISwitcher.UISwitcher displayPointCloudToggle;

    [Header("Transform")]
    [SerializeField] private TMP_InputField positionXInputField;
    [SerializeField] private TMP_InputField positionYInputField;
    [SerializeField] private TMP_InputField positionZInputField;
    [SerializeField] private TMP_InputField rotationXInputField;
    [SerializeField] private TMP_InputField rotationYInputField;
    [SerializeField] private TMP_InputField rotationZInputField;

    [Header("Camera Settings")]
    [SerializeField] private Slider cameraDepthMaxSlider;
    [SerializeField] private Slider cameraDepthMinSlider;
    [SerializeField] private Toggle flipXToggle;
    [SerializeField] private Toggle flipYToggle;

    [Header("Clamp Settings")]
    [SerializeField] private Slider clampXMinSlider;
    [SerializeField] private Slider clampXMaxSlider;
    [SerializeField] private Slider clampYMinSlider;
    [SerializeField] private Slider clampYMaxSlider;

    private SensorData sensorData;
    private Kinect4AzureInterface kinectInerface;

    public event Action<PointCloudUIEntry, bool> OnDisplayToggleRequested;

    public event Action<int, Vector3, Vector3> OnTransformChanged;
    public event Action<int, float> OnMaxDepthChanged;
    public event Action<int, float> OnMinDepthChanged;
    public event Action<int, bool, bool> OnFlipChanged;
    public event Action<int, float, float, float, float> OnClampChanged;

    public float DepthMin => kinectInerface != null ? kinectInerface.minDepthDistance : 0f;
    public float DepthMax => kinectInerface != null ? kinectInerface.maxDepthDistance : 10f;
    public bool FlipX => flipXToggle != null && flipXToggle.isOn;
    public bool FlipY => flipYToggle != null && flipYToggle.isOn;

    public int CameraId { get; private set; }

    private static float ParseField(TMP_InputField field, float fallback = 0f)
    {
        if (field == null) return fallback;
        return float.TryParse(field.text, out float result) ? result : fallback;
    }

    public Vector3 Position => new Vector3(
        ParseField(positionXInputField),
        ParseField(positionYInputField),
        ParseField(positionZInputField)
    );

    public Vector3 Rotation => new Vector3(
        ParseField(rotationXInputField),
        ParseField(rotationYInputField),
        ParseField(rotationZInputField)
    );

    private void Awake()
    {
        displayPointCloudToggle = GetComponentInChildren<UISwitcher.UISwitcher>();
    }

    public void Init(int cameraId)
    {
        CameraId = cameraId;

        titleText.text = titleLocalizedString.GetLocalizedString(CameraId);

        sensorData = KinectManager.Instance != null && KinectManager.Instance.IsInitialized() ? KinectManager.Instance.GetSensorData(cameraId - 1) : null;

        if (sensorData != null && sensorData.sensorInterface != null)
        {
            kinectInerface = (Kinect4AzureInterface)sensorData.sensorInterface;

            cameraDepthMaxSlider.minValue = 0.0f;
            cameraDepthMaxSlider.maxValue = 10.0f;
            cameraDepthMaxSlider.SetValueWithoutNotify(kinectInerface.maxDepthDistance);

            cameraDepthMinSlider.minValue = 0.0f;
            cameraDepthMinSlider.maxValue = 10.0f;
            cameraDepthMinSlider.SetValueWithoutNotify(kinectInerface.minDepthDistance);
        }

        // Init clamp sliders
        InitClampSlider(clampXMinSlider, 0f);
        InitClampSlider(clampXMaxSlider, 1f);
        InitClampSlider(clampYMinSlider, 0f);
        InitClampSlider(clampYMaxSlider, 1f);

        CaptureDefaults();
    }

    private void InitClampSlider(Slider slider, float defaultValue)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(defaultValue);
    }

    private void OnEnable()
    {
        positionXInputField.onValueChanged.AddListener((str) => { OnPositionOrRotationChanged(); });
        positionYInputField.onValueChanged.AddListener((str) => { OnPositionOrRotationChanged(); });
        positionZInputField.onValueChanged.AddListener((str) => { OnPositionOrRotationChanged(); });

        rotationXInputField.onValueChanged.AddListener((str) => { OnPositionOrRotationChanged(); });
        rotationYInputField.onValueChanged.AddListener((str) => { OnPositionOrRotationChanged(); });
        rotationZInputField.onValueChanged.AddListener((str) => { OnPositionOrRotationChanged(); });

        cameraDepthMaxSlider.onValueChanged.AddListener(OnDepthMaxChanged);
        cameraDepthMinSlider.onValueChanged.AddListener(OnDepthMinChanged);

        flipXToggle.onValueChanged.AddListener(OnFlipXChanged);
        flipYToggle.onValueChanged.AddListener(OnFlipYChanged);

        displayPointCloudToggle.onValueChanged.AddListener(OnDisplayToggleChanged);

        if (clampXMinSlider != null) clampXMinSlider.onValueChanged.AddListener(OnClampSliderChanged);
        if (clampXMaxSlider != null) clampXMaxSlider.onValueChanged.AddListener(OnClampSliderChanged);
        if (clampYMinSlider != null) clampYMinSlider.onValueChanged.AddListener(OnClampSliderChanged);
        if (clampYMaxSlider != null) clampYMaxSlider.onValueChanged.AddListener(OnClampSliderChanged);
    }

    private void OnDisable()
    {
        positionXInputField.onValueChanged.RemoveAllListeners();
        positionYInputField.onValueChanged.RemoveAllListeners();
        positionZInputField.onValueChanged.RemoveAllListeners();

        rotationXInputField.onValueChanged.RemoveAllListeners();
        rotationYInputField.onValueChanged.RemoveAllListeners();
        rotationZInputField.onValueChanged.RemoveAllListeners();

        cameraDepthMaxSlider.onValueChanged.RemoveAllListeners();
        cameraDepthMinSlider.onValueChanged.RemoveAllListeners();

        flipXToggle.onValueChanged.RemoveListener(OnFlipXChanged);
        flipYToggle.onValueChanged.RemoveListener(OnFlipYChanged);

        displayPointCloudToggle.onValueChanged.RemoveListener(OnDisplayToggleChanged);

        if (clampXMinSlider != null) clampXMinSlider.onValueChanged.RemoveListener(OnClampSliderChanged);
        if (clampXMaxSlider != null) clampXMaxSlider.onValueChanged.RemoveListener(OnClampSliderChanged);
        if (clampYMinSlider != null) clampYMinSlider.onValueChanged.RemoveListener(OnClampSliderChanged);
        if (clampYMaxSlider != null) clampYMaxSlider.onValueChanged.RemoveListener(OnClampSliderChanged);
    }

    public void ForceApplyAndSave()
    {
        OnPositionOrRotationChanged();

        if (kinectInerface != null)
        {
            ConfigFileManager.Instance.SaveDepthMax(CameraId, kinectInerface.maxDepthDistance, saveImmediately: false);
            ConfigFileManager.Instance.SaveDepthMin(CameraId, kinectInerface.minDepthDistance, saveImmediately: false);
        }

        var t = PointCloudManager.Instance.GetVisualEffectTransform(CameraId);
        ConfigFileManager.Instance.SaveFlip(CameraId, t.localScale.x < 0, t.localScale.y < 0, saveImmediately: false);

        ConfigFileManager.Instance.SaveClamp(CameraId,
            clampXMinSlider != null ? clampXMinSlider.value : 0f,
            clampXMaxSlider != null ? clampXMaxSlider.value : 1f,
            clampYMinSlider != null ? clampYMinSlider.value : 0f,
            clampYMaxSlider != null ? clampYMaxSlider.value : 1f,
            saveImmediately: false);
    }

    private void OnPositionOrRotationChanged()
    {
        var t = PointCloudManager.Instance.GetVisualEffectTransform(CameraId);
        t.position = Position;
        t.rotation = Quaternion.Euler(Rotation);
        PointCloudManager.Instance.SetVisualEffectTransform(t, CameraId);
        ConfigFileManager.Instance.SaveObjectTransform(CameraId, t);
        OnTransformChanged?.Invoke(CameraId, Position, Rotation);
    }

    public void SetPositionFields(Vector3 position)
    {
        positionXInputField.SetTextWithoutNotify(position.x.ToString());
        positionYInputField.SetTextWithoutNotify(position.y.ToString());
        positionZInputField.SetTextWithoutNotify(position.z.ToString());
    }

    public void SetRotationFields(Vector3 rotation)
    {
        rotationXInputField.SetTextWithoutNotify(rotation.x.ToString());
        rotationYInputField.SetTextWithoutNotify(rotation.y.ToString());
        rotationZInputField.SetTextWithoutNotify(rotation.z.ToString());
    }

    private void OnDisplayToggleChanged(bool isOn)
    {
        OnDisplayToggleRequested?.Invoke(this, isOn);
    }

    public void ApplyDisplayState(bool isOn)
    {
        displayPointCloudToggle.SetWithoutNotify(isOn);

        var container = PointCloudManager.Instance.GetPointCloudContainer(CameraId);
        if (container == null) return;

        if (isOn)
        {
            container.realtimeDelaySwitcher.displayMode = RealtimeDelaySwitcher.DisplayMode.Realtime;
            container.vfx.enabled = true;
        }
        else
        {
            container.vfx.enabled = false;
        }
    }

    public void SetDisplayToggle(bool isOn)
    {
        displayPointCloudToggle.SetWithoutNotify(isOn);
    }

    public void SetInteractable(bool interactable)
    {
        GetComponent<CanvasGroup>().interactable = interactable;
        GetComponent<CanvasGroup>().blocksRaycasts = interactable;
    }

    private void OnDepthMaxChanged(float value)
    {
        if (kinectInerface != null)
        {
            kinectInerface.maxDepthDistance = value;
        }
        ConfigFileManager.Instance.SaveDepthMax(CameraId, value);
        OnMaxDepthChanged?.Invoke(CameraId, value);
    }

    private void OnDepthMinChanged(float value)
    {
        if (kinectInerface != null)
        {
            kinectInerface.minDepthDistance = value;
        }
        ConfigFileManager.Instance.SaveDepthMin(CameraId, value);
        OnMinDepthChanged?.Invoke(CameraId, value);
    }

    public void SetMinDepth(float depthMin)
    {
        if (kinectInerface != null)
        {
            kinectInerface.minDepthDistance = depthMin;
        }
        cameraDepthMinSlider.SetValueWithoutNotify(depthMin);
        cameraDepthMinSlider.GetComponent<SliderToText>().UpdateText(depthMin);
    }

    public void SetMaxDepth(float depthMax)
    {
        if (kinectInerface != null)
        {
            kinectInerface.maxDepthDistance = depthMax;
        }
        cameraDepthMaxSlider.SetValueWithoutNotify(depthMax);
        cameraDepthMaxSlider.GetComponent<SliderToText>().UpdateText(depthMax);
    }

    private void OnFlipXChanged(bool isOn)
    {
        ApplyFlip(isOn, flipYToggle.isOn);
        ConfigFileManager.Instance.SaveFlip(CameraId, isOn, flipYToggle.isOn);
        OnFlipChanged?.Invoke(CameraId, isOn, flipYToggle.isOn);
    }

    private void OnFlipYChanged(bool isOn)
    {
        ApplyFlip(flipXToggle.isOn, isOn);
        ConfigFileManager.Instance.SaveFlip(CameraId, flipXToggle.isOn, isOn);
        OnFlipChanged?.Invoke(CameraId, flipXToggle.isOn, isOn);
    }

    private void ApplyFlip(bool flipX, bool flipY)
    {
        var container = PointCloudManager.Instance.GetPointCloudContainer(CameraId);
        if (container == null) return;

        Transform t = container.vfx.transform;
        Vector3 scale = t.localScale;
        scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);
        scale.y = Mathf.Abs(scale.y) * (flipY ? -1f : 1f);
        t.localScale = scale;
    }

    public void SetFlip(bool flipX, bool flipY)
    {
        flipXToggle.SetIsOnWithoutNotify(flipX);
        flipXToggle.GetComponent<ToggleButton>().OnValueChanged(flipX);
        flipYToggle.SetIsOnWithoutNotify(flipY);
        flipYToggle.GetComponent<ToggleButton>().OnValueChanged(flipY);
        ApplyFlip(flipX, flipY);
    }

    private void OnClampSliderChanged(float value)
    {
        float xMin = clampXMinSlider != null ? clampXMinSlider.value : 0f;
        float xMax = clampXMaxSlider != null ? clampXMaxSlider.value : 1f;
        float yMin = clampYMinSlider != null ? clampYMinSlider.value : 0f;
        float yMax = clampYMaxSlider != null ? clampYMaxSlider.value : 1f;

        ApplyClampToVFX(xMin, xMax, yMin, yMax);
        ConfigFileManager.Instance.SaveClamp(CameraId, xMin, xMax, yMin, yMax);
        OnClampChanged?.Invoke(CameraId, xMin, xMax, yMin, yMax);
    }

    private void ApplyClampToVFX(float xMin, float xMax, float yMin, float yMax)
    {
        var vfx = PointCloudManager.Instance.GetVisualEffect(CameraId);
        if (vfx == null) return;

        vfx.SetFloat("Clamp X Min", xMin);
        vfx.SetFloat("Clamp X Max", xMax);
        vfx.SetFloat("Clamp Y Min", yMin);
        vfx.SetFloat("Clamp Y Max", yMax);
    }

    public void SetClamp(float xMin, float xMax, float yMin, float yMax)
    {
        if (clampXMinSlider != null) clampXMinSlider.SetValueWithoutNotify(xMin); clampXMinSlider.GetComponent<SliderToText>().UpdateText(xMin);
        if (clampXMaxSlider != null) clampXMaxSlider.SetValueWithoutNotify(xMax); clampXMaxSlider.GetComponent<SliderToText>().UpdateText(xMax);
        if (clampYMinSlider != null) clampYMinSlider.SetValueWithoutNotify(yMin); clampYMinSlider.GetComponent<SliderToText>().UpdateText(yMin);
        if (clampYMaxSlider != null) clampYMaxSlider.SetValueWithoutNotify(yMax); clampYMaxSlider.GetComponent<SliderToText>().UpdateText(yMax);

        ApplyClampToVFX(xMin, xMax, yMin, yMax);
    }

    private (Vector3 position, Vector3 rotation, float depthMin, float depthMax, bool flipX, bool flipY, float clampXMin, float clampXMax, float clampYMin, float clampYMax) initialDefaults;

    public void CaptureDefaults()
    {
        var t = PointCloudManager.Instance.GetVisualEffectTransform(CameraId);
        float depthMin = kinectInerface != null ? kinectInerface.minDepthDistance : 0f;
        float depthMax = kinectInerface != null ? kinectInerface.maxDepthDistance : 10f;

        initialDefaults = (
            t.position,
            t.eulerAngles,
            depthMin,
            depthMax,
            t.localScale.x < 0,
            t.localScale.y < 0,
            clampXMinSlider != null ? clampXMinSlider.value : 0f,
            clampXMaxSlider != null ? clampXMaxSlider.value : 1f,
            clampYMinSlider != null ? clampYMinSlider.value : 0f,
            clampYMaxSlider != null ? clampYMaxSlider.value : 1f
        );
    }

    public void ResetToDefaults()
    {
        SetPositionFields(initialDefaults.position);
        SetRotationFields(initialDefaults.rotation);
        SetMinDepth(initialDefaults.depthMin);
        SetMaxDepth(initialDefaults.depthMax);
        SetFlip(initialDefaults.flipX, initialDefaults.flipY);
        SetClamp(initialDefaults.clampXMin, initialDefaults.clampXMax, initialDefaults.clampYMin, initialDefaults.clampYMax);
    }
}
