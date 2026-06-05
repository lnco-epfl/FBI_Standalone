using PrimeTween;
using System;
using UnityEngine;

public class Fader : MonoBehaviour
{

    public float FadeDuration => fadeDuration;
    [SerializeField] private float fadeDuration = 0.1f;

    public event Action<bool> OnFadeCompleted;

    private Material material;
    private bool isInitialize = false;
    private static Fader instance;

    private bool isBlack = true;

    public static Fader Instance { get { return instance; } }

    public bool IsInitialize { get => isInitialize; }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); }
        else
        {
            instance = this;
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        material = meshRenderer.material;

        isInitialize = true;
    }

    [ContextMenu("FadeToBlack")]
    public void FadeToBlack()
    {

        if (isBlack)
            return;

        Tween.MaterialAlpha(material, 1, FadeDuration).OnComplete(() => OnFadeComplete(true));
    }

    [ContextMenu("FadeToClear")]
    public void FadeToClear()
    {
        if (!isBlack)
            return;

        Tween.MaterialAlpha(material, 0, FadeDuration).OnComplete(() => OnFadeComplete(false)); ;
    }

    private void OnFadeComplete(bool isBlack)
    {
        OnFadeCompleted?.Invoke(isBlack);


        this.isBlack = isBlack;
    }
}
