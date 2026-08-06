using UnityEngine;
using UnityEngine.InputSystem;

public class ShortcutManager : MonoBehaviour
{

    [Header("Main Input Action")]
    public InputActionReference StartActionReference;
    public InputActionReference StopActionReference;
    public InputActionReference PauseActionReference;
    public InputActionReference NextStepActionReference;
    public InputActionReference PreviousStepActionReference;
    public InputActionReference MuteActionReference;
    public InputActionReference ResetXROriginActionReference;

    [Header("Config Input Action")]
    public InputActionReference ConfigResetXROriginActionReference;
    public InputActionReference ConfigStartDissolutionActionReference;

    private static ShortcutManager instance;
    public static ShortcutManager Instance { get { return instance; } }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); }
        else
        {
            instance = this;
        }
    }

    private void Start()
    {
        EnableMainShortCut();
        DisableConfigShortCut();
    }

    public void EnableMainShortCut()
    {
        StartActionReference.action.Enable();
        StopActionReference.action.Enable();
        PauseActionReference.action.Enable();
        NextStepActionReference.action.Enable();
        PreviousStepActionReference.action.Enable();
        MuteActionReference.action.Enable();
        ResetXROriginActionReference.action.Enable();
    }

    public void DisableMainShortCut()
    {
        StartActionReference.action.Disable();
        StopActionReference.action.Disable();
        PauseActionReference.action.Disable();
        NextStepActionReference.action.Disable();
        PreviousStepActionReference.action.Disable();
        MuteActionReference.action.Disable();
        ResetXROriginActionReference.action.Disable();
    }

    public void EnableConfigShortCut()
    {
        ConfigResetXROriginActionReference.action.Enable();
        ConfigStartDissolutionActionReference.action.Enable();
    }

    public void DisableConfigShortCut()
    {
        ConfigResetXROriginActionReference.action.Disable();
        ConfigStartDissolutionActionReference.action.Disable();
    }

}
