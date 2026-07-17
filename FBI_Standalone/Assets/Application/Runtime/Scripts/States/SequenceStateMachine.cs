using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SequenceStateMachine : MonoBehaviour
{
    private Sequence sequence;
    private string currentSequenceName;

    private Dictionary<string, Func<IState>> stateFactories;
    private Coroutine currentSequenceCoroutine;
    private List<Coroutine> activeStepCoroutines = new List<Coroutine>();
    private Dictionary<int, IState> activeStates = new Dictionary<int, IState>();

    public Action OnSequenceComplete;

    public bool IsPlaying => isPlaying;
    private bool isPlaying = false;
    public bool IsPaused => isPaused;
    private bool isPaused = false;

    private float sequenceTime = 0f;
    private HashSet<int> launchedSteps = new HashSet<int>();

    // Pour compatibilité UI — dernier step lancé
    public int CurrentStepIndex => currentStepIndex;
    private int currentStepIndex = 0;
    public int TotalSteps => sequence != null ? sequence.steps.Count : 0;
    public float SequenceTime => sequenceTime;
    public float TotalDuration => sequence != null ? GetTotalDuration() : 0f;
    public string CurrentStateName => sequence != null && currentStepIndex < sequence.steps.Count
        ? sequence.steps[currentStepIndex].step.GetStateName() : "None";

    void Awake() => InitializeStates();

    void InitializeStates()
    {
        stateFactories = new Dictionary<string, Func<IState>>
        {
            { "Wait",               () => new WaitState() },
            { "DisplayText",        () => new DisplayTextState() },
            { "LoadScene",          () => new LoadSceneState() },
            { "LoadConfig",         () => new LoadConfigState() },
            { "DisplayLikertScale", () => new DisplayLikertScaleState() },
            { "Break",              () => new BreakState() },
            { "DisplayImage",       () => new DisplayImageState() },
            { "DisplayQuestion",    () => new DisplayQuestionState() },
            { "PlaySound",          () => new PlaySoundState() },
            { "DisplayCameras",     () => new DisplayCamerasState() },
            { "DisplayVideo",       () => new DisplayVideoState() },
            { "SendLSLEvent",       () => new SendLSLEventState() },
            { "LoadDisplayConfig",  () => new LoadDisplayConfigState() }
        };
    }

    public void LoadSequenceByName(string sequenceName)
    {
        Sequence loaded = SequencesManager.Instance.GetSequence(sequenceName);
        if (loaded != null)
        {
            SetSequence(loaded);
            currentSequenceName = sequenceName;
        }
        else
        {
            Debug.LogError($"Failed to load sequence: {sequenceName}");
        }
    }

    public void SetSequence(Sequence seq)
    {
        sequence = seq;
        sequence.steps.Sort((a, b) => a.step.startTime.CompareTo(b.step.startTime));
    }

    public void ReloadCurrentSequence()
    {
        if (string.IsNullOrEmpty(currentSequenceName)) return;

        bool wasPlaying = isPlaying;
        Stop(true);
        SequencesManager.Instance.ReloadSequences();
        LoadSequenceByName(currentSequenceName);

        if (wasPlaying) Play(true);
    }

    public void Play(bool resetTime = true)
    {
        if (sequence == null || sequence.steps.Count == 0)
        {
            Debug.LogWarning("No sequence defined");
            return;
        }

        if (resetTime)
        {
            sequenceTime = 0f;
            launchedSteps.Clear();
            currentStepIndex = 0;
        }

        isPlaying = true;
        isPaused = false;

        if (currentSequenceCoroutine == null)
            currentSequenceCoroutine = StartCoroutine(RunTimeline());
    }

    public void Pause()
    {
        if (!isPlaying) return;

        isPaused = true;
        isPlaying = false;

        if (currentSequenceCoroutine != null)
        {
            StopCoroutine(currentSequenceCoroutine);
            currentSequenceCoroutine = null;
        }

        foreach (var c in activeStepCoroutines)
            if (c != null) StopCoroutine(c);
        activeStepCoroutines.Clear();

        foreach (var kvp in activeStates)
        {
            kvp.Value.Exit();
            launchedSteps.Remove(kvp.Key);
        }
        activeStates.Clear();
    }

    public void Resume()
    {
        if (!isPaused) return;

        isPlaying = true;
        isPaused = false;

        if (currentSequenceCoroutine == null)
            currentSequenceCoroutine = StartCoroutine(RunTimeline());
    }

    public void Stop(bool resetTime = true)
    {
        isPlaying = false;
        isPaused = false;

        if (currentSequenceCoroutine != null)
        {
            StopCoroutine(currentSequenceCoroutine);
            currentSequenceCoroutine = null;
        }

        foreach (var c in activeStepCoroutines)
            if (c != null) StopCoroutine(c);
        activeStepCoroutines.Clear();

        foreach (var state in activeStates.Values)
            state.Exit();
        activeStates.Clear();

        if (resetTime)
        {
            sequenceTime = 0f;
            launchedSteps.Clear();
            currentStepIndex = 0;
        }
    }

    public void SeekTo(float time)
    {
        if (sequence == null) return;

        bool wasPlaying = isPlaying;
        Stop(false);

        sequenceTime = time;

        launchedSteps.Clear();
        for (int i = 0; i < sequence.steps.Count; i++)
        {
            SequenceStep step = sequence.steps[i].step;

            if (step.startTime <= time)
                launchedSteps.Add(i);
        }

        currentStepIndex = launchedSteps.Count > 0 ? launchedSteps.Max() : 0;

        if (wasPlaying)
            Play(false);
    }


    private IEnumerator RunTimeline()
    {
        while (isPlaying)
        {
            for (int i = 0; i < sequence.steps.Count; i++)
            {
                if (launchedSteps.Contains(i)) continue;

                SequenceStep step = sequence.steps[i].step;
                if (sequenceTime >= step.startTime)
                {
                    launchedSteps.Add(i);
                    currentStepIndex = i;
                    activeStepCoroutines.Add(StartCoroutine(RunStep(i, step)));
                }
            }

            bool hasBlocking = activeStates.Keys.Any(i => sequence.steps[i].step.blocking);
            if (!hasBlocking)
                sequenceTime += Time.deltaTime;

            yield return null;

            if (launchedSteps.Count >= sequence.steps.Count && activeStates.Count == 0)
                break;
        }

        if (isPlaying)
        {
            isPlaying = false;
            currentSequenceCoroutine = null;
            OnSequenceComplete?.Invoke();
        }
        else
        {
            currentSequenceCoroutine = null;
        }
    }

    private IEnumerator RunStep(int index, SequenceStep stepData)
    {
        if (!stateFactories.TryGetValue(stepData.GetStateName(), out Func<IState> factory))
        {
            Debug.LogWarning($"State '{stepData.GetStateName()}' not found");
            yield break;
        }

        IState state = factory();
        activeStates[index] = state;
        state.Enter(stepData);

        IEnumerator exec = state.Execute();
        while (exec.MoveNext())
        {
            if (!isPlaying) yield break;
            yield return exec.Current;
        }

        state.Exit();
        activeStates.Remove(index);

        if (stepData.blocking)
        {
            int next = index + 1;
            if (next < sequence.steps.Count)
                sequenceTime = sequence.steps[next].step.startTime;
        }
    }

    private float GetTotalDuration()
    {
        float max = 0f;
        foreach (var wrapper in sequence.steps)
        {
            float end = wrapper.step.startTime + wrapper.step.GetDuration();
            if (end > max) max = end;
        }
        return max;
    }

    public List<int> GetActiveStepIndices()
    {
        return activeStates.Keys.ToList();
    }

#if UNITY_EDITOR
    [ContextMenu("Play")] public void EditorPlay() => Play(true);
    [ContextMenu("Pause")] public void EditorPause() => Pause();
    [ContextMenu("Resume")] public void EditorResume() => Resume();
    [ContextMenu("Stop")] public void EditorStop() => Stop(true);
    [ContextMenu("Seek To 30s")] public void EditorSeekTo30() => SeekTo(30f);
#endif
}