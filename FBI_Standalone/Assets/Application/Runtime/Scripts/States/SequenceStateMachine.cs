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
    private List<Coroutine> activeStepCoroutines = new List<Coroutine>();
    private Dictionary<int, IState> activeStates = new Dictionary<int, IState>();

    public Action OnSequenceComplete;

    public bool IsPlaying => isPlaying;
    private bool isPlaying = false;
    public bool IsPaused => isPaused;
    private bool isPaused = false;

    private float sequenceTime = 0f;
    private float pausedAt = 0f;
    private HashSet<int> launchedSteps = new HashSet<int>();

    // Pour compatibilité UI — dernier step lancé
    public int CurrentStepIndex => currentStepIndex;
    private int currentStepIndex = 0;
    public int TotalSteps => sequence != null ? sequence.steps.Count : 0;
    public string CurrentStateName => sequence != null && currentStepIndex < sequence.steps.Count
        ? sequence.steps[currentStepIndex].step.GetStateName() : "None";

    void Awake() => InitializeStates();

    void InitializeStates()
    {
        states = new Dictionary<string, IState>
        {
            { "Wait",               new WaitState() },
            { "DisplayText",        new DisplayTextState() },
            { "LoadScene",          new LoadSceneState() },
            { "LoadConfig",         new LoadConfigState() },
            { "DisplayLikertScale", new DisplayLikertScaleState() },
            { "Break",              new BreakState() },
            { "DisplayImage",       new DisplayImageState() },
            { "DisplayQuestion",    new DisplayQuestionState() },
            { "PlaySound",          new PlaySoundState() },
            { "DisplayCameras",     new DisplayCamerasState() },
            { "DisplayVideo",       new DisplayVideoState() },
        };
    }

    // ─── Chargement ────────────────────────────────────────────────────────────

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

    // ─── Contrôles ─────────────────────────────────────────────────────────────

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
        pausedAt = sequenceTime;

        // Stopper toutes les coroutines actives
        foreach (var c in activeStepCoroutines)
            if (c != null) StopCoroutine(c);
        activeStepCoroutines.Clear();

        // Exit les states et les retirer de launchedSteps
        // pour qu'ils soient relancés depuis le début au Resume
        foreach (var kvp in activeStates)
        {
            kvp.Value.Exit();
            launchedSteps.Remove(kvp.Key);
        }
        activeStates.Clear();

        if (currentSequenceCoroutine != null)
        {
            StopCoroutine(currentSequenceCoroutine);
            currentSequenceCoroutine = null;
        }
    }

    public void Resume()
    {
        if (!isPaused) return;

        isPlaying = true;
        isPaused = false;
        // sequenceTime reste à pausedAt — les steps actifs seront relancés depuis leur début

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

    // ─── Timeline ──────────────────────────────────────────────────────────────

    private IEnumerator RunTimeline()
    {
        float totalDuration = GetTotalDuration();

        while (isPlaying)
        {
            // Lancer tous les steps dont le startTime est atteint
            for (int i = 0; i < sequence.steps.Count; i++)
            {
                if (launchedSteps.Contains(i)) continue;

                SequenceStep step = sequence.steps[i].step;
                if (sequenceTime >= step.startTime)
                {
                    launchedSteps.Add(i);
                    currentStepIndex = i;
                    Coroutine c = StartCoroutine(RunStep(i, step));
                    activeStepCoroutines.Add(c);
                }
            }

            // Fin : tous lancés et tous terminés
            if (launchedSteps.Count >= sequence.steps.Count && activeStates.Count == 0)
                break;

            sequenceTime += Time.deltaTime;
            yield return null;
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
        if (!states.TryGetValue(stepData.GetStateName(), out IState state))
        {
            Debug.LogWarning($"State '{stepData.GetStateName()}' not found");
            yield break;
        }

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
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

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

    /// Retourne les indices des steps actifs à un instant donné
    public List<int> GetActiveStepIndicesAt(float time)
    {
        var result = new List<int>();
        for (int i = 0; i < sequence.steps.Count; i++)
        {
            var step = sequence.steps[i].step;
            float end = step.startTime + step.GetDuration();
            if (step.startTime <= time && time < end)
                result.Add(i);
        }
        return result;
    }

#if UNITY_EDITOR
    [ContextMenu("Play")] public void EditorPlay() => Play(true);
    [ContextMenu("Pause")] public void EditorPause() => Pause();
    [ContextMenu("Resume")] public void EditorResume() => Resume();
    [ContextMenu("Stop")] public void EditorStop() => Stop(true);
#endif
}