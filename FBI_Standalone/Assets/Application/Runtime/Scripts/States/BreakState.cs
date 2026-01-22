using System.Collections;
using UnityEngine;

public class BreakState : IState
{

    private BreakStep step;

    public float intervalTime = 1f;
    private bool skipFromUser;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as BreakStep;

        WorldUIManager.Instance.OnSkipHoldValidated += OnSkipHoldValidated;
    }

    public IEnumerator Execute()
    {

        skipFromUser = false;
        int currentValue = (int)step.duration;

        WorldUIManager.Instance.DisplayBreak(step.instructionText);

        yield return new WaitForSeconds(1.0f);

        while (currentValue >= 0 && !skipFromUser)
        {
            WorldUIManager.Instance.UpdateCounter(currentValue.ToString());

            yield return new WaitForSeconds(intervalTime);

            currentValue--;
        }

        WorldUIManager.Instance.HideBreak();

    }

    public void Exit()
    {
        WorldUIManager.Instance.OnSkipHoldValidated -= OnSkipHoldValidated;

        WorldUIManager.Instance.HideBreak();
    }

    private void OnSkipHoldValidated()
    {
        skipFromUser = true;
    }
}
