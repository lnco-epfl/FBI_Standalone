using PrimeTween;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.VFX;

public class DisplayCamerasState : IState
{
    private DisplayCamerasStep step;

    private ConfigFile previousConfig;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayCamerasStep;
    }

    public IEnumerator Execute()
    {

        // to DO, boucle for multiple cameras in the same step
        // for interpolation Create a tween sequence for executing all the transition a the in the timelime 

        var pointCloudID = int.Parse(step.cameraID);
        var pointCloudContainer = PointCloudManager.Instance.GetPointCloudContainer(pointCloudID);

        VisualEffect vfxEffect = pointCloudContainer.vfx;
        PointCloudReplayBuffer pointCloudReplayBuffer = pointCloudContainer.replayBuffer;
        RealtimeDelaySwitcher realtimeDelaySwitcher = pointCloudContainer.realtimeDelaySwitcher;

        if(step.interpolation != null )
        {
            previousConfig = ConfigFileManager.Instance.CurrentConfig;
        }

        if(!string.IsNullOrEmpty(step.configFileName))
        {
            var configs = ConfigFileManager.Instance.GetAvailableConfigs();

            if (configs.Contains(step.configFileName) && ConfigFileManager.Instance.CurrentConfig.configName != step.configFileName)
            {
                ConfigFileManager.Instance.Load(step.configFileName);
            }
            else
            {
                EventFileManager.Error($"[DisplayCameraState] Config file {step.configFileName} not found");
            }
        }

        OutputFileManager.Instance.OutputFileData.TimeSinceStart = ExperimentManager.Instance.ElaspedTimeSinceStart;

        OutputFileManager.Instance.OutputFileData.StepType = "DisplayCamera";
        OutputFileManager.Instance.OutputFileData.StepCount = ExperimentManager.Instance.SequenceCurrentStep;

        OutputFileManager.Instance.OutputFileData.CameraDelay = step.delay;
        OutputFileManager.Instance.OutputFileData.CameraDisplayDuration = step.displayTime;
        OutputFileManager.Instance.OutputFileData.CameraID = step.cameraID;

        realtimeDelaySwitcher.displayMode = step.delay > 0.0f ? RealtimeDelaySwitcher.DisplayMode.Delay : RealtimeDelaySwitcher.DisplayMode.Realtime;
        pointCloudReplayBuffer.replayDelaySeconds = step.delay;

        //pointCloudReplayBuffer.enabled = step.delay > 0.0f ? true : false;
        pointCloudReplayBuffer.enableReplay = step.delay > 0.0f ? true : false;

        vfxEffect.enabled = true;

        EventFileManager.Log($"[DisplayCameraState] Display Camera {step.cameraID} for {step.GetDuration()} seconds with {step.delay} of delay");

        if (step.interpolation != null)
        {
            var currentConfig = ConfigFileManager.Instance.CurrentConfig;

            yield return new WaitForSeconds(step.interpolation.delay);

            yield return Tween.Position(target: vfxEffect.transform, startValue: previousConfig.pointClouds[pointCloudID - 1].position.ToVector3(), endValue: currentConfig.pointClouds[pointCloudID - 1].position.ToVector3(), duration: step.interpolation.duration, ease: step.interpolation.ease)
                .Group(Tween.Rotation(target: vfxEffect.transform, startValue: previousConfig.pointClouds[pointCloudID - 1].rotation.ToVector3(), endValue: currentConfig.pointClouds[pointCloudID - 1].rotation.ToVector3(), duration: step.interpolation.duration, ease: step.interpolation.ease)).ToYieldInstruction();
            //.Group(Tween.Scale(target: vfxEffect.transform, startValue: previousConfig.pointClouds[pointCloudID].scale.ToVector3(), endValue: currentConfig.pointClouds[pointCloudID].scale.ToVector3(), duration: step.interpolation.duration, ease: step.interpolation.ease)).ToYieldInstruction();

            yield return new WaitForSeconds(step.displayTime - step.interpolation.duration - step.interpolation.delay);
        }
        else
        {
            yield return new WaitForSeconds(step.displayTime);
        }

        //pointCloudReplayBuffer.enabled = false;
        pointCloudReplayBuffer.enableReplay = false;
        vfxEffect.enabled = false;

        OutputFileManager.Instance.SaveOutputEntry();

        // wait for the memory properly allocated
        yield return new WaitForSeconds(0.5f);

    }

    public void Exit()
    {
        var pointCloudContainer = PointCloudManager.Instance.GetPointCloudContainer(int.Parse(step.cameraID));

        VisualEffect vfxEffect = pointCloudContainer.vfx;
        PointCloudReplayBuffer pointCloudReplayBuffer = pointCloudContainer.replayBuffer;
        RealtimeDelaySwitcher realtimeDelaySwitcher = pointCloudContainer.realtimeDelaySwitcher;

        pointCloudReplayBuffer.enableReplay = false;
        vfxEffect.enabled = false;

        Tween.StopAll(onTarget: vfxEffect.transform);

    }
}
