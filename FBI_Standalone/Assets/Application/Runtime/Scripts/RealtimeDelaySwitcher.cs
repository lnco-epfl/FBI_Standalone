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

    [Tooltip("Mode d'affichage du point cloud")]
    public DisplayMode displayMode = DisplayMode.Realtime;

    private VisualEffect visualEffect;

    private void Awake()
    {
        visualEffect = GetComponent<VisualEffect>();
    }

    private void Start()
    {
        SetTexture(ColorMain, VertexMain);
    }

    private void Update()
    {
        switch (displayMode)
        {
            case DisplayMode.Realtime:
                SetTexture(ColorMain, VertexMain);
                break;
            case DisplayMode.Delay:
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
