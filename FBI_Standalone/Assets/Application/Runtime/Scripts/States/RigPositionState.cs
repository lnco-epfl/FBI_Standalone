using System.Collections;
using UnityEngine;

public class RigPositionState : IState
{
    private RigPositionStep step;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as RigPositionStep;
    }

    public IEnumerator Execute()
    {
        var origin = PlayerManager.Instance.transform;

        origin.position = step.position;
        origin.rotation = Quaternion.Euler(0f, step.yaw, 0f);

        EventFileManager.Log($"[RigPositionState] Rig positioned at {step.position} with yaw {step.yaw}");

        yield break;
    }

    public void Exit()
    {

    }
}
