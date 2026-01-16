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

        if (step.fadeToBlack)
        {
            Fader.Instance.FadeToBlack();
        }

        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

        if(step.text != null)
        {
            EventFileManager.Log($"DisplayTextState DisplayText {step.text.GetLocalizedString()} for {step.diplayDuration}s");

            WorldUIManager.Instance.DisplayText(step.text?.GetLocalizedString());

            yield return new WaitForSeconds(step.diplayDuration);

            WorldUIManager.Instance.HideText();
        }
     

        if (step.fadeToClear)
        {
            Fader.Instance.FadeToClear();
        }

        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

    }

    public void Exit()
    {
        WorldUIManager.Instance.HideText();

        if (step.fadeToClear)
        {
            Fader.Instance.FadeToClear();
        }
    }

}
