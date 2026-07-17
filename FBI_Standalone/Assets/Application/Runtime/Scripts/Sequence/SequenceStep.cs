using Eflatun.SceneReference;
using PrimeTween;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[System.Serializable]
public abstract class SequenceStep
{
    public float startTime = 0f;
    public bool blocking = false;

    public virtual float GetDuration() => 1f;
    public abstract string GetStateName();
    public virtual string GetDisplayName() => GetStateName();
    public virtual Color GetColor() => Color.black;
}

[System.Serializable]
public class DisplayTextStep : SequenceStep
{
    public string text;

    public float diplayDuration = 5f;

    public override float GetDuration() => diplayDuration;
    public override string GetStateName() => "DisplayText";

    public override string GetDisplayName()
    {

        var displaytext = string.IsNullOrEmpty(text) ? "Not text" : text;

        return $"Display {displaytext}";

    }
    public override Color GetColor() => Color.blue;
}

[System.Serializable]
public class WaitStep : SequenceStep
{
    public float waitTime = 1f;
    public override float GetDuration() => waitTime;
    public override string GetStateName() => "Wait";
    public override string GetDisplayName() => $"Wait {waitTime}s";
    public override Color GetColor() => Color.cyan;
}

[System.Serializable]
public class LoadSceneStep : SequenceStep
{
    public float duration = 1f;

    public SceneReference scene;

    public override float GetDuration() => duration;
    public override string GetStateName() => "LoadScene";
    public override string GetDisplayName()
    {

        var sceneName = scene.State != SceneReferenceState.Unsafe ? scene.Name : "Unknown Scene";

        return $"Load Scene {sceneName}";

    }
    public override Color GetColor() => Color.green;
}

[System.Serializable]
public class LoadConfigStep : SequenceStep
{

    public string fileName;

    public override float GetDuration() => 1.0f;
    public override string GetStateName() => "LoadConfig";
    public override string GetDisplayName()
    {
        return $"Load Scene {fileName}";
    }
    public override Color GetColor() => Color.brown;
}

[System.Serializable]
public class LoadDisplayConfigStep : SequenceStep
{

    public string configName;

    public SerializableVector3 positionOverride;
    public SerializableVector3 rotationOverride;
    public SerializableVector3 scaleOverride;
    public SerializableColor backgroundColorOverride;

    public override float GetDuration() => 0f;
    public override string GetStateName() => "LoadDisplayConfig";
    public override string GetDisplayName()
    {
        var name = string.IsNullOrEmpty(configName) ? "(overrides only)" : configName;
        return $"Load Display Config {name}";
    }
    public override Color GetColor() => Color.brown;
}

[System.Serializable]
public class DisplayLikertScaleStep : SequenceStep
{
    public string question;

    public string leftLabel;

    public string rightLabel;

    public int min;

    public int max;

    public bool randomCursorPosition;

    public override float GetDuration() => 10f;
    public override string GetStateName() => "DisplayLikertScale";
    public override string GetDisplayName()
    {
        var text = string.IsNullOrEmpty(question) ? "No Text" : question;
        return $"Display Likert Scale {text}";

    }
    public override Color GetColor() => Color.magenta;
}

[System.Serializable]
public class BreakStep : SequenceStep
{
    public string instructionText;

    public float duration = 90;
    public override float GetDuration() => duration;
    public override string GetStateName() => "Break";
    public override string GetDisplayName()
    {
        return $"Break of {duration}";
    }
    public override Color GetColor() => Color.red;
}

[System.Serializable]
public class DisplayImageStep : SequenceStep
{
    public Sprite image;

    public float scale;

    public bool fixationCross;

    public float diplayDuration = 5f;

    public override float GetDuration() => diplayDuration;
    public override string GetStateName() => "DisplayImage";

    public override string GetDisplayName()
    {

        var displaytext = image != null ? image.name : "Not Image";

        return $"Display {displaytext}";

    }
    public override Color GetColor() => Color.white;
}


[System.Serializable]
public class DisplayQuestionStep : SequenceStep
{
    public string question;

    public List<string> responseOptions = new List<string>();

    public bool allowMultipleResponses = false;
    public override float GetDuration() => 10f;
    public override string GetStateName() => "DisplayQuestion";
    public override string GetDisplayName()
    {
        var text = string.IsNullOrEmpty(question) ? "No Text" : question;
        return $"Display Question {text}";
    }
    public override Color GetColor() => Color.yellow;
}

[System.Serializable]
public class PlaySoundStep : SequenceStep
{
    public AudioClip sound;

    public bool subtitle = false;
    public override float GetDuration()
    {
        return sound != null ? sound.length : 0.0f;
    }
    public override string GetStateName() => "PlaySound";
    public override string GetDisplayName()
    {
        var text = sound == null ? "No Text" : sound.name;
        return $"Play sound {text}";
    }
    public override Color GetColor() => Color.orange;
}

[Serializable]
public class InterpolationData
{
    public float duration = 0;
    public float delay = 0;
    public Ease ease = Ease.Default;
    public string startConfigName = string.Empty;
}

public class Dissolution
{
    public float duration = 0;
    public float delay = 0;
}

public class Fade
{
    public float duration = 0;
    public float delay = 0;
}

[Serializable]
public class RigInterpolationData
{

    public Vector3 startPosition = Vector3.zero;
    public Vector3 endPosition = Vector3.zero;

    public float startYaw = 0f;
    public float endYaw = 0f;

    public float duration = 0f;
    public float delay = 0f;

    public Ease ease = Ease.Default;

}

public class CameraData
{
    public string id = "1";
    public float delay = 0.0f;
    public string configName = string.Empty;

    public InterpolationData interpolation;
    public Dissolution dissolution;
    public Fade fade;
}

[System.Serializable]
public class DisplayCamerasStep : SequenceStep
{

    public float displayTime = 1f;

    public List<CameraData> camerasData = new List<CameraData>();

    public RigInterpolationData rigInterpolation;

    [NonSerialized] public List<PointCloud> ownedPointClouds = new List<PointCloud>();

    public override float GetDuration() => displayTime;
    public override string GetStateName() => "DisplayCameras";
    public override string GetDisplayName() => $"Display Cameras {camerasData.Count} for {displayTime}";
    public override Color GetColor() => Color.grey;
}


[System.Serializable]
public class DisplayVideoStep : SequenceStep
{
    public string videoName;

    public bool looping = false;
    public bool muteAudio = false;
    public bool subtitle = true;

    public float displayDuration = 10f;

    public override float GetDuration() => displayDuration;
    public override string GetStateName() => "DisplayVideo";
    public override string GetDisplayName()
    {
        var name = string.IsNullOrEmpty(videoName) ? "No Video" : videoName;
        return $"Display Video {name}";
    }
    public override Color GetColor() => Color.azure;
}

[System.Serializable]
public class SendLSLEventStep : SequenceStep
{
    public string eventName;

    public override float GetDuration() => 0f;
    public override string GetStateName() => "SendLSLEvent";

    public override string GetDisplayName()
    {
        var name = string.IsNullOrEmpty(eventName) ? "No Event" : eventName;
        return $"Send LSL Event {name}";
    }
    public override Color GetColor() => Color.floralWhite;
}

[Serializable]
public class SequenceStepWrapper
{

    public StepType stepType;

    [SerializeReference]
    public SequenceStep step;

    public enum StepType
    {
        DisplayText, Wait, SpawnObject, LoadScene, LoadConfig, LoadDisplayConfig, DisplayLikertScale, Break, DisplayImage, DisplayQuestion, PlaySound, DisplayCameras, DisplayVideo, SendLSLEvent
    }

    public SequenceStepWrapper()
    {
        stepType = StepType.Wait;
        step = new WaitStep();
    }

    public void UpdateStepType()
    {
        switch (stepType)
        {
            case StepType.DisplayText: step = new DisplayTextStep(); break;
            case StepType.Wait: step = new WaitStep(); break;
            case StepType.LoadScene: step = new LoadSceneStep(); break;
            case StepType.LoadConfig: step = new LoadConfigStep(); break;
            case StepType.LoadDisplayConfig: step = new LoadDisplayConfigStep(); break;
            case StepType.DisplayLikertScale: step = new DisplayLikertScaleStep(); break;
            case StepType.Break: step = new BreakStep(); break;
            case StepType.DisplayImage: step = new DisplayImageStep(); break;
            case StepType.DisplayQuestion: step = new DisplayQuestionStep(); break;
            case StepType.PlaySound: step = new PlaySoundStep(); break;
            case StepType.DisplayCameras: step = new DisplayCamerasStep(); break;
            case StepType.DisplayVideo: step = new DisplayVideoStep(); break;
            case StepType.SendLSLEvent: step = new SendLSLEventStep(); break;
        }
    }
}