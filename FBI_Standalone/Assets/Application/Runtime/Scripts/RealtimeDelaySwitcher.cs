using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

public class RealtimeDelaySwitcher : MonoBehaviour
{
    [SerializeField] private RenderTexture ColorMain;
    [SerializeField] private RenderTexture VertexMain;

    [SerializeField] private RenderTexture ColorDelay;
    [SerializeField] private RenderTexture VertexDelay;

    public enum DisplayMode
    {
        Realtime,
        Delay
    }

    [Tooltip("Point cloud display mode")]
    [SerializeField] private DisplayMode _displayMode = DisplayMode.Realtime;
    public DisplayMode displayMode
    {
        get => _displayMode;
        set
        {
            if (_displayMode == value) return;
            _displayMode = value;
            ApplyDisplayMode();
        }
    }

    private VisualEffect visualEffect;

    private void Awake()
    {
        visualEffect = GetComponent<VisualEffect>();
    }

    private void Start()
    {
        ApplyDisplayMode();
    }

    private void ApplyDisplayMode()
    {
        switch (_displayMode)
        {
            case DisplayMode.Realtime:
                EventFileManager.Log($"[RealtimeDelaySwitcher] DisplayMode.Realtime");
                SetTexture(ColorMain, VertexMain);
                break;
            case DisplayMode.Delay:
                EventFileManager.Log($"[RealtimeDelaySwitcher] DisplayMode.Delay");
                SetTexture(ColorDelay, VertexDelay);
                break;
        }
    }

    private void SetTexture(RenderTexture colorTexture, RenderTexture vertexTexture)
    {
        visualEffect.SetTexture("Color", colorTexture);
        visualEffect.SetTexture("Vertex", vertexTexture);
    }
}