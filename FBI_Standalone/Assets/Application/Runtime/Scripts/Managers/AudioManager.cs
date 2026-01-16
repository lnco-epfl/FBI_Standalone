using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    private AudioSource sfxSource;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    public static AudioManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); } else { instance = this; }
    }


    private void Start()
    {

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        var audioVolumeManager = GetComponent<AudioVolumeManager>();

        sfxSource.outputAudioMixerGroup = audioVolumeManager.AudioMixer.FindMatchingGroups("Master")[0];
        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0.0f;
    }

    public void Play2DSFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }
}