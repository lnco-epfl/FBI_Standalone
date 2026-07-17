using System.Collections;
using UnityEngine;

public class LoadDisplayConfigState : IState
{
    private LoadDisplayConfigStep step;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as LoadDisplayConfigStep;
    }

    public IEnumerator Execute()
    {

        EventFileManager.Log($"[LoadDisplayConfigState] Load condig file {step.configName}");

        //var configs = ConfigFileManager.Instance.GetAvailableConfigs();

        //if (configs.Contains(step.configName))
        {

            DisplayConfigFileManager.Instance.ApplyStep(step, WorldUIManager.Instance.transform);

            //ConfigFileManager.Instance.Load(step.configName);
        }
        //else
        {
            EventFileManager.Error($"[LoadDisplayConfigState] Config file {step.configName} not found");
        }

        yield return new WaitForSeconds(0.5f);

    }

    public void Exit()
    {

    }

}

