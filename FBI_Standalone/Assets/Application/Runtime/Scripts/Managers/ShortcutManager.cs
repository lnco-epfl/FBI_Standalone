using UnityEngine;
using UnityEngine.InputSystem;

public class ShortcutManager : MonoBehaviour
{

    [Header("Input Action")]
    public InputActionReference StartActionReference;
    public InputActionReference StopActionReference;
    public InputActionReference PauseActionReference;
    public InputActionReference NextStepActionReference;
    public InputActionReference PreviousStepActionReference;
    public InputActionReference MuteActionReference;
    public InputActionReference ResetXROriginActionReference;
    public InputActionReference EnablePasstroughAvatarActionReference;

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
        EnableShortCut();
    }

    public void EnableShortCut()
    {
        StartActionReference.action.Enable();
        StopActionReference.action.Enable();
        PauseActionReference.action.Enable();
        NextStepActionReference.action.Enable();
        PreviousStepActionReference.action.Enable();
        MuteActionReference.action.Enable();
        ResetXROriginActionReference.action.Enable();
        EnablePasstroughAvatarActionReference.action.Enable();
    }

    public void DisableShortCut()
    {
        StartActionReference.action.Disable();
        StopActionReference.action.Disable();
        PauseActionReference.action.Disable();
        NextStepActionReference.action.Disable();
        PreviousStepActionReference.action.Disable();
        MuteActionReference.action.Disable();
        ResetXROriginActionReference.action.Disable();
        EnablePasstroughAvatarActionReference.action.Disable();
    }

}
