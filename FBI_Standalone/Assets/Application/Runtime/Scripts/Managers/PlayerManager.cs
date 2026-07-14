using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Avatar")]
    [SerializeField] private GameObject avatar;

    [Header("Controller")]
    [SerializeField] private GameObject leftController;
    [SerializeField] private GameObject rightController;

    private static PlayerManager instance;
    public static PlayerManager Instance { get { return instance; } }

    private Vector3 lastPosition;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); } else { instance = this; }
    }

    private void Start()
    {
        DisplayAvatar(false);
        DisplayControllers(false);
    }

    private void LateUpdate()
    {
 
        /*Vector3 delta = transform.position - lastPosition;

        if (delta != Vector3.zero && WorldUIManager.Instance != null)
        {
            WorldUIManager.Instance.Position += delta;
        }

        lastPosition = transform.position;*/
    }



    public void DisplayAvatar(bool isDisplay)
    {
        avatar.SetActive(isDisplay);
    }

    public void DisplayControllers(bool isDisplay)
    {
        leftController.SetActive(isDisplay);
        rightController.SetActive(isDisplay);
    }
}
