using System.Collections;
using UnityEngine;

public class LoadConfigState : IState
{
    private LoadConfigStep step;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as LoadConfigStep;
    }

    public IEnumerator Execute()
    {

        EventFileManager.Log($"[LoadConfigState] Load condig file {step.fileName}");

        var configs = ConfigFileManager.Instance.GetAvailableConfigs();

        if(configs.Contains(step.fileName))
        {
            ConfigFileManager.Instance.Load(step.fileName);
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
