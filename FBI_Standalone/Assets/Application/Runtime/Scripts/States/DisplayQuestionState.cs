using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public enum QuestionAnswer
{
    None = -1,
    Left = 0,
    Right = 1
}

public class DisplayQuestionState : IState
{
    private DisplayQuestionStep step;

    private QuestionAnswer questionValue = QuestionAnswer.None;



    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayQuestionStep;

        WorldUIManager.Instance.OnQuestionValidated += OnQuestionValidated;
    }

    public IEnumerator Execute()
    {

        questionValue = QuestionAnswer.None;

        OutputFileManager.Instance.OutputFileData.TimeSinceStart = ExperimentManager.Instance.ElaspedTimeSinceStart;

        OutputFileManager.Instance.OutputFileData.StepType = "Question";
        OutputFileManager.Instance.OutputFileData.StepCount = ExperimentManager.Instance.SequenceCurrentStep;

        EventFileManager.Log($"[DisplayQuestionState] DisplayQuestion");

        WorldUIManager.Instance.DisplayQuestion(step.question, step.responseOptions);

        float startTime = Time.time;

        yield return new WaitUntil(() => questionValue != QuestionAnswer.None);

        float endTime = Time.time;
        float responseTime = endTime - startTime;

        OutputFileManager.Instance.OutputFileData.QuestionResponseTime = responseTime;
        OutputFileManager.Instance.OutputFileData.QuestionResponse = questionValue.ToString();

        EventFileManager.Log($"[DisplayQuestionState] Question Answer {questionValue} in {responseTime}");

        OutputFileManager.Instance.SaveOutputEntry();

        yield return new WaitForSeconds(0.5f);

        WorldUIManager.Instance.HideQuestion();

    }

    public void Exit()
    {
        WorldUIManager.Instance.OnQuestionValidated -= OnQuestionValidated;

        WorldUIManager.Instance.HideQuestion();

    }

    private void OnQuestionValidated(QuestionAnswer value)
    {
        questionValue = value;

    }
}