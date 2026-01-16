using UnityEngine;
using UnityEngine.Audio;


public class AudioVolumeManager : MonoBehaviour
{
    public AudioMixer AudioMixer { get => audioMixer; }
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string volumeParameter = "Volume";

    private const float minVolume = -80f;
    private const float maxVolume = 0f;

    private float currentVolume = 1f;

    private static AudioVolumeManager instance;
    public static AudioVolumeManager Instance { get { return instance; } }



    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); } else { instance = this; }
    }

    private void Start()
    {
        SetVolume(currentVolume);
    }

    public void SetVolume(float volumeValue)
    {
        currentVolume = Mathf.Clamp01(volumeValue);

        float dBValue = Mathf.Log10(Mathf.Max(0.0001f, currentVolume)) * 20f;

        dBValue = Mathf.Clamp(dBValue, minVolume, maxVolume);

        audioMixer.SetFloat(volumeParameter, dBValue);
    }

    public float GetVolume()
    {
        return currentVolume;
    }

    public void Mute()
    {
        audioMixer.SetFloat(volumeParameter, minVolume);
    }

    public void Unmute()
    {
        SetVolume(currentVolume);
    }

}