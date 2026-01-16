using System.Collections;
using UnityEngine;

public class DisplayImageState : IState
{
    private DisplayImageStep step;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayImageStep;
    }

    public IEnumerator Execute()
    {

        if (step.fadeToBlack)
        {
            Fader.Instance.FadeToBlack();
        }
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        EventFileManager.Log($"DisplayImageState DisplayImage {step.image?.name} for {step.diplayDuration}s");

        if(step.fixationCross)
        {
            OutputFileManager.Instance.OutputFileData.TimeSinceStart = ExperimentManager.Instance.ElaspedTimeSinceStart;

            OutputFileManager.Instance.OutputFileData.StepType = "FixationCross";
            OutputFileManager.Instance.OutputFileData.StepCount = ExperimentManager.Instance.SequenceCurrentStep;

            OutputFileManager.Instance.OutputFileData.FixingCrossDuration = step.diplayDuration;

            OutputFileManager.Instance.SaveOutputEntry();
        }
      
        WorldUIManager.Instance.DisplayImage(step.image, step.scale);

        yield return new WaitForSeconds(step.diplayDuration);

        WorldUIManager.Instance.HideImage();

        if (step.fadeToClear)
        {
            Fader.Instance.FadeToClear();
        }
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);
    }

    public void Exit()
    {
        WorldUIManager.Instance.HideImage();

        if (step.fadeToClear)
        {
            Fader.Instance.FadeToClear();
        }
    }

}
