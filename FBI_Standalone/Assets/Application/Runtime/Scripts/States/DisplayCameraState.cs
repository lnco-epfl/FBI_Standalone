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

        realtimeDelaySwitcher.enabled = true;
        pointCloudReplayBuffer.enabled = true;
        vfxEffect.enabled = true;

        yield return new WaitForSeconds(step.displayTime);

        realtimeDelaySwitcher.enabled = false;
        pointCloudReplayBuffer.enabled = false;
        vfxEffect.enabled = false;

        // wait for the memory properly allocated
        yield return new WaitForSeconds(5f);

    }

    public void Exit()
    {

    }
}
