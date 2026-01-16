using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.rfilkov.kinect;

/// <summary>
/// Buffer circulaire pour enregistrer et rejouer le point cloud avec un délai configurable
/// </summary>
public class PointCloudReplayBuffer : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
    public int sensorIndex = 0;

    [Tooltip("Délai de replay en secondes (ex: 2.0 pour 2 secondes de délai)")]
    [Range(0.1f, 10f)]
    public float replayDelaySeconds = 2.0f;

    [Tooltip("Nombre de frames par seconde à capturer (influence la taille du buffer)")]
    [Range(15, 60)]
    public int captureFrameRate = 30;

    [Tooltip("Activer/désactiver le mode replay")]
    public bool enableReplay = true;

    [Header("Textures de sortie")]
    [Tooltip("Render texture pour les vertices du point cloud en replay")]
    public RenderTexture replayVertexTexture = null;

    [Tooltip("Render texture pour les couleurs du point cloud en replay")]
    public RenderTexture replayColorTexture = null;

    [Header("Debug")]
    [Tooltip("Afficher les informations de debug")]
    public bool showDebugInfo = false;

    // Classe pour stocker une frame du point cloud
    private class PointCloudFrame
    {
        public Texture2D vertexTexture;
        public Texture2D colorTexture;
        public float timestamp;

        public PointCloudFrame(int width, int height)
        {
            vertexTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            colorTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            timestamp = 0f;
        }

        public void Release()
        {
            if (vertexTexture != null) Destroy(vertexTexture);
            if (colorTexture != null) Destroy(colorTexture);
        }
    }

    // Buffer circulaire
    private List<PointCloudFrame> frameBuffer;
    private int bufferSize;
    private int writeIndex = 0;
    private int readIndex = 0;

    // Références
    private KinectManager kinectManager = null;
    private KinectInterop.SensorData sensorData = null;
    private DepthSensorBase sensorInt = null;

    // Timing
    private float lastCaptureTime = 0f;
    private float captureInterval = 0f;
    private int textureWidth = 0;
    private int textureHeight = 0;

    // Matériau pour copier les textures
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

        // Initialiser le buffer
        InitializeBuffer();

        // Créer un matériau pour copier les textures
        copyMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
    }

    void InitializeBuffer()
    {
        // Calculer la taille du buffer nécessaire
        captureInterval = 1f / captureFrameRate;
        bufferSize = Mathf.CeilToInt(replayDelaySeconds * captureFrameRate) + 2;

        // Déterminer la résolution des textures
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

        // Créer le buffer
        frameBuffer = new List<PointCloudFrame>(bufferSize);
        for (int i = 0; i < bufferSize; i++)
        {
            frameBuffer.Add(new PointCloudFrame(textureWidth, textureHeight));
        }

        if (showDebugInfo)
        {
            Debug.Log($"PointCloudReplayBuffer initialized: {bufferSize} frames, {textureWidth}x{textureHeight}, delay: {replayDelaySeconds}s");
        }

        // Créer les render textures si elles n'existent pas
        if (replayVertexTexture == null)
        {
            replayVertexTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGBFloat);
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

        // Capturer une nouvelle frame si nécessaire
        if (currentTime - lastCaptureTime >= captureInterval)
        {
            CaptureFrame();
            lastCaptureTime = currentTime;
        }

        // Lire et afficher la frame avec le délai approprié
        DisplayDelayedFrame();
    }

    void CaptureFrame()
    {
        // Obtenir les textures du point cloud actuelles
        RenderTexture vertexRT = sensorInt.pointCloudVertexTexture;
        RenderTexture colorRT = sensorInt.pointCloudColorTexture;

        if (vertexRT == null || colorRT == null)
            return;

        // Obtenir la frame du buffer à l'index d'écriture
        PointCloudFrame frame = frameBuffer[writeIndex];
        frame.timestamp = Time.time;

        // Copier les textures
        CopyRenderTextureToTexture2D(vertexRT, frame.vertexTexture);
        CopyRenderTextureToTexture2D(colorRT, frame.colorTexture);

        // Avancer l'index d'écriture (buffer circulaire)
        writeIndex = (writeIndex + 1) % bufferSize;

        if (showDebugInfo && writeIndex == 0)
        {
            Debug.Log($"Buffer wrap-around at {Time.time:F2}s");
        }
    }

    void DisplayDelayedFrame()
    {
        // Calculer quel frame afficher basé sur le délai
        float targetTime = Time.time - replayDelaySeconds;

        // Trouver la frame la plus proche du temps cible
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

            // Copier vers les render textures de sortie
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
        // Nettoyer le buffer
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

    // Méthodes publiques pour contrôler le replay

    /// <summary>
    /// Change le délai de replay en temps réel
    /// </summary>
    public void SetReplayDelay(float delaySeconds)
    {
        replayDelaySeconds = Mathf.Clamp(delaySeconds, 0.1f, 10f);

        // Réinitialiser le buffer si nécessaire
        int newBufferSize = Mathf.CeilToInt(replayDelaySeconds * captureFrameRate) + 2;
        if (newBufferSize != bufferSize)
        {
            // Nettoyer l'ancien buffer
            foreach (var frame in frameBuffer)
            {
                frame.Release();
            }

            InitializeBuffer();
        }
    }

    /// <summary>
    /// Active ou désactive le mode replay
    /// </summary>
    public void ToggleReplay(bool enabled)
    {
        enableReplay = enabled;
    }

    /// <summary>
    /// Vide le buffer
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
