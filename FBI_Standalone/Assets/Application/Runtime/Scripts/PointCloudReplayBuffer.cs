using com.rfilkov.kinect;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
        public RenderTexture vertexTexture;
        public RenderTexture colorTexture;
        public float timestamp;

        public PointCloudFrame(int width, int height)
        {
            vertexTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf);
            vertexTexture.Create();

            colorTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            colorTexture.Create();

            timestamp = 0f;
        }

        public void Release()
        {
            if (vertexTexture != null)
            {
                vertexTexture.Release();
                Destroy(vertexTexture);
            }
            if (colorTexture != null)
            {
                colorTexture.Release();
                Destroy(colorTexture);
            }
        }
    }

    // Circular buffer
    private List<PointCloudFrame> frameBuffer;
    private int bufferSize;
    private int writeIndex = 0;
    private int readIndex = 0;

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

    private void Start()
    {
        StartCoroutine(WaitForKinectManagerInitialization(Initialize));
    }

    private IEnumerator WaitForKinectManagerInitialization(Action callback)
    {
        yield return new WaitUntil(() => KinectManager.Instance != null && KinectManager.Instance.IsInitialized());
        callback.Invoke();
    }

    public void Initialize()
    {
        kinectManager = KinectManager.Instance;

        sensorData = kinectManager.GetSensorData(sensorIndex);

        if (sensorData != null && sensorData.sensorInterface != null)
        {
            sensorInt = (DepthSensorBase)sensorData.sensorInterface;

            if (sensorInt.pointCloudVertexTexture != null &&
                sensorInt.pointCloudColorTexture != null)
            {
                InitializeBuffer();
            }
            else
            {
                Debug.LogWarning($"[ReplayBuffer] Point cloud textures not ready for sensor {sensorIndex}");
            }
        }
        else
        {
            Debug.LogError($"[ReplayBuffer] Sensor data not available for index {sensorIndex}");
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

        if (frameBuffer != null)
        {
            foreach (var frame in frameBuffer)
                frame.Release();
            frameBuffer.Clear();
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

        float estimatedMemoryMB = (bufferSize * textureWidth * textureHeight * 12) / (1024f * 1024f);

        if (showDebugInfo)
        {
            Debug.Log($"[ReplayBuffer] Initialized: {bufferSize} frames @ {textureWidth}x{textureHeight}, " +
                      $"delay: {replayDelaySeconds}s, estimated memory: {estimatedMemoryMB:F2} MB");
        }
    }


    private void CaptureFrame()
    {
        RenderTexture vertexRT = sensorInt.pointCloudVertexTexture;
        RenderTexture colorRT = sensorInt.pointCloudColorTexture;

        if (vertexRT == null || colorRT == null)
            return;

        PointCloudFrame frame = frameBuffer[writeIndex];
        frame.timestamp = Time.time;

        Graphics.Blit(vertexRT, frame.vertexTexture);
        Graphics.Blit(colorRT, frame.colorTexture);

        writeIndex = (writeIndex + 1) % bufferSize;

        if (showDebugInfo && writeIndex == 0)
        {
            Debug.Log($"[ReplayBuffer] Buffer wrap-around at {Time.time:F2}s");
        }    
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


    void OnDestroy()
    {
        if (frameBuffer != null)
        {
            foreach (var frame in frameBuffer)
                frame.Release();
            frameBuffer.Clear();
        }

        if (replayVertexTexture != null && replayVertexTexture.name == "ReplayVertexTexture")
        {
            replayVertexTexture.Release();
            Destroy(replayVertexTexture);
        }

        if (replayColorTexture != null && replayColorTexture.name == "ReplayColorTexture")
        {
            replayColorTexture.Release();
            Destroy(replayColorTexture);
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

    /// <summary>
    /// Releases all buffer memory. Call this when replay is not needed temporarily.
    /// </summary>
    public void ReleaseBuffer()
    {
        if (frameBuffer != null)
        {
            foreach (var frame in frameBuffer)
                frame.Release();
            frameBuffer.Clear();
            frameBuffer = null;
        }

        isBufferReady = false;

        if (showDebugInfo)
            Debug.Log("[ReplayBuffer] Buffer released to free memory");
    }
}