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

        EventFileManager.Log($"[DisplayImageState] DisplayImage {step.image?.name} for {step.diplayDuration}s");
        
        WorldUIManager.Instance.DisplayImage(step.image, step.scale);

        yield return new WaitForSeconds(step.diplayDuration);

        WorldUIManager.Instance.HideImage();

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
