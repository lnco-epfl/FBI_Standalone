
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GazeReticle : MonoBehaviour
{
    [SerializeField] private Image reticleImage;
    [SerializeField] private Image progressRing;
    [SerializeField] private XRGazeInteractor gazeInteractor;

    private float gazeStartTime;
    private bool isGazing;
    private bool startHover;

    private void OnEnable()
    {
        gazeInteractor.hoverEntered.AddListener(OnHoverEnter);
        gazeInteractor.hoverExited.AddListener(OnHoverExited);
    }

    private void OnDisable()
    {
        gazeInteractor.hoverEntered.RemoveListener(OnHoverEnter);
        gazeInteractor.hoverExited.RemoveListener(OnHoverExited);
    }

    private void OnHoverEnter(HoverEnterEventArgs hoverEnterEventArgs)
    {
        startHover = true;
    }

    private void OnHoverExited(HoverExitEventArgs hoverExitEventArgs)
    {
        startHover = false;
    }

    void Update()
    {
        UpdateReticle();
    }

    void UpdateReticle()
    {

        if (gazeInteractor.hasHover && startHover)
        {
            if (!isGazing)
            {
                isGazing = true;
                gazeStartTime = Time.time;
            }

            float progress = (Time.time - gazeStartTime) / gazeInteractor.hoverTimeToSelect;
            progressRing.fillAmount = Mathf.Clamp01(progress);

            reticleImage.color = Color.yellow;
        }
        else
        {
            isGazing = false;
            progressRing.fillAmount = 0f;
            reticleImage.color = Color.white;
        }
    }
}
