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

        if (step.fadeToBlack)
        {
            Fader.Instance.FadeToBlack();
        }

        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

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

        if (step.fadeToClear)
        {
            Fader.Instance.FadeToClear();
        }
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);
    }

    public void Exit()
    {
        WorldUIManager.Instance.OnSkipHoldValidated -= OnSkipHoldValidated;

        WorldUIManager.Instance.HideBreak();

        if (step.fadeToClear)
        {
            Fader.Instance.FadeToClear();
        }
    }

    private void OnSkipHoldValidated()
    {
        skipFromUser = true;
    }
}
