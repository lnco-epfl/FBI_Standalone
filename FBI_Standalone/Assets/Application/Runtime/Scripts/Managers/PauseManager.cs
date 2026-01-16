using PrimeTween;
using UnityEngine;
using UnityEngine.Localization;

public class PauseManager : MonoBehaviour
{

    [Header("Pause")]
    [SerializeField] private LocalizedString pauseText;

    private void OnEnable()
    {
        ExperimentManager.Instance.OnPause += OnExperimentPause;
    }

    private void OnDisable()
    {
        ExperimentManager.Instance.OnPause -= OnExperimentPause;
    }

    private void OnExperimentPause(bool isPause)
    {
        if (isPause)
        {
            Fader.Instance.FadeToBlack();

            Tween.Delay(Fader.Instance.FadeDuration).OnComplete(() =>
            {
                WorldUIManager.Instance.DisplayText(pauseText.GetLocalizedString());
            });

        }
        else
        {
            WorldUIManager.Instance.HideText();

            Tween.Delay(WorldUIManager.Instance.FadeDuration).OnComplete(() =>
            {
                Fader.Instance.FadeToClear();
            });

        }

    }
}
