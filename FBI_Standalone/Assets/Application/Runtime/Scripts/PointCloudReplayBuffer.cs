using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;


public class PointCloudReplayBuffer : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
    public int sensorIndex = 0;

    [Tooltip("Replay delay in seconds (e.g. 2.0 for a 2-second delay)")]
    [Range(0.001f, 3f)]
    public float replayDelaySeconds = 1.0f;

    [Tooltip("Number of frames per second to capture (affects buffer size)")]
    [Range(15, 60)]
    public int captureFrameRate = 30;

    [Tooltip("Enable/disable replay mode")]
    public bool enableReplay = false;

    [Header("Output Textures")]
    [Tooltip("Render texture for replayed point cloud vertices")]
    public RenderTexture replayVertexTexture = null;

    [Tooltip("Render texture for replayed point cloud colors")]
    public RenderTexture replayColorTexture = null;

    [Header("Debug")]
    [Tooltip("Show debug information")]
    public bool showDebugInfo = false;

    private class PointCloudFrame
    {
        public Texture2D vertexTexture;
        public Texture2D colorTexture;
        public float timestamp;

        public PointCloudFrame(int width, int height)
        {
            vertexTexture = new Texture2D(width, height, TextureFormat.RGBAHalf, false, linear: true);
            colorTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear: true);
            timestamp = 0f;
        }

        public void Release()
        {
            if (vertexTexture != null) Destroy(vertexTexture);
            if (colorTexture != null) Destroy(colorTexture);
        }
    }

    // Circular buffer
    private List<PointCloudFrame> frameBuffer;
    private int bufferSize;
    private int writeIndex = 0;
    private int readIndex = 0;

    // Flag: safe to run Update logic
    private bool isBufferReady = false;

    // References
    private KinectManager kinectManager = null;
    private KinectInterop.SensorData sensorData = null;
    private DepthSensorBase sensorInt = null;

    // Timing
    private float lastCaptureTime = 0f;
    private float captureInterval = 0f;
    private int textureWidth = 0;
    private int textureHeight = 0;

    private void Awake()
    {
        //Initialize();
    }

    public void Initialize()
    {
        kinectManager = KinectManager.Instance;

        sensorData = kinectManager.GetSensorData(sensorIndex);

        if (sensorData != null || sensorData.sensorInterface != null)
        {

            sensorInt = (DepthSensorBase)sensorData.sensorInterface;

            if (sensorInt.pointCloudVertexTexture != null || sensorInt.pointCloudColorTexture != null || sensorInt.pointCloudVertexTexture.width > 0 || sensorInt.pointCloudVertexTexture.height > 0)
            {
                InitializeBuffer();
            }
        }
    }

    void Update()
    {
        // Do nothing until the buffer is fully initialized
        if (!isBufferReady || !enableReplay || sensorInt == null)
            return;

        float currentTime = Time.time;

        if (currentTime - lastCaptureTime >= captureInterval)
        {
            CaptureFrame();
            lastCaptureTime = currentTime;
        }

        DisplayDelayedFrame();
    }

    private void InitializeBuffer()
    {
        isBufferReady = false;

        captureInterval = 1f / captureFrameRate;
        bufferSize = Mathf.CeilToInt(replayDelaySeconds * captureFrameRate) + 2;

        // Resolve texture dimensions from the sensor
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

        // Critical guard — never allocate zero-dimension textures
        if (textureWidth <= 0 || textureHeight <= 0)
        {
            Debug.LogError($"[ReplayBuffer] Invalid texture dimensions ({textureWidth}x{textureHeight}) " +
                           $"for sensor {sensorIndex}. Buffer not initialized.");
            enabled = false;
            return;
        }

        // Allocate frame buffer
        frameBuffer = new List<PointCloudFrame>(bufferSize);
        for (int i = 0; i < bufferSize; i++)
            frameBuffer.Add(new PointCloudFrame(textureWidth, textureHeight));

        // Create output render textures if not already assigned in the Inspector
        if (replayVertexTexture == null)
        {
            replayVertexTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGBHalf);
            replayVertexTexture.name = "ReplayVertexTexture";
            replayVertexTexture.Create();
        }

        if (replayColorTexture == null)
        {
            replayColorTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
            replayColorTexture.name = "ReplayColorTexture";
            replayColorTexture.Create();
        }

        writeIndex = 0;
        readIndex = 0;
        isBufferReady = true;

        if (showDebugInfo)
            Debug.Log($"[ReplayBuffer] Initialized: {bufferSize} frames @ {textureWidth}x{textureHeight}, delay: {replayDelaySeconds}s");
    }


    private void CaptureFrame()
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

        if (showDebugInfo && writeIndex == 0)
            Debug.Log($"[ReplayBuffer] Buffer wrap-around at {Time.time:F2}s");
    }

    private void DisplayDelayedFrame()
    {
        float targetTime = Time.time - replayDelaySeconds;
        int bestIndex = -1;
        float bestTimeDiff = float.MaxValue;

        for (int i = 0; i < bufferSize; i++)
        {
            if (frameBuffer[i].timestamp <= 0f) continue;

            float diff = Mathf.Abs(frameBuffer[i].timestamp - targetTime);
            if (diff < bestTimeDiff)
            {
                bestTimeDiff = diff;
                bestIndex = i;
            }
        }

        if (bestIndex < 0) return;

        readIndex = bestIndex;
        PointCloudFrame frame = frameBuffer[readIndex];

        Graphics.Blit(frame.vertexTexture, replayVertexTexture);
        Graphics.Blit(frame.colorTexture, replayColorTexture);
    }

    private void CopyRenderTextureToTexture2D(RenderTexture source, Texture2D destination)
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
                frame.Release();
            frameBuffer.Clear();
        }
    }


    /// <summary>
    /// Changes the replay delay at runtime. Reinitializes the buffer if needed.
    /// </summary>
    public void SetReplayDelay(float delaySeconds)
    {
        replayDelaySeconds = Mathf.Clamp(delaySeconds, 0.1f, 10f);

        int newBufferSize = Mathf.CeilToInt(replayDelaySeconds * captureFrameRate) + 2;
        if (newBufferSize == bufferSize) return;

        if (frameBuffer != null)
        {
            foreach (var frame in frameBuffer)
                frame.Release();
        }

        InitializeBuffer();
    }

    /// <summary>
    /// Enables or disables replay mode.
    /// </summary>
    public void ToggleReplay(bool value)
    {
        enableReplay = value;
    }

    /// <summary>
    /// Resets all frame timestamps and read/write indices.
    /// </summary>
    public void ClearBuffer()
    {
        if (frameBuffer == null) return;

        writeIndex = 0;
        readIndex = 0;

        foreach (var frame in frameBuffer)
            frame.timestamp = 0f;
    }
}