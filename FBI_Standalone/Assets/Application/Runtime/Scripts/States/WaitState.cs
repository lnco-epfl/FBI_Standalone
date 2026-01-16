using System.Collections;
using UnityEngine;

public class WaitState : IState
{
    private WaitStep step;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as WaitStep;
    }

    public IEnumerator Execute()
    {
        EventFileManager.Log($"WaitState Waiting for {step.waitTime} s");

        yield return new WaitForSeconds(step.waitTime);
    }

    public void Exit()
    {

    }
}
