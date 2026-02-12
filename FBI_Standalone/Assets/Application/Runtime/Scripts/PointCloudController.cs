using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;

public class PointCloudController : MonoBehaviour
{
    public enum DisplayMode
    {
        Realtime,
        Delay
    }

    [Header("Configuration")]
    [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
    public int sensorIndex = 0;

    [Tooltip("Mode d'affichage du point cloud")]
    public DisplayMode displayMode = DisplayMode.Realtime;

    [Tooltip("Délai de replay en secondes (ex: 2.0 pour 2 secondes de délai)")]
    [Range(0.1f, 10f)]
    public float replayDelaySeconds = 2.0f;

    [Tooltip("Nombre de frames par seconde à capturer (influence la taille du buffer)")]
    [Range(15, 60)]
    public int captureFrameRate = 30;

    [Tooltip("Activer/désactiver le système (capture toujours en arrière-plan)")]
    public bool enableSystem = true;

    [Header("Textures de sortie")]
    [Tooltip("Render texture pour les vertices du point cloud")]
    public RenderTexture outputVertexTexture = null;

    [Tooltip("Render texture pour les couleurs du point cloud")]
    public RenderTexture outputColorTexture = null;

    [Header("Debug")]
    [Tooltip("Afficher les informations de debug")]
    public bool showDebugInfo = false;

    private class PointCloudFrame
    {
        public Texture2D vertexTexture;
        public Texture2D colorTexture;
        public float timestamp;

        public PointCloudFrame(int width, int height)
        {
            vertexTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            colorTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            timestamp = -1f;
        }

        public void Release()
        {
            if (vertexTexture != null) Destroy(vertexTexture);
            if (colorTexture != null) Destroy(colorTexture);
        }
    }

    private List<PointCloudFrame> frameBuffer;
    private int bufferSize;
    private int writeIndex = 0;
    private int currentFrameCount = 0;

    private KinectManager kinectManager = null;
    private KinectInterop.SensorData sensorData = null;
    private DepthSensorBase sensorInt = null;

    private float lastCaptureTime = 0f;
    private float captureInterval = 0f;
    private int textureWidth = 0;
    private int textureHeight = 0;

    private Material copyMaterial;

    void Start()
    {
        kinectManager = KinectManager.Instance;

        if (kinectManager == null || !kinectManager.IsInitialized())
        {
            Debug.LogError("KinectManager not initialized!");
            enabled = false;
            return;
        }

        sensorData = kinectManager.GetSensorData(sensorIndex);

        if (sensorData == null || sensorData.sensorInterface == null)
        {
            Debug.LogError($"Sensor {sensorIndex} not available!");
            enabled = false;
            return;
        }

        sensorInt = (DepthSensorBase)sensorData.sensorInterface;

        InitializeBuffer();

        copyMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
    }

    void InitializeBuffer()
    {
        captureInterval = 1f / captureFrameRate;
        bufferSize = Mathf.CeilToInt(replayDelaySeconds * captureFrameRate) + 5;

        if (sensorInt.pointCloudResolution == DepthSensorBase.PointCloudResolution.ColorCameraResolution)
        {
            textureWidth = sensorData.colorImageWidth;
            textureHeight = sensorData.colorImageHeight;
        }
        else
        {
            textureWidth = sensorData.depthImageWidth;
            textureHeight = sensorData.depthImageHeight;
        }

        if (frameBuffer != null)
        {
            foreach (var frame in frameBuffer)
            {
                frame.Release();
            }
        }

        frameBuffer = new List<PointCloudFrame>(bufferSize);
        for (int i = 0; i < bufferSize; i++)
        {
            frameBuffer.Add(new PointCloudFrame(textureWidth, textureHeight));
        }

        writeIndex = 0;
        currentFrameCount = 0;

        if (showDebugInfo)
        {
            Debug.Log($"PointCloudController initialized: {bufferSize} frames, {textureWidth}x{textureHeight}, delay: {replayDelaySeconds}s");
        }

        if (outputVertexTexture == null)
        {
            outputVertexTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGBFloat);
            outputVertexTexture.name = "OutputVertexTexture";
            outputVertexTexture.Create();
        }

        if (outputColorTexture == null)
        {
            outputColorTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
            outputColorTexture.name = "OutputColorTexture";
            outputColorTexture.Create();
        }
    }

    void Update()
    {
        if (!enableSystem || sensorInt == null)
            return;

        float currentTime = Time.time;

        if (currentTime - lastCaptureTime >= captureInterval)
        {
            CaptureFrame();
            lastCaptureTime = currentTime;
        }

        switch (displayMode)
        {
            case DisplayMode.Realtime:
                DisplayRealtimeFrame();
                break;
            case DisplayMode.Delay:
                DisplayDelayedFrame();
                break;
        }
    }

    void CaptureFrame()
    {
        RenderTexture vertexRT = sensorInt.pointCloudVertexTexture;
        RenderTexture colorRT = sensorInt.pointCloudColorTexture;

        if (vertexRT == null || colorRT == null)
            return;

        PointCloudFrame frame = frameBuffer[writeIndex];
        frame.timestamp = Time.time;

        CopyRenderTextureToTexture2D(vertexRT, frame.vertexTexture);
        CopyRenderTextureToTexture2D(colorRT, frame.colorTexture);

        writeIndex = (writeIndex + 1) % bufferSize;
        if (currentFrameCount < bufferSize)
        {
            currentFrameCount++;
        }

        if (showDebugInfo && writeIndex == 0)
        {
            Debug.Log($"Buffer wrap-around at {Time.time:F2}s");
        }
    }

    void DisplayRealtimeFrame()
    {
        RenderTexture vertexRT = sensorInt.pointCloudVertexTexture;
        RenderTexture colorRT = sensorInt.pointCloudColorTexture;

        if (vertexRT == null || colorRT == null)
            return;

        Graphics.Blit(vertexRT, outputVertexTexture);
        Graphics.Blit(colorRT, outputColorTexture);
    }

    void DisplayDelayedFrame()
    {
        if (currentFrameCount == 0)
            return;

        float targetTime = Time.time - replayDelaySeconds;

        int bestIndex = -1;
        float bestTimeDiff = float.MaxValue;

        for (int i = 0; i < currentFrameCount; i++)
        {
            if (frameBuffer[i].timestamp < 0)
                continue;

            float timeDiff = Mathf.Abs(frameBuffer[i].timestamp - targetTime);
            if (timeDiff < bestTimeDiff)
            {
                bestTimeDiff = timeDiff;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            PointCloudFrame frame = frameBuffer[bestIndex];

            if (frame.vertexTexture != null && frame.colorTexture != null)
            {
                Graphics.Blit(frame.vertexTexture, outputVertexTexture);
                Graphics.Blit(frame.colorTexture, outputColorTexture);

                if (showDebugInfo && Random.value < 0.01f)
                {
                    Debug.Log($"Displaying frame {bestIndex}, timestamp: {frame.timestamp:F2}, target: {targetTime:F2}, diff: {bestTimeDiff:F3}s");
                }
            }
        }
    }

    void CopyRenderTextureToTexture2D(RenderTexture source, Texture2D destination)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = source;

        destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        destination.Apply();

        RenderTexture.active = previous;
    }

    void OnDestroy()
    {
        if (frameBuffer != null)
        {
            foreach (var frame in frameBuffer)
            {
                frame.Release();
            }
            frameBuffer.Clear();
        }

        if (copyMaterial != null)
        {
            Destroy(copyMaterial);
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box("Point Cloud Display System");
        GUILayout.Label($"Mode: {displayMode}");
        GUILayout.Label($"Delay: {replayDelaySeconds:F2}s");
        GUILayout.Label($"Buffer Size: {bufferSize} frames");
        GUILayout.Label($"Frames Captured: {currentFrameCount}");
        GUILayout.Label($"Write Index: {writeIndex}");
        GUILayout.Label($"FPS: {captureFrameRate}");
        GUILayout.Label($"Resolution: {textureWidth}x{textureHeight}");

        if (GUILayout.Button("Toggle Mode"))
        {
            ToggleDisplayMode();
        }
        GUILayout.EndArea();
    }

    public void SetDisplayMode(DisplayMode mode)
    {
        displayMode = mode;
        if (showDebugInfo)
        {
            Debug.Log($"Display mode changed to: {mode}");
        }
    }

    [ContextMenu("ToggleDisplayMode")]
    public void ToggleDisplayMode()
    {
        displayMode = (displayMode == DisplayMode.Realtime) ?
            DisplayMode.Delay : DisplayMode.Realtime;

        if (showDebugInfo)
        {
            Debug.Log($"Display mode toggled to: {displayMode}");
        }
    }

    public void SetReplayDelay(float delaySeconds)
    {
        replayDelaySeconds = Mathf.Clamp(delaySeconds, 0.1f, 10f);

        int newBufferSize = Mathf.CeilToInt(replayDelaySeconds * captureFrameRate) + 5;
        if (newBufferSize != bufferSize)
        {
            InitializeBuffer();
        }
    }

    public void ToggleSystem(bool enabled)
    {
        enableSystem = enabled;
    }

    public void ClearBuffer()
    {
        writeIndex = 0;
        currentFrameCount = 0;

        foreach (var frame in frameBuffer)
        {
            frame.timestamp = -1f;
        }
    }

    public DisplayMode GetCurrentMode()
    {
        return displayMode;
    }

    public bool IsRealtime()
    {
        return displayMode == DisplayMode.Realtime;
    }

    public bool IsDelay()
    {
        return displayMode == DisplayMode.Delay;
    }

    public int GetCurrentFrameCount()
    {
        return currentFrameCount;
    }

    public float GetCurrentDelay()
    {
        return replayDelaySeconds;
    }
}