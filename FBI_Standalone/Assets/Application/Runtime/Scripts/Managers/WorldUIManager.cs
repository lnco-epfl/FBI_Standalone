using PrimeTween;
using System;

using System.Collections.Generic;
using TMPro;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Video;

public class WorldUIManager : MonoBehaviour
{

    [Header("Global")]
    public float FadeDuration => fadeDuration;
    [SerializeField] private float fadeDuration = 0.1f;

    [Header("Text")]
    [SerializeField] private Transform textContainer;
    private Image textContainerBackground;
    private TMP_Text tmpTextContainer;
    private CanvasGroup canvasGroupTextContainer;

    private bool isTextDisplay = false;


    [Header("Likert Scale")]
    [SerializeField] private Transform likertScaleContainer;

    private Image likertScaleContainerBackground;

    private TMP_Text questionTextLikertScaleContainer;
    private TMP_Text labelBottomLeftTextLikertScaleContainer;
    private TMP_Text labelBottomRightTextLikertScaleContainer;
    private TMP_Text labelTopLeftTextLikertScaleContainer;
    private TMP_Text labelTopRightTextLikertScaleContainer;

    private Button validationButtonLikertScaleContrainer;
    private Slider sliderLikertScaleContainer;

    private CanvasGroup canvasGroupLikertScaleContainer;

    [SerializeField] private int sliderStepValue = 1;

    private bool isLikertScaleDisplay = false;

    public event Action<int> OnLikertScaleValidated;

    [Header("Break")]
    [SerializeField] private Transform breakContainer;
    private Image breakContainerBackground;
    private CanvasGroup canvasGroupBreakContainer;
    private TMP_Text instructionTextBreakContainer;
    private TMP_Text counterTextBreakContainer;
    private Button skipHoldButtonBreakContainer;
    private Image holdmaskButtonBreakContainer;
    private Tween holdTween;

    private bool isBreakDisplay = false;

    [Header("Image")]
    [SerializeField] private Transform imageContainer;
    private Image imageContainerBackground;
    private CanvasGroup canvasImageContainer;
    private Image imageImageContainer;
    private Transform imageImageTransform;

    public event Action OnSkipHoldValidated;

    private bool isImageDisplay = false;

    [Header("Video")]
    [SerializeField] private Transform videoContainer;
    private Image videoContainerBackground;
    private CanvasGroup canvasGroupVideoContainer;
    private RawImage videoRawImage;
    private UnityEngine.Video.VideoPlayer videoPlayer;
    private RenderTexture videoRenderTexture;
    private Action onVideoFinishedCallback;

    private Action<float> onReadyCallback;

    private bool isVideoDisplay = false;

    [Header("Question")]
    [SerializeField] private Transform questionContainer;
    [SerializeField] private Button responceButtonPrefab;
    private Image questionContainerBackground;
    private CanvasGroup canvasGroupQuestionContainer;
    private TMP_Text questionTextQuestionContainer;
    private Transform reponseButtonsTransform; 
    private List<Button> responseButtonsList = new List<Button>();
    private Button validationButtonQuestionContainer;

    private bool isQuestionDisplay = false;

    private bool allowMultipleResponses;

    public event Action<List<QuestionAnswer>> OnQuestionValidated;

    public event Action<bool> OnDisplayGazeCursor;

    private static WorldUIManager instance;

    public Vector3 Position
    {
        get => transform.position;
        set => transform.position = value;
    }

    public Vector3 Rotation
    {
        get => transform.eulerAngles;
        set => transform.eulerAngles = value;
    }

    private Color backgroundColor = Color.black;
    private Image currentBackground;



    public Color BackgroundColor { get => backgroundColor; set => backgroundColor = value; }

    public static WorldUIManager Instance { get { return instance; } }



    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); }
        else
        {
            instance = this;
        }


        tmpTextContainer = textContainer.GetComponentInChildren<TMP_Text>();
        canvasGroupTextContainer = textContainer.GetComponent<CanvasGroup>();
        textContainerBackground = textContainer.GetComponentInChildren<Image>();

        canvasGroupLikertScaleContainer = likertScaleContainer.GetComponent<CanvasGroup>();
        questionTextLikertScaleContainer = likertScaleContainer.GetComponentInChildren<TMP_Text>();
        sliderLikertScaleContainer = likertScaleContainer.GetComponentInChildren<Slider>();
        labelTopLeftTextLikertScaleContainer = sliderLikertScaleContainer.transform.Find("Label Top Left").GetComponent<TMP_Text>();
        labelTopRightTextLikertScaleContainer = sliderLikertScaleContainer.transform.Find("Label Top Right").GetComponent<TMP_Text>();
        labelBottomLeftTextLikertScaleContainer = sliderLikertScaleContainer.transform.Find("Label Bottom Left").GetComponent<TMP_Text>();
        labelBottomRightTextLikertScaleContainer = sliderLikertScaleContainer.transform.Find("Label Bottom Right").GetComponent<TMP_Text>();
        validationButtonLikertScaleContrainer = likertScaleContainer.GetComponentInChildren<Button>();
        likertScaleContainerBackground = likertScaleContainer.transform.Find("Background").GetComponent<Image>();

        canvasGroupBreakContainer = breakContainer.GetComponent<CanvasGroup>();
        instructionTextBreakContainer = breakContainer.Find("Instruction").GetComponent<TMP_Text>();
        counterTextBreakContainer = breakContainer.Find("Counter").GetComponent<TMP_Text>();
        skipHoldButtonBreakContainer = breakContainer.GetComponentInChildren<Button>();
        holdmaskButtonBreakContainer = skipHoldButtonBreakContainer.transform.Find("Hold Mask").GetComponent<Image>();
        breakContainerBackground = breakContainer.transform.Find("Background").GetComponent<Image>();

        canvasGroupVideoContainer = videoContainer.GetComponent<CanvasGroup>();
        videoContainerBackground = videoContainer.Find("Background").GetComponent<Image>();
        videoRawImage = videoContainer.Find("Video Player").GetComponent<RawImage>();
        videoPlayer = videoContainer.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();

        videoPlayer.loopPointReached += OnVideoLoopPointReached;

        canvasImageContainer = imageContainer.GetComponent<CanvasGroup>();
        imageImageContainer = imageContainer.Find("Image").GetComponent<Image>();
        imageImageTransform = imageImageContainer.GetComponent<Transform>();
        imageContainerBackground = imageContainer.transform.Find("Background").GetComponent<Image>();

        canvasGroupQuestionContainer = questionContainer.GetComponent<CanvasGroup>();
        questionTextQuestionContainer = questionContainer.Find("Question").GetComponent<TMP_Text>();
        reponseButtonsTransform = questionContainer.Find("ResponseButtons");
        questionContainerBackground = questionContainer.transform.Find("Background").GetComponent<Image>();
        validationButtonQuestionContainer = questionContainer.Find("ValidationButton").GetComponent<Button>();
    }

    private void Start()
    {
        currentBackground = null;

        canvasGroupTextContainer.alpha = 0;
        tmpTextContainer.text = string.Empty;

        canvasGroupLikertScaleContainer.alpha = 0;
        questionTextLikertScaleContainer.text = string.Empty;
        labelTopLeftTextLikertScaleContainer.text = string.Empty;
        labelTopRightTextLikertScaleContainer.text = string.Empty;
        labelBottomLeftTextLikertScaleContainer.text = string.Empty;
        labelBottomRightTextLikertScaleContainer.text = string.Empty;


        canvasGroupVideoContainer.alpha = 0;
        videoRawImage.texture = null;

        canvasGroupBreakContainer.alpha = 0;
        instructionTextBreakContainer.text = string.Empty;
        counterTextBreakContainer.text = string.Empty;
        holdmaskButtonBreakContainer.fillAmount = 0;

        canvasImageContainer.alpha = 0;
        imageImageContainer.sprite = null;

        canvasGroupQuestionContainer.alpha = 0;
        questionTextQuestionContainer.text = string.Empty;

    }

    private void OnEnable()
    {

        sliderLikertScaleContainer.onValueChanged.AddListener(OnSliderValueChanged);

        validationButtonLikertScaleContrainer.onClick.AddListener(OnValidationLikertButtonPressed);

        skipHoldButtonBreakContainer.onClick.AddListener(OnSkipHoldButtonPressed);

        validationButtonQuestionContainer.onClick.AddListener(OnValidationQuestionButtonPressed);

        ConfigFileManager.Instance.OnConfigLoaded += OnConfigLoaded;

    }

    private void OnDisable()
    {

        sliderLikertScaleContainer.onValueChanged.RemoveListener(OnSliderValueChanged);

        validationButtonLikertScaleContrainer.onClick.RemoveListener(OnValidationLikertButtonPressed);

        skipHoldButtonBreakContainer.onClick.RemoveListener(OnSkipHoldButtonPressed);

        validationButtonQuestionContainer.onClick.RemoveListener(OnValidationQuestionButtonPressed);

        ConfigFileManager.Instance.OnConfigLoaded -= OnConfigLoaded;
    }

    private void OnConfigLoaded(ConfigFile configFile)
    {
        if(configFile.stimulusDisplay != null)
        {
            backgroundColor = configFile.stimulusDisplay.backgroundColor.ToColor();

            this.transform.position = configFile.stimulusDisplay.position.ToVector3();

            var rotation = configFile.stimulusDisplay.rotation.ToVector3();
            this.transform.rotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z);  

        }


    }

    private void OnSliderValueChanged(float value)
    {
        DisplayValidationButtonLikertScaleContrainer(true);
    }

    private void OnValidationLikertButtonPressed()
    {
        if(validationButtonLikertScaleContrainer.gameObject.activeSelf)
        {
            OnLikertScaleValidated?.Invoke((int)sliderLikertScaleContainer.value);
        }
    }

    private void OnValidationQuestionButtonPressed()
    {
        if (validationButtonQuestionContainer.gameObject.activeSelf)
        {
            List<QuestionAnswer> answers = new List<QuestionAnswer>();
            for (int i = 0; responseButtonsList.Count > i; i++)
            {
                var button = responseButtonsList[i];

                if (button.targetGraphic.color == Color.green)
                {
                    answers.Add((QuestionAnswer)i + 1);
                }
            }

            OnQuestionValidated?.Invoke(answers);
        }
    }

    public void DisplayValidationButtonLikertScaleContrainer(bool isDisplay)
    {
        validationButtonLikertScaleContrainer.gameObject.SetActive(isDisplay);
    }

    public void DisplayValidationButtonQuestionContainer(bool isDisplay)
    {
        validationButtonQuestionContainer.gameObject.SetActive(isDisplay);
    }


    private void OnSkipHoldButtonPressed()
    {
        OnSkipHoldValidated?.Invoke();
    }

 
    private void OnButtonQuestionPressed(Button button)
    {
        if(!validationButtonQuestionContainer.gameObject.activeSelf)
        {
            DisplayValidationButtonQuestionContainer(true);
        }


        if (!allowMultipleResponses)
        {
            for (int i = 0; i < responseButtonsList.Count; i++)
            {
                responseButtonsList[i].targetGraphic.color = button.colors.normalColor;
            }

            button.targetGraphic.color = Color.green;
        }
        else
        {
            if (button.targetGraphic.color == button.colors.normalColor)
            {
                button.targetGraphic.color = Color.green;
            }
            else
            {
                button.targetGraphic.color = button.colors.normalColor;
            }
            
        }

       

    }

    private void FadeCanvasGroup(CanvasGroup canvasGroup, float endValue, Action value = null)
    {
        Tween.Alpha(canvasGroup, endValue: endValue, duration: fadeDuration).OnComplete(value);
    }

    public void DisplayText(string text)
    {
        canvasGroupTextContainer.alpha = 0;
        canvasGroupTextContainer.interactable = true;
        canvasGroupTextContainer.blocksRaycasts = true;

        textContainerBackground.color = backgroundColor;
        currentBackground = textContainerBackground;

        tmpTextContainer.text = text;

        isTextDisplay = true;
        FadeCanvasGroup(canvasGroupTextContainer, 1);
    }

    public void HideText()
    {
        if(isTextDisplay)
        {

            isTextDisplay = false;

            canvasGroupTextContainer.interactable = false;
            canvasGroupTextContainer.blocksRaycasts = false;

            currentBackground = null;

            tmpTextContainer.text = string.Empty;

            FadeCanvasGroup(canvasGroupTextContainer, 0);
        }
       
    }

    public void DisplayLikertScale(string question, string labelLeft, string labelRight, int min, int max, bool randomCursorPosition)
    {

        isLikertScaleDisplay = true;
        questionTextLikertScaleContainer.text = question;
        labelBottomLeftTextLikertScaleContainer.text = labelLeft;
        labelBottomRightTextLikertScaleContainer.text = labelRight;

        labelTopLeftTextLikertScaleContainer.text = min.ToString();
        labelTopRightTextLikertScaleContainer.text = max.ToString();

        likertScaleContainerBackground.color = backgroundColor;
        currentBackground = likertScaleContainerBackground;

        sliderLikertScaleContainer.minValue = min;
        sliderLikertScaleContainer.maxValue = max;

        float totalValue = (max + 1) - min;

        float borderValue = Mathf.Ceil(totalValue * 0.1f);
        float centerValue = Mathf.Ceil(totalValue * 0.5f);

        if (randomCursorPosition)
        {
            sliderLikertScaleContainer.SetValueWithoutNotify(UnityEngine.Random.Range(sliderLikertScaleContainer.minValue + borderValue, sliderLikertScaleContainer.maxValue - borderValue));
        }
        else
        {
            sliderLikertScaleContainer.SetValueWithoutNotify(centerValue);
        }

        DisplayValidationButtonLikertScaleContrainer(false);

        canvasGroupLikertScaleContainer.interactable = true;
        canvasGroupLikertScaleContainer.blocksRaycasts = true;

        FadeCanvasGroup(canvasGroupLikertScaleContainer, 1, () =>
        {
            OnDisplayGazeCursor?.Invoke(true);
        });
    }

    public void HideLikertScale()
    {
        if(isLikertScaleDisplay)
        {

            isLikertScaleDisplay = false;

            canvasGroupLikertScaleContainer.interactable = false;
            canvasGroupLikertScaleContainer.blocksRaycasts = false;

            currentBackground = null;

            OnDisplayGazeCursor?.Invoke(false);

            FadeCanvasGroup(canvasGroupLikertScaleContainer, 0);
        }

    }

    public void DisplayBreak(string instruction)
    {

        isBreakDisplay = true;
        canvasGroupBreakContainer.alpha = 0;

        breakContainerBackground.color = backgroundColor;
        currentBackground = breakContainerBackground;

        instructionTextBreakContainer.text = instruction;

        canvasGroupBreakContainer.interactable = true;
        canvasGroupBreakContainer.blocksRaycasts = true;

        FadeCanvasGroup(canvasGroupBreakContainer, 1, () =>
        {
            OnDisplayGazeCursor?.Invoke(true);
        });
    }

    public void UpdateCounter(string counter)
    {
        counterTextBreakContainer.text = counter;
    }

    public void HideBreak()
    {

        if(isBreakDisplay)
        {
            isBreakDisplay = false;

            canvasGroupBreakContainer.interactable = false;
            canvasGroupBreakContainer.blocksRaycasts = false;

            currentBackground = null;

            OnDisplayGazeCursor?.Invoke(false);

            FadeCanvasGroup(canvasGroupBreakContainer, 0);
        }

    }

    public void DisplayImage(Sprite image, float scale)
    {
        isImageDisplay = true;
        canvasImageContainer.alpha = 0;

        imageContainerBackground.color = backgroundColor;
        currentBackground = imageContainerBackground;

        imageImageContainer.sprite = image;

        imageImageTransform.localScale = Vector3.one * scale;

        canvasImageContainer.interactable = true;
        canvasImageContainer.blocksRaycasts = true;

        FadeCanvasGroup(canvasImageContainer, 1);

    }

    public void HideImage()
    {
        if(isImageDisplay)
        {
            canvasImageContainer.interactable = false;
            canvasImageContainer.blocksRaycasts = false;

            currentBackground = null;

            isImageDisplay = false;

            FadeCanvasGroup(canvasImageContainer, 0, () =>
            {
                imageImageContainer.sprite = null;

            });
        }


    }

    public void DisplayQuestion(string question, List<string> responseOptions, bool allowMultipleResponses)
    {

        isQuestionDisplay = true;

        canvasGroupQuestionContainer.alpha = 0;

        questionContainerBackground.color = backgroundColor;
        currentBackground = questionContainerBackground;

        questionTextQuestionContainer.text = question;

        ClearChildren(reponseButtonsTransform);

        this.allowMultipleResponses = allowMultipleResponses;

        responseButtonsList.Clear();

        for (int i = 0; i < responseOptions.Count; i++)
        {
            var button = GameObject.Instantiate(responceButtonPrefab, reponseButtonsTransform);
            button.GetComponentInChildren<TMP_Text>().text = responseOptions[i];
            button.onClick.AddListener( () => OnButtonQuestionPressed(button));

            responseButtonsList.Add(button);
        }

        DisplayValidationButtonQuestionContainer(false);

        canvasGroupQuestionContainer.interactable = true;
        canvasGroupQuestionContainer.blocksRaycasts = true;

        FadeCanvasGroup(canvasGroupQuestionContainer, 1,() =>
        {
            OnDisplayGazeCursor?.Invoke(true);
        });
    }

    public void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    public void HideQuestion()
    {

        if (isQuestionDisplay)
        {

            isQuestionDisplay = false;
            allowMultipleResponses = false;

            canvasGroupQuestionContainer.interactable = false;
            canvasGroupQuestionContainer.blocksRaycasts = false;

            currentBackground = null;

            OnDisplayGazeCursor?.Invoke(false);

            questionTextQuestionContainer.text = string.Empty;

            for (int i = 0; i < responseButtonsList.Count; i++)
            {
                responseButtonsList[i].GetComponentInChildren<TMP_Text>().text = string.Empty;
            }

            FadeCanvasGroup(canvasGroupQuestionContainer, 0);
        }

    }

    public void DisplayVideo(string filePath, bool loop, bool mute, Action onFinished = null, Action<float> onReady = null)
    {

        isVideoDisplay = true;
        canvasGroupVideoContainer.alpha = 0;

        videoContainerBackground.color = backgroundColor;
        currentBackground = videoContainerBackground;

        onVideoFinishedCallback = onFinished;

        if (videoRenderTexture != null)
            videoRenderTexture.Release();

        videoRenderTexture = new RenderTexture(1920, 1080, 0);
        videoRawImage.texture = videoRenderTexture;

        videoPlayer.targetTexture = videoRenderTexture;
        videoPlayer.url = $"file://{filePath}";
        videoPlayer.isLooping = loop;
        videoPlayer.SetDirectAudioMute(0, mute);
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
        onReadyCallback = onReady;

    }

    private void OnVideoPrepared(UnityEngine.Video.VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        vp.Play();

        canvasGroupVideoContainer.interactable = true;
        canvasGroupVideoContainer.blocksRaycasts = true;

        FadeCanvasGroup(canvasGroupVideoContainer, 1);

        float duration = (float)vp.frameCount / vp.frameRate;
        onReadyCallback?.Invoke(duration);
        onReadyCallback = null;
    }

    private void OnVideoLoopPointReached(UnityEngine.Video.VideoPlayer vp)
    {
        onVideoFinishedCallback?.Invoke();
        onVideoFinishedCallback = null;
    }

    public void HideVideo()
    {

        if(isVideoDisplay)
        {
            isVideoDisplay = false;
            canvasGroupVideoContainer.interactable = false;
            canvasGroupVideoContainer.blocksRaycasts = false;

            currentBackground = null;

            videoPlayer.Stop();
            videoRawImage.texture = null;

            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                videoRenderTexture = null;

            }
            FadeCanvasGroup(canvasGroupVideoContainer, 0);
        }

    }

    public void SetCurrentBackgoundColor(Color color)
    {
        backgroundColor = color;

        if(currentBackground != null)
        {
            currentBackground.color = backgroundColor;
        }
    }

    public VideoPlayer GetVideoPlayer()
    {
        return videoPlayer;
    }
}
