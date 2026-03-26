using System;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{

    private Animator animator;
    private Toggle toggle;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDisable()
    {
        toggle.onValueChanged.RemoveListener(OnValueChanged);
    }

    public void OnValueChanged(bool value)
    {
        animator.SetBool("IsOn", value);
    }

    
}
