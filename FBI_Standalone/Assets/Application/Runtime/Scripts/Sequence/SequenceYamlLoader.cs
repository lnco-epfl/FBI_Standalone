using System;
using System.Collections.Generic;
using System.IO;
using Eflatun.SceneReference;
using UnityEngine;
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

        return sequence;
    }

    private SequenceStep CreateStepFromData(StepData data)
    {
        switch (data.stepType)
        {
            case SequenceStepWrapper.StepType.DisplayText:
                return new DisplayTextStep
                {
                    text = data.text,
                    diplayDuration = data.duration
                };

            case SequenceStepWrapper.StepType.Wait:
                return new WaitStep
                {
                    waitTime = data.duration
                };

            case SequenceStepWrapper.StepType.LoadScene:
                return new LoadSceneStep
                {
                    duration = data.duration,
                    Scene = LoadSceneReference(data.scenePath),
                };

            case SequenceStepWrapper.StepType.LoadConfig:
                return new LoadConfigStep
                {
                    fileName = data.fileName,
                };

            case SequenceStepWrapper.StepType.DisplayLikertScale:
                return new DisplayLikertScaleStep
                {
                    question = data.question,
                    leftLabel = data.leftLabel,
                    rightLabel = data.rightLabel,
                };

            case SequenceStepWrapper.StepType.Break:
                return new BreakStep
                {
                    instructionText = data.text,
                    duration = data.duration,
                };

            case SequenceStepWrapper.StepType.DisplayImage:
                return new DisplayImageStep
                {
                    image = LoadSprite(data.imagePath),
                    scale = data.scale,
                    diplayDuration = data.duration
                };

            case SequenceStepWrapper.StepType.DisplayQuestion:
                return new DisplayQuestionStep
                {
                    question = data.question,
                    responseOptions = data.options,
                };

            case SequenceStepWrapper.StepType.PlaySound:
                return new PlaySoundStep
                {
                    sound = LoadAudioClip(data.soundPath)
                };
            case SequenceStepWrapper.StepType.DisplayCamera:
                return new DisplayCameraStep
                {
                    displayTime = data.duration,
                    cameraID = data.cameraID,
                    delay = data.delay
                };

            default:
                return new WaitStep();
        }
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

    // Text
    public string text;
    public string question;
    public string leftLabel;
    public string rightLabel;
    public List<string> options;

    //camera
    public string cameraID;
    public float delay;

    // Assets
    public string scenePath;
    public string imagePath;
    public string soundPath;
    public string fileName;

    // Image
    public float scale;

}