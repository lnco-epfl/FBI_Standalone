using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;


/// <summary>
/// Circular buffer to record and replay the point cloud with a configurable delay
/// </summary>
public class PointCloudReplayBuffer : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
    public int sensorIndex = 0;

    [Tooltip("Replay delay in seconds (e.g. 2.0 for a 2-second delay)")]
    [Range(0.1f, 10f)]
    public float replayDelaySeconds = 2.0f;

    [Tooltip("Number of frames per second to capture (affects buffer size)")]
    [Range(15, 60)]
    public int captureFrameRate = 30;

    [Tooltip("Enable/disable replay mode")]
    public bool enableReplay = true;

    [Header("Output Textures")]
    [Tooltip("Render texture for replayed point cloud vertices")]
    public RenderTexture replayVertexTexture = null;

    [Tooltip("Render texture for replayed point cloud colors")]
    public RenderTexture replayColorTexture = null;

    [Header("Debug")]
    [Tooltip("Show debug information")]
    public bool showDebugInfo = false;

    // Class to store a single point cloud frame
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

    // References
    private KinectManager kinectManager = null;
    private KinectInterop.SensorData sensorData = null;
    private DepthSensorBase sensorInt = null;

    // Timing
    private float lastCaptureTime = 0f;
    private float captureInterval = 0f;
    private int textureWidth = 0;
    private int textureHeight = 0;

    // Material used to copy textures
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

        // Initialize the buffer
        InitializeBuffer();

        // Create a material to copy textures
        copyMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
    }

    void InitializeBuffer()
    {
        // Calculate the required buffer size
        captureInterval = 1f / captureFrameRate;
        bufferSize = Mathf.CeilToInt(replayDelaySeconds * captureFrameRate) + 2;

        // Determine texture resolution
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

        // Create the buffer
        frameBuffer = new List<PointCloudFrame>(bufferSize);
        for (int i = 0; i < bufferSize; i++)
        {
            frameBuffer.Add(new PointCloudFrame(textureWidth, textureHeight));
        }

        if (showDebugInfo)
        {
            Debug.Log($"PointCloudReplayBuffer initialized: {bufferSize} frames, {textureWidth}x{textureHeight}, delay: {replayDelaySeconds}s");
        }

        // Create render textures if they don't exist
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
    }

    void Update()
    {
        if (!enableReplay || sensorInt == null)
            return;

        float currentTime = Time.time;

        // Capture a new frame if needed
        if (currentTime - lastCaptureTime >= captureInterval)
        {
            CaptureFrame();
            lastCaptureTime = currentTime;
        }

        // Read and display the frame with the appropriate delay
        DisplayDelayedFrame();
    }

    void CaptureFrame()
    {
        // Get the current point cloud textures
        RenderTexture vertexRT = sensorInt.pointCloudVertexTexture;
        RenderTexture colorRT = sensorInt.pointCloudColorTexture;

        if (vertexRT == null || colorRT == null)
            return;

        // Get the frame at the current write index
        PointCloudFrame frame = frameBuffer[writeIndex];
        frame.timestamp = Time.time;

        // Copy the textures
        CopyRenderTextureToTexture2D(vertexRT, frame.vertexTexture);
        CopyRenderTextureToTexture2D(colorRT, frame.colorTexture);

        // Advance the write index (circular buffer)
        writeIndex = (writeIndex + 1) % bufferSize;

        if (showDebugInfo && writeIndex == 0)
        {
            Debug.Log($"Buffer wrap-around at {Time.time:F2}s");
        }
    }

    void DisplayDelayedFrame()
    {
        // Calculate which frame to display based on the delay
        float targetTime = Time.time - replayDelaySeconds;

        // Find the frame closest to the target time
        int bestIndex = -1;
        float bestTimeDiff = float.MaxValue;

        for (int i = 0; i < bufferSize; i++)
        {
            float timeDiff = Mathf.Abs(frameBuffer[i].timestamp - targetTime);
            if (timeDiff < bestTimeDiff && frameBuffer[i].timestamp > 0)
            {
                bestTimeDiff = timeDiff;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            readIndex = bestIndex;
            PointCloudFrame frame = frameBuffer[readIndex];

            // Copy to the output render textures
            Graphics.Blit(frame.vertexTexture, replayVertexTexture);
            Graphics.Blit(frame.colorTexture, replayColorTexture);
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
        // Clean up the buffer
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

        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Box("Point Cloud Replay Buffer");
        GUILayout.Label($"Delay: {replayDelaySeconds:F2}s");
        GUILayout.Label($"Buffer Size: {bufferSize} frames");
        GUILayout.Label($"Write Index: {writeIndex}");
        GUILayout.Label($"Read Index: {readIndex}");
        GUILayout.Label($"FPS: {captureFrameRate}");
        GUILayout.Label($"Resolution: {textureWidth}x{textureHeight}");
        GUILayout.EndArea();
    }

    // Public methods to control replay

    /// <summary>
    /// Changes the replay delay at runtime
    /// </summary>
    public void SetReplayDelay(float delaySeconds)
    {
        replayDelaySeconds = Mathf.Clamp(delaySeconds, 0.1f, 10f);

        // Reinitialize the buffer if needed
        int newBufferSize = Mathf.CeilToInt(replayDelaySeconds * captureFrameRate) + 2;
        if (newBufferSize != bufferSize)
        {
            // Clean up the old buffer
            foreach (var frame in frameBuffer)
            {
                frame.Release();
            }

            InitializeBuffer();
        }
    }

    /// <summary>
    /// Enables or disables replay mode
    /// </summary>
    public void ToggleReplay(bool enabled)
    {
        enableReplay = enabled;
    }

    /// <summary>
    /// Clears the buffer
    /// </summary>
    public void ClearBuffer()
    {
        writeIndex = 0;
        readIndex = 0;

        foreach (var frame in frameBuffer)
        {
            frame.timestamp = 0f;
        }
    }
}