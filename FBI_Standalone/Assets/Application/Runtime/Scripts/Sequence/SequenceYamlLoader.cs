using Eflatun.SceneReference;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class SequenceYamlLoader
{
    private IDeserializer deserializer;
    private ISerializer serializer;

    private static SequenceYamlLoader instance;
    public static SequenceYamlLoader Instance
    {
        get
        {
            if (instance == null)
                instance = new SequenceYamlLoader();
            return instance;
        }
    }

    private SequenceYamlLoader()
    {
        deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
    }

    public Sequence LoadSequenceFromYaml(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"YAML file not found: {filePath}");
                return null;
            }

            string yamlContent = File.ReadAllText(filePath);
            var sequenceData = deserializer.Deserialize<SequenceData>(yamlContent);

            return ConvertToSequence(sequenceData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading YAML sequence: {e.Message}");
            return null;
        }
    }


    private Sequence ConvertToSequence(SequenceData data)
    {
        var sequence = ScriptableObject.CreateInstance<Sequence>();
        sequence.steps = new List<SequenceStepWrapper>();

        foreach (var stepData in data.steps)
        {
            var wrapper = new SequenceStepWrapper();
            wrapper.stepType = stepData.stepType;
            wrapper.step = CreateStepFromData(stepData);
            sequence.steps.Add(wrapper);
        }

        sequence.steps.Sort((a, b) => a.step.startTime.CompareTo(b.step.startTime));

        return sequence;
    }

    private SequenceStep CreateStepFromData(StepData data)
    {
        switch (data.stepType)
        {
            case SequenceStepWrapper.StepType.DisplayText:
                return new DisplayTextStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    text = data.text,
                    diplayDuration = data.duration
                };

            case SequenceStepWrapper.StepType.Wait:
                return new WaitStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    waitTime = data.duration
                };

            case SequenceStepWrapper.StepType.LoadScene:
                return new LoadSceneStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    duration = data.duration,
                    scene = LoadSceneReference(data.scenePath),
                };

            case SequenceStepWrapper.StepType.LoadConfig:
                return new LoadConfigStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    fileName = data.configName,
                };

            case SequenceStepWrapper.StepType.DisplayLikertScale:
                return new DisplayLikertScaleStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    question = data.question,
                    leftLabel = data.leftLabel,
                    rightLabel = data.rightLabel,
                    min = data.min,
                    max = data.max,
                    randomCursorPosition = data.randomCursorPosition
                };

            case SequenceStepWrapper.StepType.Break:
                return new BreakStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    instructionText = data.text,
                    duration = data.duration,
                };

            case SequenceStepWrapper.StepType.DisplayImage:
                return new DisplayImageStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    image = LoadSprite(data.imagePath),
                    scale = data.scale,
                    diplayDuration = data.duration
                };

            case SequenceStepWrapper.StepType.DisplayQuestion:
                return new DisplayQuestionStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    question = data.question,
                    responseOptions = data.options,
                };

            case SequenceStepWrapper.StepType.PlaySound:
                return new PlaySoundStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    subtitle = data.subtitle,
                    sound = LoadAudioClip(data.soundPath)
                };

            case SequenceStepWrapper.StepType.DisplayVideo:
                return new DisplayVideoStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    videoName = data.videoName,
                    looping = data.looping,
                    muteAudio = data.muteAudio,
                    subtitle = data.subtitle,
                };

            case SequenceStepWrapper.StepType.DisplayCameras:
                return new DisplayCamerasStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    displayTime = data.duration,
                    camerasData = data.cameraDatas,
                    rigInterpolation = ConvertRigInterpolation(data.rigInterpolation),
                };

            case SequenceStepWrapper.StepType.SendLSLEvent:
                return new SendLSLEventStep
                {
                    startTime = data.startTime,
                    blocking = data.blocking,
                    eventName = data.eventName,
                };

            default:
                return new WaitStep();
        }
    }

    // Returns null if no rigInterpolation block was present in the YAML (field stays null = feature disabled).
    private RigInterpolationData ConvertRigInterpolation(RigInterpolationYamlData data)
    {
        if (data == null)
            return null;

        return new RigInterpolationData
        {
            startPosition = new Vector3(data.startPosition.x, data.startPosition.y, data.startPosition.z),
            endPosition = new Vector3(data.endPosition.x, data.endPosition.y, data.endPosition.z),
            startYaw = data.startYaw,
            endYaw = data.endYaw,
            duration = data.duration,
            delay = data.delay,
            ease = data.ease,
        };
    }

    private SceneReference LoadSceneReference(string path)
    {
        return SceneReference.FromScenePath("Assets/Application/Runtime/Scenes/" + path + ".unity");
    }

    private Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        return AssetsManager.Instance.GetSprite(path);
    }

    private AudioClip LoadAudioClip(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        return AssetsManager.Instance.GetAudioClip(path);
    }
}


[Serializable]
public class SequenceData
{
    public List<StepData> steps = new List<StepData>();
}

[Serializable]
public class StepData
{
    public SequenceStepWrapper.StepType stepType;

    public float duration;
    public bool blocking;

    public float startTime;

    // Text
    public string text;
    public string question;
    public string leftLabel;
    public string rightLabel;
    public List<string> options;

    // Likert
    public int min;
    public int max;
    public bool randomCursorPosition;

    // Camera
    public List<CameraData> cameraDatas;
    public float delay;

    // Rig interpolation (optional — null if not present in YAML)
    public RigInterpolationYamlData rigInterpolation;

    // Assets
    public string scenePath;
    public string imagePath;
    public string soundPath;
    public string videoName;

    // Config
    public string configName;

    // Image
    public float scale;

    // Video
    public bool looping;
    public bool muteAudio;

    public bool subtitle;

    // LSL Event
    public string eventName;
}

// Separate YAML DTO for RigInterpolationData because Vector3 doesn't deserialize
// directly from YAML — we use a plain XYZ struct instead and convert in ConvertRigInterpolation().
[Serializable]
public class RigInterpolationYamlData
{
    public Vector3YamlData startPosition = new Vector3YamlData();
    public Vector3YamlData endPosition = new Vector3YamlData();

    public float startYaw = 0f;
    public float endYaw = 0f;

    public float duration = 0f;
    public float delay = 0f;
    public PrimeTween.Ease ease = PrimeTween.Ease.Default;
}

[Serializable]
public class Vector3YamlData
{
    public float x = 0f;
    public float y = 0f;
    public float z = 0f;
}