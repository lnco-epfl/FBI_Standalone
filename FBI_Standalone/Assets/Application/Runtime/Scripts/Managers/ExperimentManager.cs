using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExperimentManager : MonoBehaviour
{

    public Action<bool, Sequence> OnInitialized;
    public Action OnStarted;
    public Action<bool> OnPause;
    public Action OnStop;

    public bool IsInitialized => isInitialized;
    private bool isInitialized = false;

    public bool IsRunning => isRunning;
    private bool isRunning = false;

    public DateTime StartDate => startDate;
    private DateTime startDate;

    public float StartTime => startTime;
    private float startTime;

    public double ElaspedTimeSinceStart => elaspedTimeSinceStart;
    private double elaspedTimeSinceStart;

    private SequenceStateMachine sequenceStateMachine;

    public int SequenceCurrentStep => sequenceStateMachine.CurrentStepIndex;
    public int SequenceTotalSetps => sequenceStateMachine.TotalSteps;

    private static ExperimentManager instance;
    public static ExperimentManager Instance => instance;



    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }

        sequenceStateMachine = GetComponent<SequenceStateMachine>();
    }

    private void OnEnable()
    {
        ShortcutManager.Instance.StartActionReference.action.performed += OnStartActionPerformed;
        ShortcutManager.Instance.StopActionReference.action.performed += OnStopActionPerformed;
        ShortcutManager.Instance.PauseActionReference.action.performed += OnPauseActionPerformed;
        ShortcutManager.Instance.NextStepActionReference.action.performed += OnNextStepActionPerformed;
        ShortcutManager.Instance.PreviousStepActionReference.action.performed += OnPreviousStepActionPerformed;

        sequenceStateMachine.OnSequenceComplete += OnStateMachineSequenceComplete;
    }


    private void OnDisable()
    {
        ShortcutManager.Instance.StartActionReference.action.performed -= OnStartActionPerformed;
        ShortcutManager.Instance.StopActionReference.action.performed -= OnStopActionPerformed;
        ShortcutManager.Instance.PauseActionReference.action.performed -= OnPauseActionPerformed;
        ShortcutManager.Instance.NextStepActionReference.action.performed -= OnNextStepActionPerformed;
        ShortcutManager.Instance.PreviousStepActionReference.action.performed -= OnPreviousStepActionPerformed;

        sequenceStateMachine.OnSequenceComplete -= OnStateMachineSequenceComplete;
    }

    void Start()
    {
        EventFileManager.Instance.Initialize();
    }

    void Update()
    {
        if (isRunning)
        {
            elaspedTimeSinceStart += Time.deltaTime;
        }
    }

    public void InitializeExperiment(Sequence sequence)
    {
        if (sequence != null)
        {
            sequenceStateMachine.SetSequence(sequence);

            isInitialized = true;

            EventFileManager.Log($"ExperimentManager Initialize with sequence {sequence.name}");
        }
        else
        {

            sequenceStateMachine.SetSequence(null);
            EventFileManager.Error($"ExperimentManager Initialize error with null sequence");
            isInitialized = false;
        }

        OnInitialized?.Invoke(isInitialized, sequence);
    }

    public void StartExperiment()
    {

        startTime = Time.time;
        startDate = DateTime.Now;

        elaspedTimeSinceStart = 0;

        isRunning = true;

        sequenceStateMachine.Play(true);

        EventFileManager.Log($"ExperimentManager Start experiment");

        OnStarted?.Invoke();
    }
    public void PauseExperiment(bool isPause)
    {
        if (isPause)
        {
            sequenceStateMachine.Pause();

            isRunning = false;

        }
        else
        {
            sequenceStateMachine.Play();

            isRunning = true;
        }

        EventFileManager.Log($"ExperimentManager Pause experiment {isPause}");

        OnPause?.Invoke(isPause);
    }

    public void StopExperiment()
    {
        sequenceStateMachine.Stop(true);

        isRunning = false;

        EventFileManager.Log($"ExperimentManager Stop experiment");

        OnStop?.Invoke();

    }

    public void NextStep(int index)
    {
        if (index > 0)
        {
            sequenceStateMachine.NextStep();
        }
        else if (index < 0)
        {
            sequenceStateMachine.PreviousStep();
        }
    }

    public void GoToStep(int index)
    {
        sequenceStateMachine.GoToStep(index);
    }
    private void OnStateMachineSequenceComplete()
    {
        StopExperiment();
    }

    private void OnStartActionPerformed(InputAction.CallbackContext context)
    {
        if (!isInitialized)
        {
            //Should be disable in prob
            InitializeExperiment(SequencesManager.Instance.Sequences[0]);
        }
        StartExperiment();
    }

    private void OnStopActionPerformed(InputAction.CallbackContext context)
    {
        StopExperiment();
    }
    private void OnPauseActionPerformed(InputAction.CallbackContext context)
    {
        PauseExperiment(isRunning ? true : false);
    }
    private void OnNextStepActionPerformed(InputAction.CallbackContext context)
    {
        NextStep(1);
    }
    private void OnPreviousStepActionPerformed(InputAction.CallbackContext context)
    {
        NextStep(-1);
    }




}
