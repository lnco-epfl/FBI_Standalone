using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    [SerializeField] private GameObject avatar;

    private static PlayerManager instance;
    public static PlayerManager Instance { get { return instance; } }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); } else { instance = this; }
    }

    private void Start()
    {
        DisplayAvatar(false);
    }

    public void DisplayAvatar(bool isDisplay)
    {
        avatar.SetActive(isDisplay);
    }
}
