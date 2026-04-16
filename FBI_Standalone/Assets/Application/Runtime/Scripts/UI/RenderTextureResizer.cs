using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class RenderTextureResizer : MonoBehaviour
{
    private RawImage rawImage;
    private RectTransform rectTransform;
    private RenderTexture renderTexture;
    private Vector2 lastSize;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        // Get the existing RenderTexture from RawImage
        renderTexture = rawImage.texture as RenderTexture;

        if (renderTexture != null)
        {
            ResizeRenderTexture();
        }
    }

    void Update()
    {
        if (renderTexture == null)
            return;

        Vector2 currentSize = rectTransform.rect.size;

        if (currentSize != lastSize && currentSize.x > 0 && currentSize.y > 0)
        {
            ResizeRenderTexture();
        }
    }

    void ResizeRenderTexture()
    {
        Vector2 size = rectTransform.rect.size;

        if (size.x <= 0 || size.y <= 0)
            return;

        // Account for Canvas Scaler
        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

        int width = Mathf.Max(1, Mathf.RoundToInt(size.x * scaleFactor));
        int height = Mathf.Max(1, Mathf.RoundToInt(size.y * scaleFactor));

        // Resize existing RenderTexture
        renderTexture.Release();
        renderTexture.width = width;
        renderTexture.height = height;
        renderTexture.Create();

        lastSize = size;
    }

    public void ForceResize()
    {
        ResizeRenderTexture();
    }
}