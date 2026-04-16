using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class DisplayCameraState : IState
{
    private DisplayCameraStep step;



    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayCameraStep;
    }

    public IEnumerator Execute()
    {
        var pointCloudContainer = PointCloudManager.Instance.GetPointCloudContainer(int.Parse(step.cameraID));

        VisualEffect vfxEffect = pointCloudContainer.vfx;
        PointCloudReplayBuffer pointCloudReplayBuffer = pointCloudContainer.replayBuffer;
        RealtimeDelaySwitcher realtimeDelaySwitcher = pointCloudContainer.realtimeDelaySwitcher;


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

        EventFileManager.Log($"[DisplayCameraState] Display Camera {step.cameraID} for {step.GetDuration()} seconds with {step.delay}");

        yield return new WaitForSeconds(step.displayTime);

        //pointCloudReplayBuffer.enabled = false;
        pointCloudReplayBuffer.enableReplay = false;
        vfxEffect.enabled = false;

        OutputFileManager.Instance.SaveOutputEntry();

        // wait for the memory properly allocated
        yield return new WaitForSeconds(0.5f);

    }

    public void Exit()
    {

    }
}
