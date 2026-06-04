using System.Collections;
using UnityEngine;

public class DisplayTextState : IState
{
    private DisplayTextStep step;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayTextStep;
    }

    public IEnumerator Execute()
    {

        if(step.text != null)
        {
            EventFileManager.Log($"[DisplayTextState] DisplayText \"{step.text}\" for {step.diplayDuration}s");

            WorldUIManager.Instance.DisplayText(step.text);

            yield return new WaitForSeconds(step.diplayDuration);

            WorldUIManager.Instance.HideText();
        }
     
    }

    public void Exit()
    {
        WorldUIManager.Instance.HideText();

    }

}
