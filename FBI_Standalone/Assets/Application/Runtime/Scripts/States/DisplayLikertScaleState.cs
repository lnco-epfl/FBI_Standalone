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

        likertScaleValue = -1;

        OutputFileManager.Instance.OutputFileData.TimeSinceStart = ExperimentManager.Instance.ElaspedTimeSinceStart;

        OutputFileManager.Instance.OutputFileData.StepType = "LikertScale";
        OutputFileManager.Instance.OutputFileData.StepCount = ExperimentManager.Instance.SequenceCurrentStep;

        EventFileManager.Log($"[DisplayLikertScaleState] DisplayLikertScale \"{step.question}\" with on left \"{step.leftLabel}\" and on right \"{step.rightLabel}\"");

        WorldUIManager.Instance.DisplayLikertScale(step.question, step.leftLabel, step.rightLabel, step.min, step.max, step.randomCursorPosition);

        float startTime = Time.time;

        yield return new WaitUntil(() => likertScaleValue != -1);

        float endTime = Time.time;
        float responseTime = endTime - startTime;

        OutputFileManager.Instance.OutputFileData.LikertResponseTime = responseTime;
        OutputFileManager.Instance.OutputFileData.LikertResponse = likertScaleValue;

        EventFileManager.Log($"[DisplayLikertScaleState] Likert Scale rating {likertScaleValue} in {responseTime}s");

        OutputFileManager.Instance.SaveOutputEntry();

        yield return new WaitForSeconds(0.5f);

        WorldUIManager.Instance.HideLikertScale();
    }

    public void Exit()
    {
        WorldUIManager.Instance.OnLikertScaleValidated -= OnLikertScaleValidated;

        WorldUIManager.Instance.HideLikertScale();

    }

    private void OnLikertScaleValidated(int value)
    {
        likertScaleValue = value;

    }
}
