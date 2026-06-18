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


    [Header("Likert Scale")]
    [SerializeField] private Transform likertScaleContainer;

    private Image likertScaleContainerBackground;

    private TMP_Text questionTextLikertScaleContainer;
    private TMP_Text labelLeftTextLikertScaleContainer;
    private TMP_Text labelRightTextLikertScaleContainer;

    private Button validationButtonLikertScaleContrainer;
    private Slider sliderLikertScaleContainer;

    private CanvasGroup canvasGroupLikertScaleContainer;

    [SerializeField] private int sliderStepValue = 1;

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

    [Header("Image")]
    [SerializeField] private Transform imageContainer;
    private Image imageContainerBackground;
    private CanvasGroup canvasImageContainer;
    private Image imageImageContainer;
    private Transform imageImageTransform;

    public event Action OnSkipHoldValidated;

    [Header("Video")]
    [SerializeField] private Transform videoContainer;
    private Image videoContainerBackground;
    private CanvasGroup canvasGroupVideoContainer;
    private RawImage videoRawImage;
    private UnityEngine.Video.VideoPlayer videoPlayer;
    private RenderTexture videoRenderTexture;
    private Action onVideoFinishedCallback;

    private Action<float> onReadyCallback;

    [Header("Question")]
    [SerializeField] private Transform questionContainer;
    [SerializeField] private Button responceButtonPrefab;
    private Image questionContainerBackground;
    private CanvasGroup canvasGroupQuestionContainer;
    private TMP_Text questionTextQuestionContainer;
    private Transform reponseButtonsTransform; 
    private List<Button> responseButtonsList = new List<Button>();

    public event Action<QuestionAnswer> OnQuestionValidated;

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
        labelLeftTextLikertScaleContainer = sliderLikertScaleContainer.transform.Find("Label Left").GetComponent<TMP_Text>();
        labelRightTextLikertScaleContainer = sliderLikertScaleContainer.transform.Find("Label Right").GetComponent<TMP_Text>();
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
    }

    private void Start()
    {
        currentBackground = null;

        canvasGroupTextContainer.alpha = 0;
        tmpTextContainer.text = string.Empty;

        canvasGroupLikertScaleContainer.alpha = 0;
        questionTextLikertScaleContainer.text = string.Empty;
        labelLeftTextLikertScaleContainer.text = string.Empty;
        labelRightTextLikertScaleContainer.text = string.Empty;

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

        ConfigFileManager.Instance.OnConfigLoaded += OnConfigLoaded;

    }

    private void OnDisable()
    {

        sliderLikertScaleContainer.onValueChanged.RemoveListener(OnSliderValueChanged);

        validationButtonLikertScaleContrainer.onClick.RemoveListener(OnValidationLikertButtonPressed);

        skipHoldButtonBreakContainer.onClick.RemoveListener(OnSkipHoldButtonPressed);

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
        DisplayValidationButton(true);
    }

    private void OnValidationLikertButtonPressed()
    {
        OnLikertScaleValidated?.Invoke((int)sliderLikertScaleContainer.value);
    }

    public void DisplayValidationButton(bool isDisplay)
    {
        validationButtonLikertScaleContrainer.gameObject.SetActive(isDisplay);
    }

    private void OnSkipHoldButtonPressed()
    {
        OnSkipHoldValidated?.Invoke();
    }

 
    private void OnButtonQuestionPressed()
    {
        OnQuestionValidated?.Invoke(QuestionAnswer.Right);
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
        FadeCanvasGroup(canvasGroupTextContainer, 1);
    }

    public void HideText()
    {
        canvasGroupTextContainer.interactable = false;
        canvasGroupTextContainer.blocksRaycasts = false;

        currentBackground = null;

        FadeCanvasGroup(canvasGroupTextContainer, 0, () =>
        {
            tmpTextContainer.text = string.Empty;
   
        });
    }

    public void DisplayLikertScale(string question, string labelLeft, string labelRight)
    {
        questionTextLikertScaleContainer.text = question;
        labelLeftTextLikertScaleContainer.text = labelLeft;
        labelRightTextLikertScaleContainer.text = labelRight;

        likertScaleContainerBackground.color = backgroundColor;
        currentBackground = likertScaleContainerBackground;

        sliderLikertScaleContainer.SetValueWithoutNotify(UnityEngine.Random.Range(sliderLikertScaleContainer.minValue + 10, sliderLikertScaleContainer.maxValue - 10));

        DisplayValidationButton(false);

        canvasGroupLikertScaleContainer.interactable = true;
        canvasGroupLikertScaleContainer.blocksRaycasts = true;

        FadeCanvasGroup(canvasGroupLikertScaleContainer, 1, () =>
        {
            OnDisplayGazeCursor?.Invoke(true);
        });
    }

    public void HideLikertScale()
    {

        canvasGroupLikertScaleContainer.interactable = false;
        canvasGroupLikertScaleContainer.blocksRaycasts = false;

        currentBackground = null;

        OnDisplayGazeCursor?.Invoke(false);

        FadeCanvasGroup(canvasGroupLikertScaleContainer, 0);
    }

    public void DisplayBreak(string instruction)
    {
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

        canvasGroupBreakContainer.interactable = false;
        canvasGroupBreakContainer.blocksRaycasts = false;

        currentBackground = null;

        OnDisplayGazeCursor?.Invoke(false);

        FadeCanvasGroup(canvasGroupBreakContainer, 0);
    }

    public void DisplayImage(Sprite image, float scale)
    {
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
        canvasImageContainer.interactable = false;
        canvasImageContainer.blocksRaycasts = false;

        currentBackground = null;

        FadeCanvasGroup(canvasImageContainer, 0, () =>
        {
            imageImageContainer.sprite = null;

        });

    }

    public void DisplayQuestion(string question, List<string> responseOptions)
    {
        canvasGroupQuestionContainer.alpha = 0;

        questionContainerBackground.color = backgroundColor;
        currentBackground = questionContainerBackground;

        questionTextQuestionContainer.text = question;

        ClearChildren(reponseButtonsTransform);

        for (int i = 0; i < responseOptions.Count; i++)
        {
            var button = GameObject.Instantiate(responceButtonPrefab, reponseButtonsTransform);
            button.GetComponentInChildren<TMP_Text>().text = responseOptions[i];
            button.onClick.AddListener(OnButtonQuestionPressed);
        }

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

        canvasGroupQuestionContainer.interactable = false;
        canvasGroupQuestionContainer.blocksRaycasts = false;

        currentBackground = null;

        OnDisplayGazeCursor?.Invoke(false);

        FadeCanvasGroup(canvasGroupQuestionContainer, 0, () =>
        {
            questionTextQuestionContainer.text = string.Empty;
            
            for (int i = 0; i < responseButtonsList.Count; i++)
            {
                responseButtonsList[i].GetComponentInChildren<TMP_Text>().text = string.Empty;
            }
        });
    }

    public void DisplayVideo(string filePath, bool loop, bool mute, Action onFinished = null, Action<float> onReady = null)
    {
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
        canvasGroupVideoContainer.interactable = false;
        canvasGroupVideoContainer.blocksRaycasts = false;

        currentBackground = null;

        FadeCanvasGroup(canvasGroupVideoContainer, 0, () =>
        {
            videoPlayer.Stop();
            videoRawImage.texture = null;

            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                videoRenderTexture = null;
            }
        });
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
