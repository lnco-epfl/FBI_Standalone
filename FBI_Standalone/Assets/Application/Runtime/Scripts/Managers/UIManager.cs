using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private CanvasGroup canvasDesktopGroup;
    [SerializeField] private CanvasGroup canvasFullScreenGroup;

    [Header("Canvas Prefabs")]
    [SerializeField] private CanvasSetupPointCloudUI canvasSetupPointCloudUIPrefab;

    [Header("Sound")]
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Button muteButton;
    [SerializeField] private Sprite onSpeakerSprite;
    [SerializeField] private Sprite offSpeakerSprite;

    [Header("Participant Data")]
    [SerializeField] private TMP_InputField ageInputField;
    [SerializeField] private TMP_Dropdown genderDropdown;
    [SerializeField] private TMP_Dropdown languageDropdown;

    [SerializeField] private TMP_Dropdown sequenceDropdown;

    [SerializeField] private TMP_Dropdown configDropdown;
    [SerializeField] private Button startSetupButton;

    [Header("Status")]
    [SerializeField] private TMP_Text startTime;
    [SerializeField] private TMP_Text timeSinceStart;

    [SerializeField] private TMP_Text progression;
    [SerializeField] private TMP_Text steps;

    [SerializeField] private Image progressionFill;

    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_InputField stepInputField;

    [Header("Buttons")]
    [SerializeField] private Button fullscreenInButton;
    [SerializeField] private Button fullscreenOutButton;

    [SerializeField] private Button quitButton;

    [SerializeField] private Button playPauseButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button stopButton;

    [SerializeField] private LocalizedString playText;
    [SerializeField] private LocalizedString pauseText;

    [SerializeField] private Button resetXROriginButton;


    private bool isPause = false;

    private bool isMute = false;

    private float audioVolume;

    private static UIManager instance;
    public static UIManager Instance { get { return instance; } }

    private List<CanvasSetupPointCloudUI> currentCanvasGraphUIs = new List<CanvasSetupPointCloudUI>();
    private List<string> configs;
    private string selectedConfig;
    private bool lastFullScreen = true;

    public string SelectedGender
    {
        get
        {
            return genderDropdown.options[genderDropdown.value].text;
        }
    }
    public int SelectedAge
    {
        get
        {
            if (string.IsNullOrEmpty(ageInputField.text))
            {
                return 0;
            }
            else
            {
                return int.Parse(ageInputField.text);
            }

        }
    }
    public string SelectedLanguage
    {
        get
        {
            return languageDropdown.options[languageDropdown.value].text;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); }
        else
        {
            instance = this;
        }
        lastFullScreen = Screen.fullScreen;
    }


    private void OnEnable()
    {

        sequenceDropdown.onValueChanged.AddListener(OnInputFileDropDownChanged);

        configDropdown.onValueChanged.AddListener(OnConfigDropDownChanged);
        startSetupButton.onClick.AddListener(OnStartSetupButtonPress);

        fullscreenInButton.onClick.AddListener(OnFullscreenInButtonPress);
        fullscreenOutButton.onClick.AddListener(OnFullscreenOutButtonPress);

        quitButton.onClick.AddListener(OnQuitButtonPress);

        soundSlider.onValueChanged.AddListener(OnSoundSliderValueChanged);
        muteButton.onClick.AddListener(OnMuteButtonPress);

        resetXROriginButton.onClick.AddListener(OnResetXROriginButtonPress);

        ShortcutManager.Instance.MuteActionReference.action.performed += MuteActionPerformed;

        ageInputField.onSubmit.AddListener(OnAgeInputFieldSubmit);
        ageInputField.onSelect.AddListener(OnInputFieldSelect);
        ageInputField.onDeselect.AddListener(OnInputFieldDeselect);

        genderDropdown.onValueChanged.AddListener(OnGenderDropDownChanged);
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        previousButton.onClick.AddListener(OnPreviousButtonPress);
        nextButton.onClick.AddListener(OnNextButtonPress);
        stepInputField.onSubmit.AddListener(OnStepInputFieldEdit);
        stepInputField.onSelect.AddListener(OnInputFieldSelect);
        stepInputField.onDeselect.AddListener(OnInputFieldDeselect);

        startButton.onClick.AddListener(OnStartButtonPress);
        stopButton.onClick.AddListener(OnStopButtonPress);
        playPauseButton.onClick.AddListener(OnPlayPauseButtonPress);

        ExperimentManager.Instance.OnInitialized += OnExperimentInitialized;
        ExperimentManager.Instance.OnStarted += OnExperimentStarted;
        ExperimentManager.Instance.OnPause += OnExperimentPause;
        ExperimentManager.Instance.OnStop += OnExperimentStop;

    }

    private void OnDisable()
    {
        sequenceDropdown.onValueChanged.RemoveListener(OnInputFileDropDownChanged);

        configDropdown.onValueChanged.RemoveListener(OnConfigDropDownChanged);
        startSetupButton.onClick.RemoveListener(OnStartSetupButtonPress);

        fullscreenInButton.onClick.RemoveListener(OnFullscreenInButtonPress);
        fullscreenOutButton.onClick.RemoveListener(OnFullscreenOutButtonPress);

        quitButton.onClick.RemoveListener(OnQuitButtonPress);

        resetXROriginButton.onClick.RemoveListener(OnResetXROriginButtonPress);

        ageInputField.onSubmit.RemoveListener(OnAgeInputFieldSubmit);
        ageInputField.onSelect.RemoveListener(OnInputFieldSelect);
        ageInputField.onDeselect.RemoveListener(OnInputFieldDeselect);

        genderDropdown.onValueChanged.RemoveListener(OnGenderDropDownChanged);
        languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);

        soundSlider.onValueChanged.RemoveListener(OnSoundSliderValueChanged);
        muteButton.onClick.RemoveListener(OnMuteButtonPress);

        ShortcutManager.Instance.MuteActionReference.action.performed -= MuteActionPerformed;

        previousButton.onClick.RemoveListener(OnPreviousButtonPress);
        nextButton.onClick.RemoveListener(OnNextButtonPress);
        stepInputField.onSubmit.RemoveListener(OnStepInputFieldEdit);
        stepInputField.onSelect.RemoveListener(OnInputFieldSelect);
        stepInputField.onDeselect.RemoveListener(OnInputFieldDeselect);

        startButton.onClick.RemoveListener(OnStartButtonPress);
        stopButton.onClick.RemoveListener(OnStopButtonPress);
        playPauseButton.onClick.RemoveListener(OnPlayPauseButtonPress);

        ExperimentManager.Instance.OnInitialized -= OnExperimentInitialized;
        ExperimentManager.Instance.OnStarted -= OnExperimentStarted;
        ExperimentManager.Instance.OnPause -= OnExperimentPause;
        ExperimentManager.Instance.OnStop -= OnExperimentStop;
    }


    private void Start()
    {
        SetFullscreenView(false);

        DisableStartButtonInteraction();
        DisableStopButtonInteraction();
        DisablePlayPauseButtonInteraction();

        OnSoundSliderValueChanged(1.0f);

        genderDropdown.SetValueWithoutNotify(0);

        var index = LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.SelectedLocale);
        languageDropdown.SetValueWithoutNotify(index);

        InitSequenceDropDown();

        //InitConfigDropDown();

    }

    private void Update()
    {
        if (ExperimentManager.Instance.IsRunning)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(ExperimentManager.Instance.ElaspedTimeSinceStart);
            timeSinceStart.text = timeSpan.ToString(@"hh\:mm\:ss");

            UdapteProgressionInfos();
        }

        if(Screen.fullScreen)
        {
            quitButton.gameObject.SetActive(true);
        }
        else
        {
            quitButton.gameObject.SetActive(false);
        }
    }

    private void OnInputFieldSelect(string value)
    {
        ShortcutManager.Instance.DisableShortCut();
    }

    private void OnInputFieldDeselect(string value)
    {
        ShortcutManager.Instance.EnableShortCut();
    }

    private void InitSequenceDropDown()
    {
        sequenceDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        options.Add(new TMP_Dropdown.OptionData("None"));

        foreach (var sequence in SequencesManager.Instance.Sequences)
        {
            options.Add(new TMP_Dropdown.OptionData(sequence.name));
        }

        sequenceDropdown.AddOptions(options);
        sequenceDropdown.SetValueWithoutNotify(0);
    }

    private void InitConfigDropDown()
    {
        configs = ConfigFileManager.Instance.GetAvailableConfigs();


        configDropdown.ClearOptions();

        if (configs.Count > 0)
        {

    
            configDropdown.AddOptions(configs);

            configDropdown.value = configs.IndexOf(selectedConfig);

        }
        else
        {
            DisableStartButtonInteraction();
        }
    }

    private void OnExperimentInitialized(bool isInitialized, Sequence sequence)
    {

        if (isInitialized)
        {
            EnableStartButtonInteraction();
            DisableStopButtonInteraction();
            DisablePlayPauseButtonInteraction();

            UdapteProgressionInfos(true);
        }
        else
        {
            DisableStartButtonInteraction();
            DisableStopButtonInteraction();
            DisablePlayPauseButtonInteraction();
        }

    }

    private void OnStartSetupButtonPress()
    {
        CreateSetupPointCloudUI();
    }

    private void OnConfigDropDownChanged(int index)
    {
        selectedConfig = configs[index];
        ConfigFileManager.Instance.Load(selectedConfig);
    }

    private void OnExperimentStarted()
    {
        DisableStartButtonInteraction();
        EnableStopButtonInteraction();
        EnablePlayPauseButtonInteraction();

        startTime.text = ExperimentManager.Instance.StartDate.ToString("dd.MM.yyyy HH:mm:ss");

    }

    private void OnExperimentStop()
    {
        EnableStartButtonInteraction();
        DisableStopButtonInteraction();
        DisablePlayPauseButtonInteraction();
    }

    private void OnExperimentPause(bool isPause)
    {

    }

    private void MuteActionPerformed(InputAction.CallbackContext context)
    {
        OnMuteButtonPress();
    }
    private void OnSoundSliderValueChanged(float value)
    {
        if (value <= 0.01f)
        {
            isMute = true;

        }
        else
        {
            isMute = false;
        }

        audioVolume = value;

        AudioVolumeManager.Instance.SetVolume(audioVolume);

        UpdateMuteButton();
    }

    private void OnMuteButtonPress()
    {
        isMute = !isMute;
        UpdateMuteButton();

    }

    private void UpdateMuteButton()
    {
        var image = muteButton.targetGraphic as Image;
        if (isMute)
        {
            AudioVolumeManager.Instance.Mute();
            image.sprite = offSpeakerSprite;
            soundSlider.SetValueWithoutNotify(0);
        }
        else
        {
            AudioVolumeManager.Instance.Unmute();
            image.sprite = onSpeakerSprite;
            soundSlider.SetValueWithoutNotify(audioVolume);
        }
    }

    private void OnStepInputFieldEdit(string input)
    {
        int output = 0;
        var result = int.TryParse(input, out output);
        if (result)
        {
            ExperimentManager.Instance.GoToStep(output-1);
        }
        else
        {
            Debug.LogError("Invalid input: " + input);
        }
    }

    private void OnGenderDropDownChanged(int value)
    {
        OutputFileManager.Instance.OutputFileData.Gender = genderDropdown.options[value].text;
    }

    private void OnAgeInputFieldSubmit(string value)
    {
        OutputFileManager.Instance.OutputFileData.Age = int.Parse(value);
    }

    private void OnLanguageChanged(int index)
    {
        OutputFileManager.Instance.OutputFileData.Language = languageDropdown.options[index].text;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
    }

    private void OnNextButtonPress()
    {
        ExperimentManager.Instance.NextStep(1);
    }

    private void OnPreviousButtonPress()
    {
        ExperimentManager.Instance.NextStep(-1);
    }

    private void OnInputFileDropDownChanged(int index)
    {
        if (index != 0)
        {
            ExperimentManager.Instance.InitializeExperiment(SequencesManager.Instance.Sequences[index - 1]);
        }
        else
        {
            ExperimentManager.Instance.InitializeExperiment(null);
        }
    }

    private void OnFullscreenOutButtonPress()
    {
        SetFullscreenView(false);
    }

    private void OnFullscreenInButtonPress()
    {
        SetFullscreenView(true);
    }
    private static void OnQuitButtonPress()
    {
        Application.Quit();
    }

    private void OnResetXROriginButtonPress()
    {
        ResetXROrigin.Instance.ResetOrigin();
    }

    private void OnPlayPauseButtonPress()
    {

        isPause = !isPause;

        ExperimentManager.Instance.PauseExperiment(isPause);
        UpdatePlayPauseButtonText();
    }

    private void OnStopButtonPress()
    {
        ExperimentManager.Instance.StopExperiment();
    }

    private void OnStartButtonPress()
    {
        ExperimentManager.Instance.StartExperiment();

        isPause = false;
        UpdatePlayPauseButtonText();

    }

    private void UpdatePlayPauseButtonText()
    {
        var text = playPauseButton.GetComponentInChildren<TMP_Text>();
        text.text = isPause ? playText.GetLocalizedString() : pauseText.GetLocalizedString();
    }

    private void EnableStartButtonInteraction()
    {
        startButton.interactable = true;
    }

    private void DisableStartButtonInteraction()
    {
        startButton.interactable = false;
    }
    private void EnableStopButtonInteraction()
    {
        stopButton.interactable = true;
    }

    private void DisableStopButtonInteraction()
    {
        stopButton.interactable = false;
    }
    private void EnablePlayPauseButtonInteraction()
    {
        playPauseButton.interactable = true;
    }

    private void DisablePlayPauseButtonInteraction()
    {
        playPauseButton.interactable = false;
    }

    public void EnablePauseBehaviourButtons()
    {
        DisableStartButtonInteraction();
        DisableStopButtonInteraction();
        DisablePlayPauseButtonInteraction();
    }

    public void DisablePauseBehaviourButtons()
    {
        DisableStartButtonInteraction();
        EnableStopButtonInteraction();
        EnablePlayPauseButtonInteraction();
    }

    private void UdapteProgressionInfos(bool Initialize = false)
    {
        var currentIndex = Initialize ? 0 : ExperimentManager.Instance.SequenceCurrentStep + 1;
        var maxIndex = ExperimentManager.Instance.SequenceTotalSetps;
        steps.text = string.Format("{0}/{1}", currentIndex, maxIndex);

        if (currentIndex.ToString() != stepInputField.text && !stepInputField.isFocused)
        {
            stepInputField.SetTextWithoutNotify(currentIndex.ToString());
        }

        float ratio = (float)currentIndex / (float)maxIndex;
        progression.text = string.Format("{0:0}%", Math.Floor(ratio * 100.0f));
        progressionFill.fillAmount = ratio;
    }


    private void SetFullscreenView(bool fullscreen)
    {
        if (fullscreen)
        {
            SetCanvasState(canvasDesktopGroup, false);
            SetCanvasState(canvasFullScreenGroup, true);
        }
        else
        {
            SetCanvasState(canvasDesktopGroup, true);
            SetCanvasState(canvasFullScreenGroup, false);
        }

    }

    private void SetCanvasState(CanvasGroup canvasGroup, bool state)
    {
        canvasGroup.alpha = state ? 1.0f : 0.0f;
        canvasGroup.blocksRaycasts = state;
        canvasGroup.interactable = state;
    }

    public void CreateSetupPointCloudUI()
    {

        var canvasUI = Instantiate(canvasSetupPointCloudUIPrefab, canvasDesktopGroup.transform);
        currentCanvasGraphUIs.Add(canvasUI);

        canvasUI.OnCanvasSetupPointCloudUIDestroy += OnCanvasSetupPointCloudUIDestroy;

    }

    private void OnCanvasSetupPointCloudUIDestroy(CanvasSetupPointCloudUI canvasSetupPointCloudUI)
    {
        currentCanvasGraphUIs.Remove(canvasSetupPointCloudUI);
        Destroy(canvasSetupPointCloudUI.gameObject);

        InitConfigDropDown();
    }

}
