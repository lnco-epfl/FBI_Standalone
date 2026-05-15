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

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); } else { instance = this; }
    }

    private void Start()
    {
        DisplayAvatar(false);
        DisplayControllers(false);
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
