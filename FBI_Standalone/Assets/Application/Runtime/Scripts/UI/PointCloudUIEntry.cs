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
    private SensorData sensorData;
    private Kinect4AzureInterface kinectInerface;

    public event Action<PointCloudUIEntry> OnPositionChanged;
    public event Action<PointCloudUIEntry> OnRotationChanged;

    /// <summary>
    /// Fired when the user clicks the display toggle. The bool indicates the desired state.
    /// The parent (CanvasSetupPointCloudUI) handles exclusivity and the switch delay.
    /// </summary>
    public event Action<PointCloudUIEntry, bool> OnDisplayToggleRequested;

    public int CameraId { get; private set; }

    public Vector3 Position => new Vector3(
        float.Parse(positionXInputField.text),
        float.Parse(positionYInputField.text),
        float.Parse(positionZInputField.text)
    );

    public Vector3 Rotation => new Vector3(
        float.Parse(rotationXInputField.text),
        float.Parse(rotationYInputField.text),
        float.Parse(rotationZInputField.text)
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
            cameraDepthMaxSlider.value = kinectInerface.maxDepthDistance;

            cameraDepthMinSlider.minValue = 0.0f;
            cameraDepthMinSlider.maxValue = 10.0f;
            cameraDepthMinSlider.value = kinectInerface.minDepthDistance;
        }
    }

    private void OnEnable()
    {
        positionXInputField.onValueChanged.AddListener((str) => OnPositionChanged?.Invoke(this));
        positionYInputField.onValueChanged.AddListener((str) => OnPositionChanged?.Invoke(this));
        positionZInputField.onValueChanged.AddListener((str) => OnPositionChanged?.Invoke(this));

        rotationXInputField.onValueChanged.AddListener((str) => OnRotationChanged?.Invoke(this));
        rotationYInputField.onValueChanged.AddListener((str) => OnRotationChanged?.Invoke(this));
        rotationZInputField.onValueChanged.AddListener((str) => OnRotationChanged?.Invoke(this));

        cameraDepthMaxSlider.onValueChanged.AddListener(OnDepthMaxChanged);
        cameraDepthMinSlider.onValueChanged.AddListener(OnDepthMinChanged);

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

        displayPointCloudToggle.onValueChanged.RemoveListener(OnDisplayToggleChanged);
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
            container.realtimeDelaySwitcher.enabled = true;
            container.replayBuffer.enabled = false;
            container.vfx.enabled = true;
        }
        else
        {
            container.realtimeDelaySwitcher.enabled = false;
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
    }

    public void SetMaxDepth(float depthMax)
    {
        if (kinectInerface != null)
        {
            kinectInerface.maxDepthDistance = depthMax;
        }
        cameraDepthMaxSlider.SetValueWithoutNotify(depthMax);
    }
}