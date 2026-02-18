using Eflatun.SceneReference;
using Intel.RealSense;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CanvasSetupPointCloudUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text title;

    [Header("New Config")]
    [SerializeField] private TMP_InputField configNameInputField;
    [SerializeField] private Button createConfigButton;
    [SerializeField] private CanvasGroup newConfigCanvasGroup;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private LocalizedString statusLocalizedText;

    [Header("Load Config")]
    [SerializeField] private TMP_Dropdown loadConfigDropdown;
    [SerializeField] private Button loadConfigButton;
    [SerializeField] private CanvasGroup loadConfigCanvasGroup;

    [Header("Point cloud")]
    [SerializeField] private Transform pointCloudPosition;
    private TMP_InputField pointCloudPositionXInputField;
    private TMP_InputField pointCloudPositionYInputField;
    private TMP_InputField pointCloudPositionZInputField;
    [SerializeField] private Transform pointCloudRotation;
    private TMP_InputField pointCloudRotationXInputField;
    private TMP_InputField pointCloudRotationYInputField;
    private TMP_InputField pointCloudRotationZInputField;
    [SerializeField] private CanvasGroup pointCloudCanvasGroup;

    [Header("Scene")]
    [SerializeField] private SceneReference scene;

    public event Action<CanvasSetupPointCloudUI> OnCanvasSetupPointCloudUIDestroy;

    private string selectedConfig;
    private List<string> configs;
    private string newConfigName;

    private void Awake()
    {
        pointCloudPositionXInputField = pointCloudPosition.Find("Values/PositionX/PositionXInputField").GetComponent<TMP_InputField>();
        pointCloudPositionYInputField = pointCloudPosition.Find("Values/PositionY/PositionYInputField").GetComponent<TMP_InputField>();
        pointCloudPositionZInputField = pointCloudPosition.Find("Values/PositionZ/PositionZInputField").GetComponent<TMP_InputField>();

        pointCloudRotationXInputField = pointCloudRotation.Find("Values/RotationX/RotationXInputField").GetComponent<TMP_InputField>();
        pointCloudRotationYInputField = pointCloudRotation.Find("Values/RotationY/RotationYInputField").GetComponent<TMP_InputField>();
        pointCloudRotationZInputField = pointCloudRotation.Find("Values/RotationZ/RotationZInputField").GetComponent<TMP_InputField>();
    }

    private void OnEnable()
    {
        closeButton.onClick.AddListener(OnButtonCloseClick);

        configNameInputField.onSubmit.AddListener(OnConfigNameInputFiledSubmit);
        configNameInputField.onDeselect.AddListener(OnConfigNameInputFiledSubmit);
        createConfigButton.onClick.AddListener(OnCreateConfigButtonClick);

        loadConfigDropdown.onValueChanged.AddListener(OnLoadConfigDropdownValueChanged);
        loadConfigButton.onClick.AddListener(OnLoadConfigButtonClick);

        pointCloudPositionXInputField.onValueChanged.AddListener(OnPointCloudPositionChanged);
        pointCloudPositionYInputField.onValueChanged.AddListener(OnPointCloudPositionChanged);
        pointCloudPositionZInputField.onValueChanged.AddListener(OnPointCloudPositionChanged);

        pointCloudRotationXInputField.onValueChanged.AddListener(OnPointCloudRotationChanged);
        pointCloudRotationYInputField.onValueChanged.AddListener(OnPointCloudRotationChanged);
        pointCloudRotationZInputField.onValueChanged.AddListener(OnPointCloudRotationChanged);

        ConfigFileManager.Instance.OnFileListRefreshed += OnFileListRefreshed;
        ConfigFileManager.Instance.OnConfigLoaded += OnConfigLoaded;
        ConfigFileManager.Instance.OnConfigSaved += OnConfigSaved;

        SceneLoaderManager.Instance.OnSceneLoaded += OnSceneLoaded;

        SetStatus(string.Empty);
    }


    private void OnDisable()
    {
        closeButton.onClick.RemoveListener(OnButtonCloseClick);

        configNameInputField.onSubmit.RemoveListener(OnConfigNameInputFiledSubmit);
        configNameInputField.onDeselect.RemoveListener(OnConfigNameInputFiledSubmit);
        createConfigButton.onClick.RemoveListener(OnCreateConfigButtonClick);

        loadConfigDropdown.onValueChanged.RemoveListener(OnLoadConfigDropdownValueChanged);
        loadConfigButton.onClick.RemoveListener(OnLoadConfigButtonClick);

        pointCloudPositionXInputField.onValueChanged.RemoveListener(OnPointCloudPositionChanged);
        pointCloudPositionYInputField.onValueChanged.RemoveListener(OnPointCloudPositionChanged);
        pointCloudPositionZInputField.onValueChanged.RemoveListener(OnPointCloudPositionChanged);

        pointCloudRotationXInputField.onValueChanged.RemoveListener(OnPointCloudRotationChanged);
        pointCloudRotationYInputField.onValueChanged.RemoveListener(OnPointCloudRotationChanged);
        pointCloudRotationZInputField.onValueChanged.RemoveListener(OnPointCloudRotationChanged);

        ConfigFileManager.Instance.OnFileListRefreshed -= OnFileListRefreshed;
        ConfigFileManager.Instance.OnConfigLoaded -= OnConfigLoaded;
        ConfigFileManager.Instance.OnConfigSaved -= OnConfigSaved;

        SceneLoaderManager.Instance.OnSceneLoaded -= OnSceneLoaded;
    }

    private void OnButtonCloseClick()
    {
        OnCanvasSetupPointCloudUIDestroy.Invoke(this);
    }

    private void OnPointCloudPositionChanged(string value)
    {

        var transform = PointCloudManager.Instance.GetVisualEffectTransform(1);

        transform.position = new Vector3(
            float.Parse(pointCloudPositionXInputField.text),
            float.Parse(pointCloudPositionYInputField.text),
            float.Parse(pointCloudPositionZInputField.text)
        );

        ConfigFileManager.Instance.SaveObjectTransform(1, transform);

        PointCloudManager.Instance.SetVisualEffectTransform(transform, 1);
    }

    private void OnPointCloudRotationChanged(string value)
    {

        var transform = PointCloudManager.Instance.GetVisualEffectTransform(1);

        transform.rotation = Quaternion.Euler(
            float.Parse(pointCloudRotationXInputField.text),
            float.Parse(pointCloudRotationYInputField.text),
            float.Parse(pointCloudRotationZInputField.text)
        );

        ConfigFileManager.Instance.SaveObjectTransform(1, transform);

        PointCloudManager.Instance.SetVisualEffectTransform(transform, 1);

    }

    private void OnLoadConfigButtonClick()
    {
        ConfigFileManager.Instance.Load(selectedConfig);
        SetStatus($"Load Config {selectedConfig}");
    }

    private void OnLoadConfigDropdownValueChanged(int value)
    {
        selectedConfig = configs[value];
    }

    private void OnCreateConfigButtonClick()
    {
        if (string.IsNullOrEmpty(newConfigName))
        {
            SetStatus("Please enter a config name.");
            return;
        }

        ConfigFileManager.Instance.CreateNew(newConfigName);
        ConfigFileManager.Instance.Save();
        selectedConfig = newConfigName;

        InitTransformInputField();

        RefreshList();
        SetStatus($"Created {newConfigName}");
    }

    private void InitTransformInputField()
    {

        var transform =  PointCloudManager.Instance.GetVisualEffectTransform(1);

        pointCloudPositionXInputField.SetTextWithoutNotify(transform.position.x.ToString());
        pointCloudPositionYInputField.SetTextWithoutNotify(transform.position.y.ToString());
        pointCloudPositionZInputField.SetTextWithoutNotify(transform.position.z.ToString());
                                                                                         
        pointCloudRotationXInputField.SetTextWithoutNotify(transform.rotation.x.ToString());
        pointCloudRotationYInputField.SetTextWithoutNotify(transform.rotation.y.ToString());
        pointCloudRotationZInputField.SetTextWithoutNotify(transform.rotation.z.ToString());
    }

    private void OnConfigNameInputFiledSubmit(string value)
    {
        newConfigName = value.Trim();
    }

    private void RefreshList()
    {
      
        configs = ConfigFileManager.Instance.GetAvailableConfigs();

        loadConfigDropdown.ClearOptions();
        loadConfigDropdown.AddOptions(configs);

        loadConfigDropdown.value = configs.IndexOf(selectedConfig);

    }

    private void OnConfigSaved(ConfigFile file)
    {
        
    }

    private void OnConfigLoaded(ConfigFile file)
    {
        var transformData = file.pointClouds[0];

        pointCloudPositionXInputField.text = transformData.position.x.ToString();
        pointCloudPositionYInputField.text = transformData.position.y.ToString();
        pointCloudPositionZInputField.text = transformData.position.z.ToString();

        pointCloudRotationXInputField.text = transformData.rotation.x.ToString();
        pointCloudRotationYInputField.text = transformData.rotation.y.ToString();
        pointCloudRotationZInputField.text = transformData.rotation.z.ToString();
    }

    private void OnFileListRefreshed(List<string> list)
    {

    }


    private void Start()
    {
        StartCoroutine("LoadScene");

        RefreshList();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene)
    {

    }

    public IEnumerator LoadScene()
    {

        Fader.Instance.FadeToBlack();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        yield return SceneLoaderManager.Instance.LoadAsyncScene(scene);

        Fader.Instance.FadeToClear();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

    }

    private void SetStatus(string message)
    {
        Debug.Log($"[CanvasSetupPointCloudUI] {message}");
        if (statusText)
        {
            statusText.text = statusLocalizedText.GetLocalizedString(message);
        }
    }
}
