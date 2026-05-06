using UnityEngine;
using UnityEngine.UI;
using System;

public class DestroyOnButtonClick : MonoBehaviour
{
    [SerializeField] private Button destroyButton;

    public event Action OnBeforeDestroy;

    private void Start()
    {
        if (destroyButton != null)
        {
            destroyButton.onClick.AddListener(DestroyThisObject);
        }
        else
        {
            Debug.LogWarning("DestroyOnButtonClick: Aucun bouton assigné sur " + gameObject.name);
        }
    }

    private void DestroyThisObject()
    {
        OnBeforeDestroy?.Invoke();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (destroyButton != null)
        {
            destroyButton.onClick.RemoveListener(DestroyThisObject);
        }
    }
}