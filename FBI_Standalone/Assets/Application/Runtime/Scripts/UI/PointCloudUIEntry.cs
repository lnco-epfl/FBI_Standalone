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
    private SensorData sensorData;
    private Kinect4AzureInterface kinectInerface;

    /// <summary>
    /// Fired when the user clicks the display toggle. The bool indicates the desired state.
    /// The parent (CanvasSetupPointCloudUI) handles exclusivity and the switch delay.
    /// </summary>
    public event Action<PointCloudUIEntry, bool> OnDisplayToggleRequested;

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

        CaptureDefaults();
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
    }

    private void OnPositionOrRotationChanged()
    {
        var t = PointCloudManager.Instance.GetVisualEffectTransform(CameraId);
        t.position = Position;
        t.rotation = Quaternion.Euler(Rotation);
        PointCloudManager.Instance.SetVisualEffectTransform(t, CameraId);
        ConfigFileManager.Instance.SaveObjectTransform(CameraId, t);
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
        //displayPointCloudToggle.SetWithoutNotify(!isOn);

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
        if(kinectInerface != null)
        {
            kinectInerface.maxDepthDistance = value;
        }
        ConfigFileManager.Instance.SaveDepthMax(CameraId, value);
    }

    private void OnDepthMinChanged(float value)
    {
        if (kinectInerface != null)
        {
            kinectInerface.minDepthDistance = value;
        }
        ConfigFileManager.Instance.SaveDepthMin(CameraId, value);
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
    }

    private void OnFlipYChanged(bool isOn)
    {
        ApplyFlip(flipXToggle.isOn, isOn);
        ConfigFileManager.Instance.SaveFlip(CameraId, flipXToggle.isOn, isOn);
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

    private (Vector3 position, Vector3 rotation, float depthMin, float depthMax, bool flipX, bool flipY) initialDefaults;

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
            t.localScale.y < 0
        );
    }

    public void ResetToDefaults()
    {
        SetPositionFields(initialDefaults.position);
        SetRotationFields(initialDefaults.rotation);
        SetMinDepth(initialDefaults.depthMin);
        SetMaxDepth(initialDefaults.depthMax);
        SetFlip(initialDefaults.flipX, initialDefaults.flipY);
    }
}