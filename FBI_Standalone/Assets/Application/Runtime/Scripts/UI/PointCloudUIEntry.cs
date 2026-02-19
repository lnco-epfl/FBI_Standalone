using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class PointCloudUIEntry : MonoBehaviour
{

    [Header("Title")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private LocalizedString titleLocalizedString;
    [SerializeField] private Toggle displayPointCloudToggle;

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

    public event Action<PointCloudUIEntry> OnPositionChanged;
    public event Action<PointCloudUIEntry> OnRotationChanged;

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

    public void Init(int cameraId)
    {
        CameraId = cameraId;

        titleText.text = titleLocalizedString.GetLocalizedString(CameraId);
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
        displayPointCloudToggle.SetIsOnWithoutNotify(isOn);
    }

    public void SetInteractable(bool interactable)
    {
        GetComponent<CanvasGroup>().interactable = interactable;
    }

    private void OnDepthMaxChanged(float value)
    {
        ConfigFileManager.Instance.SaveDepthMax(CameraId, value);
    }

    private void OnDepthMinChanged(float value)
    {
        ConfigFileManager.Instance.SaveDepthMin(CameraId, value);
    }
}