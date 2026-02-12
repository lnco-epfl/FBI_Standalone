using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class RealtimeDelaySwitcher : MonoBehaviour
{
    [SerializeField] private Texture2D ColorMain;
    [SerializeField] private Texture2D VertexMain;

    [SerializeField] private Texture2D ColorDelay;
    [SerializeField] private Texture2D VertexDelay;

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



    private void SetTexture(Texture2D colorTexture, Texture2D vertexTexture)
    {
        visualEffect.SetTexture("Color", colorTexture);
        visualEffect.SetTexture("Vertex", vertexTexture);
    }
}
