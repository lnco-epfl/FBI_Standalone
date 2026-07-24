using System.Collections;
using UnityEngine;

public class LoadCameraConfigState : IState
{
    private LoadCameraConfigStep step;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as LoadCameraConfigStep;
    }

    public IEnumerator Execute()
    {

        EventFileManager.Log($"[LoadConfigState] Load condig file {step.fileName}");

        var configs = CameraConfigFileManager.Instance.GetAvailableConfigs();

        if(configs.Contains(step.fileName))
        {
            CameraConfigFileManager.Instance.Load(step.fileName);
        }
        else
        {
            EventFileManager.Error($"[LoadConfigState] Config file {step.fileName} not found");
        }

        yield return new WaitForSeconds(0.5f);

    }

    public void Exit()
    {

    }
}
