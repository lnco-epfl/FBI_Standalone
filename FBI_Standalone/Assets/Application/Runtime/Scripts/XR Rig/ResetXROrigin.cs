using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ResetXROrigin : MonoBehaviour
{

    [Header("Transform")]
    public Transform head;
    public Transform origin;
    public Transform target;


    private static ResetXROrigin instance;
    public static ResetXROrigin Instance { get { return instance; } }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); }
        else
        {
            instance = this;
        }
    }


    private void OnEnable()
    {
        ShortcutManager.Instance.ResetXROriginActionReference.action.performed += OnResetXROriginActionPerformed;

        SceneLoaderManager.Instance.OnSceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        ShortcutManager.Instance.ResetXROriginActionReference.action.performed -= OnResetXROriginActionPerformed;

        SceneLoaderManager.Instance.OnSceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        //ResetOrigin();
    }

    private void OnSceneLoaded(Scene scene)
    {
        ResetOrigin();
    }

    private void OnResetXROriginActionPerformed(InputAction.CallbackContext obj)
    {
        ResetOrigin();
    }

    [ContextMenu("ResetOrigin")]
    public void ResetOrigin()
    {
        EventFileManager.Log("[ResetXROrigin] Reset Origin");
        float offsetTargetAngle = target.rotation.eulerAngles.y - head.rotation.eulerAngles.y;
        origin.Rotate(Vector3.up, offsetTargetAngle);
        Vector3 offsetTargetMotion = target.position - head.position;
        origin.position += offsetTargetMotion;

    }


}
