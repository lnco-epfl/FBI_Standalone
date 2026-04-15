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


        realtimeDelaySwitcher.displayMode = step.delay > 0.0f ? RealtimeDelaySwitcher.DisplayMode.Delay : RealtimeDelaySwitcher.DisplayMode.Realtime;
        pointCloudReplayBuffer.replayDelaySeconds = step.delay;

        pointCloudReplayBuffer.enableReplay = step.delay > 0.0f ? true : false;

        vfxEffect.enabled = true;

        EventFileManager.Log($"[DisplayCameraState] Display Camera {step.cameraID} for {step.GetDuration()} seconds with {step.delay}");

        yield return new WaitForSeconds(step.displayTime);

        pointCloudReplayBuffer.enableReplay = false;
        vfxEffect.enabled = false;

        // wait for the memory properly allocated
        yield return new WaitForSeconds(5f);

    }

    public void Exit()
    {

    }
}
