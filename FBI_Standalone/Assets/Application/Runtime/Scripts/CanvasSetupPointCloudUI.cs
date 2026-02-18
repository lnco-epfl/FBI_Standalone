using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasSetupPointCloudUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text title;

    public event Action<CanvasSetupPointCloudUI> OnCanvasSetupPointCloudUIDestroy;

    private void OnEnable()
    {
        closeButton.onClick.AddListener(OnButtonCloseClick);
    }

    private void OnDisable()
    {
        closeButton.onClick.RemoveListener(OnButtonCloseClick);
    }

    private void OnButtonCloseClick()
    {
        OnCanvasSetupPointCloudUIDestroy.Invoke(this);
    }

    private void OnDestroy()
    {
        
    }

    private void Start()
    {
        
    }


}
