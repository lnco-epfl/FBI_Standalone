
using PrimeTween;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class GazeReticle : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image reticleImage;
    [SerializeField] private Image progressRing;
    [SerializeField] private XRGazeInteractor gazeInteractor;

    [Header("Filter Settings")]
    [SerializeField] private string interactiveTag = "UIGazeHover";

    private float gazeStartTime;
    private bool isGazing;


    private void OnEnable()
    {
        gazeInteractor.uiHoverEntered.AddListener(OnHoverEnter);
        gazeInteractor.uiHoverExited.AddListener(OnHoverExited);

        WorldUIManager.Instance.OnDisplayGazeCursor += OnDisplayGazeCursor;
    }

    private void OnDisable()
    {
        gazeInteractor.uiHoverEntered.RemoveListener(OnHoverEnter);
        gazeInteractor.uiHoverExited.RemoveListener(OnHoverExited);

        WorldUIManager.Instance.OnDisplayGazeCursor -= OnDisplayGazeCursor;
    }

    private void Start()
    {
        canvasGroup.alpha = 0.0f;
    }

    private void OnDisplayGazeCursor(bool idDisplay)
    {
        if(idDisplay)
        {
            DisplayGazeCursor();
        }
        else
        {
            HideGazeCursor();
        }
    }

    public void DisplayGazeCursor()
    {
        FadeCanvasGroup(canvasGroup, 0.25f, 1.0f, () => gazeInteractor.enabled = true);
    }

    public void HideGazeCursor()
    {
        FadeCanvasGroup(canvasGroup, 0.25f, 0.0f, () => gazeInteractor.enabled = false); 
    }


    private void FadeCanvasGroup(CanvasGroup canvasGroup,float duration, float endValue, Action value = null)
    {
        Tween.Alpha(canvasGroup, endValue: endValue, duration: duration).OnComplete(value);
    }


    private void OnHoverEnter(UIHoverEventArgs args)
    {
     
        if (!string.IsNullOrEmpty(interactiveTag) && args.uiObject.CompareTag(interactiveTag))
        {
            isGazing = true;
            gazeStartTime = Time.time;

            progressRing.color = Color.yellow;
        }

    }

    private void OnHoverExited(UIHoverEventArgs args)
    {
        if (!string.IsNullOrEmpty(interactiveTag) && args.uiObject.CompareTag(interactiveTag))
        {
            isGazing = false;
            progressRing.color = Color.white;
        }
    }



    void Update()
    {
        if (isGazing)
        {
            float progress = (Time.time - gazeStartTime) / gazeInteractor.hoverTimeToSelect;
            progressRing.fillAmount = Mathf.Clamp01(progress);
        }
        else
        {
            progressRing.fillAmount = 0f;
        }
    }
}

