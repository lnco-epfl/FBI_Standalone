using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SequenceStateMachine : MonoBehaviour
{
    private Sequence sequence;
    private string currentSequenceName;

    private Dictionary<string, IState> states;
    private Coroutine currentSequenceCoroutine;
    private Coroutine currentStateCoroutine;
    private IState currentState;

    public Action OnSequenceComplete;

    public bool IsPlaying => isPlaying;
    private bool isPlaying = false;
    public bool IsPaused => isPaused;
    private bool isPaused = false;
    public int CurrentStepIndex => currentStepIndex;
    private int currentStepIndex = 0;

    private bool stateCompleted = false;

    public int TotalSteps => sequence != null ? sequence.steps.Count : 0;
    public string CurrentStateName => sequence != null && currentStepIndex < sequence.steps.Count ? sequence.steps[currentStepIndex].step.GetStateName() : "None";

    void Awake()
    {
        InitializeStates();
    }

    void InitializeStates()
    {
        states = new Dictionary<string, IState>();

        states.Add("Wait", new WaitState());
        states.Add("DisplayText", new DisplayTextState());
        states.Add("LoadScene", new LoadSceneState());
        states.Add("LoadConfig", new LoadConfigState());
        states.Add("DisplayLikertScale", new DisplayLikertScaleState());
        states.Add("Break", new BreakState());
        states.Add("DisplayImage", new DisplayImageState());
        states.Add("DisplayQuestion", new DisplayQuestionState());
        states.Add("PlaySound", new PlaySoundState());
        states.Add("DisplayCamera", new DisplayCameraState());
        states.Add("DisplayVideo", new DisplayVideoState());
    }

    /// <summary>
    /// Charge et initialise une s�quence depuis un fichier YAML par son nom
    /// </summary>
    public void LoadSequenceByName(string sequenceName)
    {
        Sequence loadedSequence = SequencesManager.Instance.GetSequence(sequenceName);

        if (loadedSequence != null)
        {
            SetSequence(loadedSequence);
            currentSequenceName = sequenceName;
            Debug.Log($"Loaded sequence: {sequenceName}");
        }
        else
        {
            Debug.LogError($"Failed to load sequence: {sequenceName}");
        }
    }

    /// <summary>
    /// Recharge la s�quence actuelle depuis le fichier YAML
    /// </summary>
    public void ReloadCurrentSequence()
    {
        if (!string.IsNullOrEmpty(currentSequenceName))
        {
            bool wasPlaying = isPlaying;
            int savedIndex = currentStepIndex;

            Stop(false);
            LoadSequenceByName(currentSequenceName);

            currentStepIndex = savedIndex;

            if (wasPlaying)
            {
                Play(false);
            }
        }
    }


    public void Play(bool resetCurrentIndex = false)
    {
        if (sequence == null || sequence.steps.Count == 0)
        {
            Debug.LogWarning("No Sequence define");
            return;
        }

        isPlaying = true;
        isPaused = false;

        if (resetCurrentIndex)
        {
            currentStepIndex = 0;
        }

        if (currentSequenceCoroutine == null)
        {
            currentSequenceCoroutine = StartCoroutine(ExecuteSequence());
        }
    }

    public void Pause()
    {
        if (!isPlaying) return;

        isPaused = true;
        isPlaying = false;

        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
        }

        if (currentState != null)
        {
            currentState.Exit();
            currentState = null;
        }

        stateCompleted = false;
    }

    public void Stop(bool resetCurrentIndex = false)
    {
        isPlaying = false;
        isPaused = false;

        if (currentSequenceCoroutine != null)
        {
            StopCoroutine(currentSequenceCoroutine);
            currentSequenceCoroutine = null;
        }

        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
        }

        if (currentState != null)
        {
            currentState.Exit();
            currentState = null;
        }

        if (resetCurrentIndex)
        {
            currentStepIndex = 0;
        }

        stateCompleted = false;
    }

    public void NextStep()
    {
        if (sequence == null || sequence.steps.Count == 0)
            return;

        bool wasPlaying = isPlaying;
        Stop(false);

        currentStepIndex = Mathf.Min(currentStepIndex + 1, sequence.steps.Count - 1);

        if (wasPlaying)
        {
            Play(false);
        }
    }

    public void PreviousStep()
    {
        if (sequence == null || sequence.steps.Count == 0)
            return;

        bool wasPlaying = isPlaying;
        Stop(false);

        currentStepIndex = Mathf.Max(currentStepIndex - 1, 0);

        if (wasPlaying)
        {
            Play(false);
        }
    }

    public void GoToStep(int stepIndex)
    {
        if (sequence == null || sequence.steps.Count == 0)
            return;

        bool wasPlaying = isPlaying;
        Stop(false);

        currentStepIndex = Mathf.Clamp(stepIndex, 0, sequence.steps.Count - 1);

        if (wasPlaying)
        {
            Play(false);
        }
    }

    private IEnumerator ExecuteSequence()
    {
        while (currentStepIndex < sequence.steps.Count)
        {
            yield return new WaitUntil(() => !isPaused);

            if (!isPlaying && !isPaused) break;

            yield return StartCoroutine(ExecuteCurrentStep());

            if (!isPlaying && !isPaused) break;

            if (isPaused) continue;

            currentStepIndex++;
        }

        if ((!isPaused && isPlaying) && currentStepIndex >= sequence.steps.Count)
        {
            isPlaying = false;
            currentSequenceCoroutine = null;
            OnSequenceComplete?.Invoke();
        }
        else if (!isPaused && !isPlaying)
        {

            currentSequenceCoroutine = null;
        }
    }

    private IEnumerator ExecuteCurrentStep()
    {
        if (currentStepIndex >= sequence.steps.Count) yield break;

        SequenceStep currentStep = sequence.steps[currentStepIndex].step;
        var stateName = currentStep.GetStateName();

        if (states.ContainsKey(stateName))
        {
            currentState = states[stateName];
            currentState.Enter(currentStep);

            stateCompleted = false;
            currentStateCoroutine = StartCoroutine(ExecuteStateWithCallback(currentState));

            yield return new WaitUntil(() => stateCompleted || isPaused || (!isPlaying && !isPaused));

            if (stateCompleted && currentState != null)
            {
                currentState.Exit();
                currentState = null;
            }

            currentStateCoroutine = null;
        }
        else
        {
            Debug.LogWarning($"State '{stateName}' not found");
        }
    }

    private IEnumerator ExecuteStateWithCallback(IState state)
    {
        IEnumerator stateEnumerator = state.Execute();

        while (stateEnumerator.MoveNext())
        {
            if (isPaused || !isPlaying)
            {
                yield break;
            }

            yield return stateEnumerator.Current;
        }

        stateCompleted = true;
    }

    public void SetSequence(Sequence sequence)
    {
        this.sequence = sequence;
    }
}