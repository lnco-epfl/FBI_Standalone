using System.Collections;
using UnityEngine;

public class LoadSceneState : IState
{
    private LoadSceneStep step;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as LoadSceneStep;
    }

    public IEnumerator Execute()
    {

        Fader.Instance.FadeToBlack();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);


        EventFileManager.Log($"[LoadSceneStatea] LoadAsyncScene {step.scene.Name}");
        yield return SceneLoaderManager.Instance.LoadAsyncScene(step.scene);

        OutputFileManager.Instance.OutputFileData.Scene = step.scene.Name;

        Fader.Instance.FadeToClear();
        yield return new WaitForSeconds(Fader.Instance.FadeDuration * 2.0f);

    }

    public void Exit()
    {

    }
}
