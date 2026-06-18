using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private int maxSfxSources = 10;
    private AudioSource[] sfxSources;
    private int nextSourceIndex = 0;

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
        maxSfxSources = Mathf.Clamp(maxSfxSources, 1, 10);

        var audioVolumeManager = GetComponent<AudioVolumeManager>();
        var masterGroup = audioVolumeManager.AudioMixer.FindMatchingGroups("Master")[0];

        sfxSources = new AudioSource[maxSfxSources];
        for (int i = 0; i < maxSfxSources; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = masterGroup;
            source.volume = sfxVolume;
            source.playOnAwake = false;
            source.spatialBlend = 0.0f;
            sfxSources[i] = source;
        }
    }

    public void Play2DSFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        source.PlayOneShot(clip, volumeScale * sfxVolume);
    }

    private AudioSource GetAvailableSource()
    {
        for (int i = 0; i < sfxSources.Length; i++)
        {
            if (!sfxSources[i].isPlaying)
            {
                return sfxSources[i];
            }
        }

        AudioSource fallback = sfxSources[nextSourceIndex];
        nextSourceIndex = (nextSourceIndex + 1) % sfxSources.Length;
        return fallback;
    }

}