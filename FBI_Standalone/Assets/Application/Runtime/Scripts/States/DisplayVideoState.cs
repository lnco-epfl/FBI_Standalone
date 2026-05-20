using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class DisplayVideoState : IState
{
    private DisplayVideoStep step;
    private bool videoFinished;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayVideoStep;
        videoFinished = false;
    }

    public IEnumerator Execute()
    {
        string videoPath = AssetsManager.Instance.GetVideoPath(step.videoName);

        if (string.IsNullOrEmpty(videoPath))
        {
            Debug.LogError($"[DisplayVideoState] Video not found: {step.videoName}");
            yield break;
        }

        float timeout = step.displayDuration;

        WorldUIManager.Instance.DisplayVideo(videoPath, step.looping, step.muteAudio,
            onFinished: () => videoFinished = true,
            onReady: (duration) =>
            {
                if (!step.looping)
                    timeout = duration;

                EventFileManager.Log($"[DisplayVideoState] DisplayVideo {step.videoName} duration={duration:F2}s looping={step.looping}");
            }
        );

        float elapsed = 0f;
        while (!videoFinished && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        WorldUIManager.Instance.HideVideo();
    }

    public void Exit()
    {
        WorldUIManager.Instance.HideVideo();
    }
}