using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public enum QuestionAnswer
{
    None = -1,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,

}

public class DisplayQuestionState : IState
{
    private DisplayQuestionStep step;

    private List<QuestionAnswer> questionValue = new List<QuestionAnswer>();

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayQuestionStep;

        WorldUIManager.Instance.OnQuestionValidated += OnQuestionValidated;
    }

    public IEnumerator Execute()
    {

        questionValue = new List<QuestionAnswer>();

        OutputFileManager.Instance.OutputFileData.TimeSinceStart = ExperimentManager.Instance.ElaspedTimeSinceStart;

        OutputFileManager.Instance.OutputFileData.StepType = "Question";
        OutputFileManager.Instance.OutputFileData.StepCount = ExperimentManager.Instance.SequenceCurrentStep;

        EventFileManager.Log($"[DisplayQuestionState] DisplayQuestion \"{step.question} \"with anwsers \"{string.Join(",", step.responseOptions)}\"");

        WorldUIManager.Instance.DisplayQuestion(step.question, step.responseOptions, step.allowMultipleResponses);

        float startTime = Time.time;

        yield return new WaitUntil(() => questionValue.Count != 0);

        float endTime = Time.time;
        float responseTime = endTime - startTime;

        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < questionValue.Count; i++)
        {
            stringBuilder.Append(step.responseOptions[(int)questionValue[i] - 1]);

            if(questionValue.Count >= 1 && questionValue.Count - 1 > i)
            stringBuilder.Append(",");
        }

        OutputFileManager.Instance.OutputFileData.QuestionResponseTime = responseTime;
        OutputFileManager.Instance.OutputFileData.QuestionResponse = stringBuilder.ToString();
        OutputFileManager.Instance.OutputFileData.QuestionResponseIndex = string.Join(",", questionValue);

        EventFileManager.Log($"[DisplayQuestionState] Question Answer \"{stringBuilder.ToString()}\" at index {string.Join(",", questionValue)} in {responseTime}");

        OutputFileManager.Instance.SaveOutputEntry();

        yield return new WaitForSeconds(0.5f);

        WorldUIManager.Instance.HideQuestion();

    }

    public void Exit()
    {
        WorldUIManager.Instance.OnQuestionValidated -= OnQuestionValidated;

        WorldUIManager.Instance.HideQuestion();

    }

    private void OnQuestionValidated(List<QuestionAnswer> answers)
    {
        questionValue = answers;
    }

}