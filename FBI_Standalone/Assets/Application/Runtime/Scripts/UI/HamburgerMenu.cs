using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Generic hamburger dropdown menu.
/// Attach to the hamburger button GameObject.
/// Assign the menuPanel in the Inspector — it will be shown/hidden on click.
/// The panel closes automatically when clicking outside.
/// </summary>
public class HamburgerMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button hamburgerButton;
    [SerializeField] private GameObject menuPanel;

    [Header("Menu Items")]
    [SerializeField] private Button copyConfigButton;
    [SerializeField] private Button pasteConfigButton;

    public event Action OnCopyConfigClicked;
    public event Action OnPasteConfigClicked;

    private bool isOpen = false;

    private void Awake()
    {
        menuPanel.SetActive(false);
    }

    private void OnEnable()
    {
        hamburgerButton.onClick.AddListener(ToggleMenu);
        copyConfigButton.onClick.AddListener(OnCopyClicked);
        pasteConfigButton.onClick.AddListener(OnPasteClicked);
    }

    private void OnDisable()
    {
        hamburgerButton.onClick.RemoveListener(ToggleMenu);
        copyConfigButton.onClick.RemoveListener(OnCopyClicked);
        pasteConfigButton.onClick.RemoveListener(OnPasteClicked);
        CloseMenu();
    }

    private void Update()
    {
        // Close when clicking anywhere outside the panel
        if (isOpen && Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                menuPanel.GetComponent<RectTransform>(),
                Input.mousePosition,
                null))
            {
                CloseMenu();
            }
        }
    }

    private void ToggleMenu()
    {
        if (isOpen) CloseMenu();
        else OpenMenu();
    }

    private void OpenMenu()
    {
        isOpen = true;
        menuPanel.SetActive(true);
    }

    private void CloseMenu()
    {
        isOpen = false;
        menuPanel.SetActive(false);
    }

    private void OnCopyClicked()
    {
        CloseMenu();
        OnCopyConfigClicked?.Invoke();
    }

    private void OnPasteClicked()
    {
        CloseMenu();
        OnPasteConfigClicked?.Invoke();
    }
}
