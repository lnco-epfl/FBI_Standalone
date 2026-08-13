using System.Collections;
using UnityEngine;

public class PlaySoundState : IState
{
    private PlaySoundStep step;

    private AudioSource audioSource;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as PlaySoundStep;
    }

    public IEnumerator Execute()
    {
        EventFileManager.Log($"[PlaySoundState] Play Sound {step.sound.name} for {step.GetDuration()} seconds");

        SubtitleReader.Instance.SetAudioSubtitlesEnabled(step.subtitle);

        audioSource = AudioManager.instance.Play2DSFX(step.sound);

        yield return new WaitForSeconds(step.GetDuration());
    }

    public void Exit()
    {
        AudioManager.instance.KillSound(audioSource);
        if(step.subtitle)
        {
            SubtitleReader.Instance.HideSubtitle();
        }
     
    }

}