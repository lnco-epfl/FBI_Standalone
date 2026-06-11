using System;
using System.IO;
using System.Collections;
using UnityEngine;

public class AudioRecorderManager : MonoBehaviour
{

    [Header("Recording")]
    [Tooltip("Max recording duration in seconds (0 = unlimited)")]
    public float maxRecordingSeconds = 300f;

    [Tooltip("Sample rate (8000, 16000, 22050, 44100, 48000)")]
    public int sampleRate = 44100;


    [Tooltip("File name prefix")]
    public string filePrefix = "recording";

    private string outputFolderPath;

    public bool IsRecording { get; private set; }
    public float RecordingTime { get; private set; }

    public event Action<string> OnRecordingSaved;   // (filePath)
    public event Action<string> OnRecordingError;   // (errorMessage)
    public event Action OnRecordingStarted;
    public event Action OnRecordingStopped;

    private AudioClip clip;
    private string deviceName;
    private float startTime;
    private Coroutine timerCoroutine;
    public static AudioRecorderManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            EventFileManager.Warning("[AudioRecorderManager] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        outputFolderPath = Path.Combine(Application.dataPath, "..", "Output", "Recordings");

        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

    }

    private void OnDestroy()
    {
        if (IsRecording)
        {
            StopRecording(save: false);
        }
    }

    [ContextMenu("StartRecording")]
    public void StartRecording()
    {
        if (IsRecording)
        {
            EventFileManager.Warning("[AudioRecorderManager] Already recording.");
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            string err = "No microphone detected.";
            EventFileManager.Error($"[AudioRecorderManager] {err}");
            OnRecordingError?.Invoke(err);
            return;
        }

        deviceName = Microphone.devices[0];
        int maxSec = maxRecordingSeconds > 0 ? Mathf.CeilToInt(maxRecordingSeconds) : 3600;

        clip = Microphone.Start(deviceName, loop: false, lengthSec: maxSec, frequency: sampleRate);

        if (clip == null)
        {
            string err = $"Failed to start microphone: {deviceName}";
            EventFileManager.Error($"[AudioRecorderManager] {err}");
            OnRecordingError?.Invoke(err);
            return;
        }

        IsRecording = true;
        startTime = Time.realtimeSinceStartup;
        RecordingTime = 0f;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerCoroutine());

        EventFileManager.Log($"[AudioRecorderManager] Recording started with device: {deviceName}");
        OnRecordingStarted?.Invoke();
    }

    [ContextMenu("StopAndSave")]
    public string StopAndSave()
    {
        return StopAndSave(string.Empty);
    }
    public string StopAndSave(string name = "")
    {
        if (!IsRecording)
        {
            EventFileManager.Warning("[AudioRecorderManager] No recording in progress.");
            return null;
        }

        return StopRecording(save: true, name);
    }

    public void CancelRecording()
    {
        if (IsRecording)
        {
            StopRecording(save: false);
        }
    }

    private string StopRecording(bool save, string name = "")
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        int samplesRecorded = Microphone.GetPosition(deviceName);
        Microphone.End(deviceName);
        IsRecording = false;

        OnRecordingStopped?.Invoke();
        EventFileManager.Log($"[AudioRecorderManager] Recording stopped with a duration of {RecordingTime:F1}s");

        if (!save || samplesRecorded <= 0)
        {
            if (clip != null) { Destroy(clip); clip = null; }
            return null;
        }

        float[] samples = new float[samplesRecorded * clip.channels];
        clip.GetData(samples, 0);

        int channels = clip.channels;
        Destroy(clip);
        clip = null;

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = Path.Combine(outputFolderPath, $"{filePrefix}_{timestamp}_{name}.wav");

        try
        {
            WavWriter.Write(filePath, samples, channels, sampleRate);
            EventFileManager.Log($"[AudioRecorderManager] File saved: {filePath}");
            OnRecordingSaved?.Invoke(filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            string err = $"Failed to save file: {ex.Message}";
            EventFileManager.Error($"[AudioRecorderManager] {err}");
            OnRecordingError?.Invoke(err);
            return null;
        }
    }

    private IEnumerator TimerCoroutine()
    {
        while (IsRecording)
        {
            RecordingTime = Time.realtimeSinceStartup - startTime;

            if (maxRecordingSeconds > 0 && RecordingTime >= maxRecordingSeconds)
            {
                EventFileManager.Log("[AudioRecorderManager] Max duration reached, stopping automatically.");
                StopAndSave();
                yield break;
            }

            yield return null;
        }
    }

}

public static class WavWriter
{
    public static void Write(string path, float[] samples, int channels, int sampleRate)
    {
        const int bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int dataSize = samples.Length * (bitsPerSample / 8);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // RIFF header
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);             // PCM
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)blockAlign);
        bw.Write((short)bitsPerSample);

        // data chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);

        foreach (float s in samples)
        {
            short pcm = (short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767);
            bw.Write(pcm);
        }
    }
}