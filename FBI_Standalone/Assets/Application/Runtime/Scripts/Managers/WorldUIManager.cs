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

    [SerializeField] private InputActionReference sliderRightInputActionReference;
    [SerializeField] private InputActionReference sliderLeftInputActionReference;

    [SerializeField] private InputActionReference validationButtonLikertInputActionReference;

    [SerializeField] private int sliderStepValue = 1;

    public event Action<int> OnLikertScaleValidated;

    [Header("Break")]
    [SerializeField] private Transform breakContainer;
    private CanvasGroup canvasGroupBreakContainer;
    private TMP_Text instructionTextBreakContainer;
    private TMP_Text counterTextBreakContainer;
    private Button skipHoldButtonBreakContainer;
    private Image holdmaskButtonBreakContainer;
    [SerializeField] private InputActionReference skipHoldInputActionReference;
    private Tween holdTween;

    [Header("Image")]
    [SerializeField] private Transform imageContainer;
    private CanvasGroup canvasImageContainer;
    private Image imageImageContainer;
    private Transform imageImageTransform;

    public event Action OnSkipHoldValidated;

    [Header("Question")]
    [SerializeField] private Transform questionContainer;
    private CanvasGroup canvasGroupQuestionContainer;
    private Button buttonLeftQuestionContainer;
    private TMP_Text buttonLeftTextQuestionContainer;
    private Button buttonRightQuestionContainer;
    private TMP_Text buttonRightTextQuestionContainer;
    private TMP_Text questionTextQuestionContainer;

    [SerializeField] private InputActionReference buttonLeftInputActionReference;
    [SerializeField] private InputActionReference buttonRightInputActionReference;

    public event Action<QuestionAnswer> OnQuestionValidated;

    [Header("QuestionMulti")]
    [SerializeField] private Transform questionMultiContainer;
    private CanvasGroup canvasGroupQuestionMultiContainer;
    private Button buttonLeftQuestionMultiContainer;
    private Button buttonRightQuestionMultiContainer;
    private TMP_Text carrouselTextQuestionMultiContainer;
    private TMP_Text questionTextQuestionMultiContainer;
    private Button buttonValidationQuestionMultiContainer;

    private List<string> carrouselOptionList = new List<string>();
    private int currentCarrouselIndex = 0;

    [SerializeField] private InputActionReference carrouselLeftInputActionReference;
    [SerializeField] private InputActionReference carrouselRightInputActionReference;
    [SerializeField] private InputActionReference validationButtonCarrouselInputActionReference;

    [Header("Other")]
    [SerializeField] private InputActionReference trackpadInputActionReference;
    public event Action<int> OnQuestionMultiValidated;


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
        buttonLeftQuestionContainer = questionContainer.Find("LeftButton").GetComponent<Button>();
        buttonLeftTextQuestionContainer = buttonLeftQuestionContainer.GetComponentInChildren<TMP_Text>();
        buttonRightQuestionContainer = questionContainer.Find("RightButton").GetComponent<Button>();
        buttonRightTextQuestionContainer = buttonRightQuestionContainer.GetComponentInChildren<TMP_Text>();
        questionTextQuestionContainer = questionContainer.Find("Question").GetComponent<TMP_Text>();

        canvasGroupQuestionMultiContainer = questionMultiContainer.GetComponent<CanvasGroup>();
        buttonLeftQuestionMultiContainer = questionMultiContainer.Find("LeftButton").GetComponent<Button>();
        buttonRightQuestionMultiContainer = questionMultiContainer.Find("RightButton").GetComponent<Button>();
        carrouselTextQuestionMultiContainer = questionMultiContainer.Find("Carrousel").GetComponent<TMP_Text>();
        questionTextQuestionMultiContainer = questionMultiContainer.Find("Question").GetComponent<TMP_Text>();
        buttonValidationQuestionMultiContainer = questionMultiContainer.Find("ValidationButton").GetComponent<Button>();

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
        buttonLeftTextQuestionContainer.text = string.Empty;
        buttonRightTextQuestionContainer.text = string.Empty;

        canvasGroupQuestionMultiContainer.alpha = 0;
        questionTextQuestionMultiContainer.text = string.Empty;
        carrouselOptionList.Clear();

        sliderRightInputActionReference.action.Disable();
        sliderLeftInputActionReference.action.Disable();
        validationButtonLikertInputActionReference.action.Disable();

        skipHoldInputActionReference.action.Disable();

        buttonLeftInputActionReference.action.Disable();
        buttonRightInputActionReference.action.Disable();

        carrouselLeftInputActionReference.action.Disable();
        carrouselRightInputActionReference.action.Disable();
        validationButtonCarrouselInputActionReference.action.Disable();
    }

    private void OnEnable()
    {

        sliderRightInputActionReference.action.performed += OnSliderRightActionPerformed;
        sliderLeftInputActionReference.action.performed += OnSliderLeftActionPerformed;

        validationButtonLikertInputActionReference.action.performed += OnValidationLikertButtonActionPerformed;
        validationButtonLikertInputActionReference.action.started += OnValidationLikertButtonActionStarted;
        validationButtonLikertInputActionReference.action.canceled += OnValidationLikertButtonActionCanceled;

        sliderLikertScaleContainer.onValueChanged.AddListener(OnSliderValueChanged);

        validationButtonLikertScaleContrainer.onClick.AddListener(OnValidationLikertButtonPressed);

        skipHoldButtonBreakContainer.onClick.AddListener(OnSkipHoldButtonPressed);

        skipHoldInputActionReference.action.started += OnSkipHoldActionStarted;
        skipHoldInputActionReference.action.performed += OnSkipHoldActionPerformed;
        skipHoldInputActionReference.action.canceled += OnSkipHoldActionCanceled;

        buttonLeftQuestionContainer.onClick.AddListener(OnButtonLeftQuestionPressed);
        buttonRightQuestionContainer.onClick.AddListener(OnButtonRightQuestionPressed);

        buttonLeftInputActionReference.action.performed += OnButtonLeftActionPerformed;
        buttonLeftInputActionReference.action.started += OnButtonLeftActionStarted;
        buttonLeftInputActionReference.action.canceled += OnButtonLeftActionCanceled;

        buttonRightInputActionReference.action.performed += OnButtonRightActionPerformed;
        buttonRightInputActionReference.action.started += OnButtonRightActionStarted;
        buttonRightInputActionReference.action.canceled += OnButtonRightActionCanceled;

        carrouselLeftInputActionReference.action.performed += OnCarrouselLeftActionPerformed;
        carrouselRightInputActionReference.action.performed += OnCarrouselRightActionPerformed;

        validationButtonCarrouselInputActionReference.action.performed += OnValidationCarrouselButtonActionPerformed;
        validationButtonCarrouselInputActionReference.action.started += OnValidationCarrouselButtonActionStarted;
        validationButtonCarrouselInputActionReference.action.canceled += OnValidationCarrouselButtonActionCanceled;

        buttonValidationQuestionMultiContainer.onClick.AddListener(OnValidationCarrouselButtonPressed);


    }



    private void OnDisable()
    {

        sliderRightInputActionReference.action.performed -= OnSliderRightActionPerformed;
        sliderLeftInputActionReference.action.performed -= OnSliderLeftActionPerformed;

        validationButtonLikertInputActionReference.action.performed -= OnValidationLikertButtonActionPerformed;

        sliderLikertScaleContainer.onValueChanged.RemoveListener(OnSliderValueChanged);

        validationButtonLikertScaleContrainer.onClick.RemoveListener(OnValidationLikertButtonPressed);

        skipHoldButtonBreakContainer.onClick.RemoveListener(OnSkipHoldButtonPressed);

        skipHoldInputActionReference.action.started -= OnSkipHoldActionStarted;
        skipHoldInputActionReference.action.performed -= OnSkipHoldActionPerformed;
        skipHoldInputActionReference.action.canceled -= OnSkipHoldActionCanceled;


        buttonLeftQuestionContainer.onClick.RemoveListener(OnButtonLeftQuestionPressed);
        buttonRightQuestionContainer.onClick.RemoveListener(OnButtonRightQuestionPressed);

        carrouselLeftInputActionReference.action.performed -= OnCarrouselLeftActionPerformed;
        carrouselRightInputActionReference.action.performed -= OnCarrouselRightActionPerformed;


        validationButtonCarrouselInputActionReference.action.performed -= OnValidationCarrouselButtonActionPerformed;
        validationButtonCarrouselInputActionReference.action.started -= OnValidationCarrouselButtonActionStarted;
        validationButtonCarrouselInputActionReference.action.canceled -= OnValidationCarrouselButtonActionCanceled;

        buttonValidationQuestionMultiContainer.onClick.AddListener(OnValidationCarrouselButtonPressed);


    }

    private void OnSliderRightActionPerformed(InputAction.CallbackContext context)
    {
        sliderLikertScaleContainer.value += sliderStepValue;
    }
    private void OnSliderLeftActionPerformed(InputAction.CallbackContext context)
    {
        sliderLikertScaleContainer.value -= sliderStepValue;
    }

    private void OnValidationLikertButtonActionPerformed(InputAction.CallbackContext context)
    {
        validationButtonLikertScaleContrainer.onClick.Invoke();
    }
    private void OnValidationLikertButtonActionCanceled(InputAction.CallbackContext context)
    {
        if (validationButtonLikertScaleContrainer.targetGraphic)
        {
            validationButtonLikertScaleContrainer.targetGraphic.color = validationButtonLikertScaleContrainer.colors.normalColor;
        }
    }

    private void OnValidationLikertButtonActionStarted(InputAction.CallbackContext context)
    {
        if (validationButtonLikertScaleContrainer.targetGraphic)
        {
            validationButtonLikertScaleContrainer.targetGraphic.color = validationButtonLikertScaleContrainer.colors.pressedColor;
        }
    }

    private void OnButtonRightActionPerformed(InputAction.CallbackContext context)
    {
        buttonRightQuestionContainer.onClick.Invoke();
    }
    private void OnButtonRightActionStarted(InputAction.CallbackContext context)
    {
        if (buttonRightQuestionContainer.targetGraphic)
        {
            buttonRightQuestionContainer.targetGraphic.color = buttonRightQuestionContainer.colors.pressedColor;
        }
    }
    private void OnButtonRightActionCanceled(InputAction.CallbackContext context)
    {
        if (buttonRightQuestionContainer.targetGraphic)
        {
            buttonRightQuestionContainer.targetGraphic.color = buttonRightQuestionContainer.colors.normalColor;
        }
    }
    private void OnButtonLeftActionPerformed(InputAction.CallbackContext context)
    {
        buttonLeftQuestionContainer.onClick.Invoke();
    }

    private void OnButtonLeftActionStarted(InputAction.CallbackContext context)
    {
        if (buttonLeftQuestionContainer.targetGraphic)
        {
            buttonLeftQuestionContainer.targetGraphic.color = buttonLeftQuestionContainer.colors.pressedColor;
        }
    }
    private void OnButtonLeftActionCanceled(InputAction.CallbackContext context)
    {
        if (buttonLeftQuestionContainer.targetGraphic)
        {
            buttonLeftQuestionContainer.targetGraphic.color = buttonLeftQuestionContainer.colors.normalColor;
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

    private void OnSkipHoldActionStarted(InputAction.CallbackContext context)
    {

        holdmaskButtonBreakContainer.fillAmount = 0;
        var hold = context.interaction as HoldInteraction;
        holdTween = Tween.UIFillAmount(holdmaskButtonBreakContainer, endValue: 1, duration: hold.duration);

        if (skipHoldButtonBreakContainer.targetGraphic)
        {
            skipHoldButtonBreakContainer.targetGraphic.color = skipHoldButtonBreakContainer.colors.pressedColor;
        }
    }

    private void OnSkipHoldActionPerformed(InputAction.CallbackContext context)
    {
        holdTween.Stop();

        holdmaskButtonBreakContainer.fillAmount = 0;

        if (skipHoldButtonBreakContainer.targetGraphic)
        {
            skipHoldButtonBreakContainer.targetGraphic.color = skipHoldButtonBreakContainer.colors.normalColor;
        }

        skipHoldButtonBreakContainer.onClick.Invoke();

    }

    private void OnSkipHoldActionCanceled(InputAction.CallbackContext context)
    {
        holdmaskButtonBreakContainer.fillAmount = 0;
        holdTween.Stop();

        if (skipHoldButtonBreakContainer.targetGraphic)
        {
            skipHoldButtonBreakContainer.targetGraphic.color = skipHoldButtonBreakContainer.colors.normalColor;
        }
    }

    private void OnButtonRightQuestionPressed()
    {
        OnQuestionValidated?.Invoke(QuestionAnswer.Right);
    }

    private void OnButtonLeftQuestionPressed()
    {
        OnQuestionValidated?.Invoke(QuestionAnswer.Left);
    }

    private void OnCarrouselLeftActionPerformed(InputAction.CallbackContext context)
    {
        DisplayNextCarrouselText(-1);
    }

    private void OnCarrouselRightActionPerformed(InputAction.CallbackContext context)
    {
        DisplayNextCarrouselText(1);
    }

    private void OnValidationCarrouselButtonActionPerformed(InputAction.CallbackContext context)
    {
        buttonValidationQuestionMultiContainer.onClick.Invoke();
    }

    private void OnValidationCarrouselButtonActionStarted(InputAction.CallbackContext context)
    {
        if (buttonValidationQuestionMultiContainer.targetGraphic)
        {
            buttonValidationQuestionMultiContainer.targetGraphic.color = buttonValidationQuestionMultiContainer.colors.pressedColor;
        }
    }

    private void OnValidationCarrouselButtonActionCanceled(InputAction.CallbackContext context)
    {
        if (buttonValidationQuestionMultiContainer.targetGraphic)
        {
            buttonValidationQuestionMultiContainer.targetGraphic.color = buttonValidationQuestionMultiContainer.colors.normalColor;
        }
    }

    private void OnValidationCarrouselButtonPressed()
    {
        OnQuestionMultiValidated?.Invoke(currentCarrouselIndex);
    }

    private void FadeCanvasGroup(CanvasGroup canvasGroup, float endValue, Action value = null)
    {
        Tween.Alpha(canvasGroup, endValue: endValue, duration: fadeDuration).OnComplete(value);
    }

    public void DisplayText(string text)
    {
        canvasGroupTextContainer.alpha = 0;
        canvasGroupTextContainer.interactable = true;
        tmpTextContainer.text = text;
        FadeCanvasGroup(canvasGroupTextContainer, 1);
    }

    public void HideText()
    {
        canvasGroupTextContainer.interactable = false;

        FadeCanvasGroup(canvasGroupTextContainer, 0, () =>
        {
            tmpTextContainer.text = string.Empty;
   
        });
    }

    public void DisplayLikertScale(string question, string labelLeft, string labelRight)
    {

        sliderRightInputActionReference.action.Enable();
        sliderLeftInputActionReference.action.Enable();
        validationButtonLikertInputActionReference.action.Enable();

        questionTextLikertScaleContainer.text = question;
        labelLeftTextLikertScaleContainer.text = labelLeft;
        labelRightTextLikertScaleContainer.text = labelRight;

        sliderLikertScaleContainer.SetValueWithoutNotify(UnityEngine.Random.Range(sliderLikertScaleContainer.minValue + 10, sliderLikertScaleContainer.maxValue - 10));

        DisplayValidationButton(false);

        canvasGroupLikertScaleContainer.interactable = true;
        FadeCanvasGroup(canvasGroupLikertScaleContainer, 1);
    }

    public void HideLikertScale()
    {

        sliderRightInputActionReference.action.Disable();
        sliderLeftInputActionReference.action.Disable();
        validationButtonLikertInputActionReference.action.Disable();

        canvasGroupLikertScaleContainer.interactable = false;
        FadeCanvasGroup(canvasGroupLikertScaleContainer, 0);
    }

    public void DisplayBreak(string instruction)
    {
        canvasGroupBreakContainer.alpha = 0;

        skipHoldInputActionReference.action.Enable();

        instructionTextBreakContainer.text = instruction;

        canvasGroupBreakContainer.interactable = true;
        FadeCanvasGroup(canvasGroupBreakContainer, 1);
    }

    public void UpdateCounter(string counter)
    {
        counterTextBreakContainer.text = counter;
    }

    public void HideBreak()
    {
        skipHoldInputActionReference.action.Disable();

        canvasGroupBreakContainer.interactable = false;
        FadeCanvasGroup(canvasGroupBreakContainer, 0);
    }

    public void DisplayImage(Sprite image, float scale)
    {
        canvasImageContainer.alpha = 0;

        imageImageContainer.sprite = image;

        imageImageTransform.localScale = Vector3.one * scale;


        canvasImageContainer.interactable = true;
        FadeCanvasGroup(canvasImageContainer, 1);

    }

    public void HideImage()
    {
        canvasImageContainer.interactable = false;

        FadeCanvasGroup(canvasImageContainer, 0, () =>
        {
            imageImageContainer.sprite = null;

        });

    }

    public void DisplayQuestion(string question, string buttonLeftText, string buttonRightText)
    {
        buttonLeftInputActionReference.action.Enable();
        buttonRightInputActionReference.action.Enable();

        canvasGroupQuestionContainer.alpha = 0;

        questionTextQuestionContainer.text = question;

        buttonLeftTextQuestionContainer.text = buttonLeftText;
        buttonRightTextQuestionContainer.text = buttonRightText;

        canvasGroupQuestionContainer.interactable = true;

        FadeCanvasGroup(canvasGroupQuestionContainer, 1);
    }

    public void HideQuestion()
    {

        buttonLeftInputActionReference.action.Disable();
        buttonRightInputActionReference.action.Disable();

        canvasGroupQuestionContainer.interactable = false;

        FadeCanvasGroup(canvasGroupQuestionContainer, 0, () =>
        {
            questionTextQuestionContainer.text = string.Empty;
            buttonLeftTextQuestionContainer.text = string.Empty;
            buttonRightTextQuestionContainer.text = string.Empty;
        });
    }

    public void DisplayQuestionMulti(string question, List<string> options)
    {
        carrouselLeftInputActionReference.action.Enable();
        carrouselRightInputActionReference.action.Enable();
        validationButtonCarrouselInputActionReference.action.Enable();

        canvasGroupQuestionMultiContainer.alpha = 0;
        questionTextQuestionMultiContainer.text = question;

        carrouselOptionList.Clear();
        carrouselOptionList = options;

        currentCarrouselIndex = Random.Range(0, carrouselOptionList.Count);

        DisplayNextCarrouselText(0);

        canvasGroupQuestionMultiContainer.interactable = true;

        FadeCanvasGroup(canvasGroupQuestionMultiContainer, 1);
    }

    public void HideQuestionMulti()
    {
        carrouselLeftInputActionReference.action.Disable();
        carrouselRightInputActionReference.action.Disable();
        validationButtonCarrouselInputActionReference.action.Disable();

        canvasGroupQuestionMultiContainer.interactable = false;

        FadeCanvasGroup(canvasGroupQuestionMultiContainer, 0, () =>
        {
            questionTextQuestionMultiContainer.text = string.Empty;
            carrouselTextQuestionMultiContainer.text = string.Empty;
            carrouselOptionList.Clear();
            currentCarrouselIndex = 0;

        });
    }

    private void DisplayNextCarrouselText(int Offset)
    {
        currentCarrouselIndex += Offset;

        currentCarrouselIndex = (currentCarrouselIndex + carrouselOptionList.Count) % carrouselOptionList.Count;

        carrouselTextQuestionMultiContainer.text = carrouselOptionList[currentCarrouselIndex];
    }
}
