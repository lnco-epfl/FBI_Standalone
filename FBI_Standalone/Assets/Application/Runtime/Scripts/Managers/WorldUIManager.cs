using PrimeTween;
using System;

using System.Collections.Generic;
using TMPro;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

using UnityEngine.UI;

using Random = UnityEngine.Random;

public class WorldUIManager : MonoBehaviour
{

    [Header("Global")]
    public float FadeDuration => fadeDuration;
    [SerializeField] private float fadeDuration = 0.1f;

    [Header("Text")]
    [SerializeField] private Transform textContainer;
    private TMP_Text tmpTextContainer;
    private CanvasGroup canvasGroupTextContainer;


    [Header("Likert Scale")]
    [SerializeField] private Transform likertScaleContainer;

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
    private CanvasGroup canvasGroupBreakContainer;
    private TMP_Text instructionTextBreakContainer;
    private TMP_Text counterTextBreakContainer;
    private Button skipHoldButtonBreakContainer;
    private Image holdmaskButtonBreakContainer;
    private Tween holdTween;

    [Header("Image")]
    [SerializeField] private Transform imageContainer;
    private CanvasGroup canvasImageContainer;
    private Image imageImageContainer;
    private Transform imageImageTransform;

    public event Action OnSkipHoldValidated;

    [Header("Question")]
    [SerializeField] private Transform questionContainer;
    [SerializeField] private Button responceButtonPrefab;
    private CanvasGroup canvasGroupQuestionContainer;
    private TMP_Text questionTextQuestionContainer;
    private Transform reponseButtonsTransform; 
    private List<Button> responseButtonsList = new List<Button>();

    public event Action<QuestionAnswer> OnQuestionValidated;

    private static WorldUIManager instance;


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

        canvasGroupLikertScaleContainer = likertScaleContainer.GetComponent<CanvasGroup>();
        questionTextLikertScaleContainer = likertScaleContainer.GetComponentInChildren<TMP_Text>();
        sliderLikertScaleContainer = likertScaleContainer.GetComponentInChildren<Slider>();
        labelLeftTextLikertScaleContainer = sliderLikertScaleContainer.transform.Find("Label Left").GetComponent<TMP_Text>();
        labelRightTextLikertScaleContainer = sliderLikertScaleContainer.transform.Find("Label Right").GetComponent<TMP_Text>();
        validationButtonLikertScaleContrainer = likertScaleContainer.GetComponentInChildren<Button>();


        canvasGroupBreakContainer = breakContainer.GetComponent<CanvasGroup>();
        instructionTextBreakContainer = breakContainer.Find("Instruction").GetComponent<TMP_Text>();
        counterTextBreakContainer = breakContainer.Find("Counter").GetComponent<TMP_Text>();
        skipHoldButtonBreakContainer = breakContainer.GetComponentInChildren<Button>();
        holdmaskButtonBreakContainer = skipHoldButtonBreakContainer.transform.Find("Hold Mask").GetComponent<Image>();

        canvasImageContainer = imageContainer.GetComponent<CanvasGroup>();
        imageImageContainer = imageContainer.Find("Image").GetComponent<Image>();
        imageImageTransform = imageImageContainer.GetComponent<Transform>();

        canvasGroupQuestionContainer = questionContainer.GetComponent<CanvasGroup>();
        questionTextQuestionContainer = questionContainer.Find("Question").GetComponent<TMP_Text>();
        reponseButtonsTransform = questionContainer.Find("ResponseButtons");

    }

    private void Start()
    {
        canvasGroupTextContainer.alpha = 0;
        tmpTextContainer.text = string.Empty;


        canvasGroupLikertScaleContainer.alpha = 0;
        questionTextLikertScaleContainer.text = string.Empty;
        labelLeftTextLikertScaleContainer.text = string.Empty;
        labelRightTextLikertScaleContainer.text = string.Empty;

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

    }



    private void OnDisable()
    {

        sliderLikertScaleContainer.onValueChanged.RemoveListener(OnSliderValueChanged);

        validationButtonLikertScaleContrainer.onClick.RemoveListener(OnValidationLikertButtonPressed);

        skipHoldButtonBreakContainer.onClick.RemoveListener(OnSkipHoldButtonPressed);

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

        tmpTextContainer.text = text;
        FadeCanvasGroup(canvasGroupTextContainer, 1);
    }

    public void HideText()
    {
        canvasGroupTextContainer.interactable = false;
        canvasGroupTextContainer.blocksRaycasts = false;

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

        sliderLikertScaleContainer.SetValueWithoutNotify(UnityEngine.Random.Range(sliderLikertScaleContainer.minValue + 10, sliderLikertScaleContainer.maxValue - 10));

        DisplayValidationButton(false);

        canvasGroupLikertScaleContainer.interactable = true;
        canvasGroupLikertScaleContainer.blocksRaycasts = true;

        FadeCanvasGroup(canvasGroupLikertScaleContainer, 1);
    }

    public void HideLikertScale()
    {

        canvasGroupLikertScaleContainer.interactable = false;
        canvasGroupLikertScaleContainer.blocksRaycasts = false;

        FadeCanvasGroup(canvasGroupLikertScaleContainer, 0);
    }

    public void DisplayBreak(string instruction)
    {
        canvasGroupBreakContainer.alpha = 0;

        instructionTextBreakContainer.text = instruction;

        canvasGroupBreakContainer.interactable = true;
        canvasGroupBreakContainer.blocksRaycasts = true;

        FadeCanvasGroup(canvasGroupBreakContainer, 1);
    }

    public void UpdateCounter(string counter)
    {
        counterTextBreakContainer.text = counter;
    }

    public void HideBreak()
    {

        canvasGroupBreakContainer.interactable = false;
        canvasGroupBreakContainer.blocksRaycasts = false;

        FadeCanvasGroup(canvasGroupBreakContainer, 0);
    }

    public void DisplayImage(Sprite image, float scale)
    {
        canvasImageContainer.alpha = 0;

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

        FadeCanvasGroup(canvasImageContainer, 0, () =>
        {
            imageImageContainer.sprite = null;

        });

    }

    public void DisplayQuestion(string question, List<string> responseOptions)
    {
        canvasGroupQuestionContainer.alpha = 0;

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

        FadeCanvasGroup(canvasGroupQuestionContainer, 1);
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

        FadeCanvasGroup(canvasGroupQuestionContainer, 0, () =>
        {
            questionTextQuestionContainer.text = string.Empty;
            
            for (int i = 0; i < responseButtonsList.Count; i++)
            {
                responseButtonsList[i].GetComponentInChildren<TMP_Text>().text = string.Empty;
            }
        });
    }

}
