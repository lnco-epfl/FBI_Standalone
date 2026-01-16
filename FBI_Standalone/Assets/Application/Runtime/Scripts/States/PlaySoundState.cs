using System.Collections;
using UnityEngine;

public class PlaySoundState : IState
{
    private PlaySoundStep step;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as PlaySoundStep;
    }

    public IEnumerator Execute()
    {
        EventFileManager.Log($"PlaySoundState Play Sound {step.sound} for {step.GetDuration()} seconds");

        AudioManager.instance.Play2DSFX(step.sound);

        yield return new WaitForSeconds(step.GetDuration());
    }

    public void Exit()
    {

    }

}