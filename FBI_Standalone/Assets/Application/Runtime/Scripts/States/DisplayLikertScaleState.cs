using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class DisplayLikertScaleState : IState
{
    private DisplayLikertScaleStep step;

    private int likertScaleValue = -1;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayLikertScaleStep;

        WorldUIManager.Instance.OnLikertScaleValidated += OnLikertScaleValidated;
    }

    public IEnumerator Execute()
    {

        if (step.fadeToBlack)
        {
            Fader.Instance.FadeToBlack();
        }
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        likertScaleValue = -1;

        OutputFileManager.Instance.OutputFileData.TimeSinceStart = ExperimentManager.Instance.ElaspedTimeSinceStart;

        OutputFileManager.Instance.OutputFileData.StepType = "LikertScale";
        OutputFileManager.Instance.OutputFileData.StepCount = ExperimentManager.Instance.SequenceCurrentStep;

        EventFileManager.Log($"DisplayLikertScaleState DisplayLikertScale {step.leftLabel}  {step.rightLabel}");

        WorldUIManager.Instance.DisplayLikertScale(step.question, step.leftLabel, step.rightLabel);

        float startTime = Time.time;

        yield return new WaitUntil(() => likertScaleValue != -1);

        float endTime = Time.time;
        float responseTime = endTime - startTime;

        OutputFileManager.Instance.SaveOutputEntry();

        yield return new WaitForSeconds(0.5f);

        WorldUIManager.Instance.HideLikertScale();

        if (step.fadeToClear)
        {
            Fader.Instance.FadeToClear();
        }
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);
    }

    public void Exit()
    {
        WorldUIManager.Instance.OnLikertScaleValidated -= OnLikertScaleValidated;

        WorldUIManager.Instance.HideLikertScale();

        if (step.fadeToClear)
        {
            Fader.Instance.FadeToClear();
        }
    }

    private void OnLikertScaleValidated(int value)
    {
        likertScaleValue = value;

    }
}
