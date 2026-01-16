using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Classe pour charger et sauvegarder les séquences au format YAML
/// </summary>
public class SequenceYamlLoader
{
    private static IDeserializer deserializer;
    private static ISerializer serializer;

    static SequenceYamlLoader()
    {
        deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// Charge une séquence depuis un fichier YAML
    /// </summary>
    public static Sequence LoadSequenceFromYaml(string filePath)
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

    /// <summary>
    /// Sauvegarde une séquence dans un fichier YAML
    /// </summary>
    public static void SaveSequenceToYaml(Sequence sequence, string filePath)
    {
        try
        {
            var sequenceData = ConvertFromSequence(sequence);
            string yamlContent = serializer.Serialize(sequenceData);

            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, yamlContent);
            Debug.Log($"Sequence saved to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving YAML sequence: {e.Message}");
        }
    }

    /// <summary>
    /// Convertit les données YAML en objet Sequence
    /// </summary>
    private static Sequence ConvertToSequence(SequenceData data)
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

    /// <summary>
    /// Convertit un objet Sequence en données YAML
    /// </summary>
    private static SequenceData ConvertFromSequence(Sequence sequence)
    {
        var data = new SequenceData();
        data.steps = new List<StepData>();

        foreach (var wrapper in sequence.steps)
        {
            var stepData = CreateDataFromStep(wrapper);
            data.steps.Add(stepData);
        }

        return data;
    }

    /// <summary>
    /// Crée une instance de SequenceStep à partir des données YAML
    /// </summary>
    private static SequenceStep CreateStepFromData(StepData data)
    {
        switch (data.stepType)
        {
            case SequenceStepWrapper.StepType.DisplayText:
                return new DisplayTextStep
                {
                    text = LoadLocalizedString(data.text),
                    fadeToBlack = data.fadeToBlack,
                    fadeToClear = data.fadeToClear,
                    diplayDuration = data.displayDuration
                };

            case SequenceStepWrapper.StepType.Wait:
                return new WaitStep
                {
                    waitTime = data.waitTime
                };

            case SequenceStepWrapper.StepType.LoadScene:
                return new LoadSceneStep
                {
                    duration = data.duration,
                    Scene = LoadSceneReference(data.scenePath),
                    fadeToClear = data.fadeToClear
                };

            case SequenceStepWrapper.StepType.DisplayLikertScale:
                return new DisplayLikertScaleStep
                {
                    question = LoadLocalizedString(data.question),
                    leftLabel = LoadLocalizedString(data.leftLabel),
                    rightLabel = LoadLocalizedString(data.rightLabel),
                    fadeToBlack = data.fadeToBlack,
                    fadeToClear = data.fadeToClear
                };

            case SequenceStepWrapper.StepType.Break:
                return new BreakStep
                {
                    instructionText = LoadLocalizedString(data.instructionText),
                    duration = data.breakDuration,
                    fadeToBlack = data.fadeToBlack,
                    fadeToClear = data.fadeToClear
                };

            case SequenceStepWrapper.StepType.DisplayImage:
                return new DisplayImageStep
                {
                    image = LoadSprite(data.imagePath),
                    scale = data.scale,
                    fixationCross = data.fixationCross,
                    fadeToBlack = data.fadeToBlack,
                    fadeToClear = data.fadeToClear,
                    diplayDuration = data.displayDuration
                };

            case SequenceStepWrapper.StepType.DisplayQuestion:
                return new DisplayQuestionStep
                {
                    question = LoadLocalizedString(data.question),
                    leftLabel = LoadLocalizedString(data.leftLabel),
                    rightLabel = LoadLocalizedString(data.rightLabel),
                    correctResponse = data.correctResponse,
                    fadeToBlack = data.fadeToBlack,
                    fadeToClear = data.fadeToClear
                };

            case SequenceStepWrapper.StepType.PlaySound:
                return new PlaySoundStep
                {
                    sound = LoadAudioClip(data.soundPath)
                };


            default:
                return new WaitStep();
        }
    }

    /// <summary>
    /// Crée des données YAML à partir d'une instance de SequenceStep
    /// </summary>
    private static StepData CreateDataFromStep(SequenceStepWrapper wrapper)
    {
        var data = new StepData { stepType = wrapper.stepType };

        switch (wrapper.stepType)
        {
            case SequenceStepWrapper.StepType.DisplayText:
                var displayText = wrapper.step as DisplayTextStep;
                data.text = SaveLocalizedString(displayText.text);
                data.fadeToBlack = displayText.fadeToBlack;
                data.fadeToClear = displayText.fadeToClear;
                data.displayDuration = displayText.diplayDuration;
                break;

            case SequenceStepWrapper.StepType.Wait:
                var wait = wrapper.step as WaitStep;
                data.waitTime = wait.waitTime;
                break;

            case SequenceStepWrapper.StepType.LoadScene:
                var loadScene = wrapper.step as LoadSceneStep;
                data.duration = loadScene.duration;
                data.scenePath = SaveSceneReference(loadScene.Scene);
                data.fadeToClear = loadScene.fadeToClear;
                break;

            case SequenceStepWrapper.StepType.DisplayLikertScale:
                var likert = wrapper.step as DisplayLikertScaleStep;
                data.question = SaveLocalizedString(likert.question);
                data.leftLabel = SaveLocalizedString(likert.leftLabel);
                data.rightLabel = SaveLocalizedString(likert.rightLabel);
                data.fadeToBlack = likert.fadeToBlack;
                data.fadeToClear = likert.fadeToClear;
                break;

            case SequenceStepWrapper.StepType.Break:
                var breakStep = wrapper.step as BreakStep;
                data.instructionText = SaveLocalizedString(breakStep.instructionText);
                data.breakDuration = breakStep.duration;
                data.fadeToBlack = breakStep.fadeToBlack;
                data.fadeToClear = breakStep.fadeToClear;
                break;

            case SequenceStepWrapper.StepType.DisplayImage:
                var image = wrapper.step as DisplayImageStep;
                data.imagePath = SaveSprite(image.image);
                data.scale = image.scale;
                data.fixationCross = image.fixationCross;
                data.fadeToBlack = image.fadeToBlack;
                data.fadeToClear = image.fadeToClear;
                data.displayDuration = image.diplayDuration;
                break;

            case SequenceStepWrapper.StepType.DisplayQuestion:
                var question = wrapper.step as DisplayQuestionStep;
                data.question = SaveLocalizedString(question.question);
                data.leftLabel = SaveLocalizedString(question.leftLabel);
                data.rightLabel = SaveLocalizedString(question.rightLabel);
                data.correctResponse = question.correctResponse;
                data.fadeToBlack = question.fadeToBlack;
                data.fadeToClear = question.fadeToClear;
                break;

            case SequenceStepWrapper.StepType.PlaySound:
                var sound = wrapper.step as PlaySoundStep;
                data.soundPath = SaveAudioClip(sound.sound);
                break;

        }

        return data;
    }

    // Helper methods for loading Unity assets
    private static UnityEngine.Localization.LocalizedString LoadLocalizedString(string reference)
    {
        // Implémentation selon votre système de localisation
        return new UnityEngine.Localization.LocalizedString();
    }

    private static Eflatun.SceneReference.SceneReference LoadSceneReference(string path)
    {
        // Implémentation selon votre système de références de scène
        return new Eflatun.SceneReference.SceneReference();
    }

    private static Sprite LoadSprite(string path)
    {
        return string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);
    }

    private static AudioClip LoadAudioClip(string path)
    {
        return string.IsNullOrEmpty(path) ? null : Resources.Load<AudioClip>(path);
    }

    private static List<UnityEngine.Localization.LocalizedString> LoadLocalizedStringList(List<string> references)
    {
        var list = new List<UnityEngine.Localization.LocalizedString>();
        if (references != null)
        {
            foreach (var reference in references)
            {
                list.Add(LoadLocalizedString(reference));
            }
        }
        return list;
    }

    // Helper methods for saving Unity assets
    private static string SaveLocalizedString(UnityEngine.Localization.LocalizedString localizedString)
    {
        // Retourner la référence de la string localisée
        return localizedString?.TableReference.ToString() ?? "";
    }

    private static string SaveSceneReference(Eflatun.SceneReference.SceneReference scene)
    {
        return scene.Name ?? "";
    }

    private static string SaveSprite(Sprite sprite)
    {
        return sprite != null ? sprite.name : "";
    }

    private static string SaveAudioClip(AudioClip clip)
    {
        return clip != null ? clip.name : "";
    }

    private static List<string> SaveLocalizedStringList(List<UnityEngine.Localization.LocalizedString> strings)
    {
        var list = new List<string>();
        if (strings != null)
        {
            foreach (var str in strings)
            {
                list.Add(SaveLocalizedString(str));
            }
        }
        return list;
    }
}

/// <summary>
/// Structure de données pour la sérialisation YAML
/// </summary>
[Serializable]
public class SequenceData
{
    public List<StepData> steps = new List<StepData>();
}

[Serializable]
public class StepData
{
    public SequenceStepWrapper.StepType stepType;

    // Propriétés communes
    public float displayDuration;
    public float duration;
    public float waitTime;
    public int breakDuration;
    public bool fadeToBlack;
    public bool fadeToClear;

    // Text et localisation
    public string text;
    public string question;
    public string leftLabel;
    public string rightLabel;
    public string instructionText;
    public string correctResponse;
    public List<string> options;

    // Assets
    public string objectToSpawnPath;
    public string scenePath;
    public string imagePath;
    public string soundPath;

    // Image
    public float scale;
    public bool fixationCross;

    // Nested steps
    public StepData displayQuestion6Step;
    public StepData displayQuestion7MultiStep;
    public StepData displayLikert8ScaleStep;
}