using System.Collections;
using UnityEngine;

public class SendLSLEventState : IState
{
    private SendLSLEventStep step;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as SendLSLEventStep;
    }

    public IEnumerator Execute()
    {
        if (!string.IsNullOrEmpty(step.eventName))
        {
            EventFileManager.Log($"[SendEventState] SendEvent \"{step.eventName}\"");

            OutletManager.Instance?.Event.SendEvent(step.eventName);
        }

        yield return null;
    }

    public void Exit()
    {
    }
}