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

        EventFileManager.Log($"[LoadDisplayConfigState] Load config file {step.configName}");

        if (!string.IsNullOrEmpty(step.configName))
        {
            var configs = DisplayConfigFileManager.Instance.GetAvailableConfigs();

            if (!configs.Contains(step.configName))
            {
                EventFileManager.Error($"[LoadDisplayConfigState] Config file {step.configName} not found");
                yield break;
            }
        }

        DisplayConfigFileManager.Instance.ApplyStep(step, WorldUIManager.Instance.transform);

        yield return new WaitForSeconds(0.5f);

    }

    public void Exit()
    {

    }

}