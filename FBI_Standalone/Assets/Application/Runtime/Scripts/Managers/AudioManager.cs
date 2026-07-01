using System;
using System.Collections;
using System.Collections.Generic;
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

    public event Action<AudioSource> SfxStarted;
    public event Action<AudioSource> SfxEnded;

    private readonly List<AudioSource> activeSfxSource = new List<AudioSource>();

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

    public AudioSource Play2DSFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return null;

        AudioSource source = GetAvailableSource();
        source.PlayOneShot(clip, volumeScale * sfxVolume);

        source.clip = clip;

        activeSfxSource.Add(source);
        SfxStarted?.Invoke(source);
        StartCoroutine(RaiseSfxEndedAfterDelay(source, clip.length));

        return source;
    }

    public void KillSound(AudioSource source)
    {
        if (source == null) return;

        source.Stop();

        activeSfxSource.Remove(source);
        SfxEnded?.Invoke(source);
        
    }


    public void KillAllSounds()
    {
        StopAllCoroutines();

        foreach (AudioSource source in sfxSources)
        {
            source.Stop();
        }

        List<AudioSource> sourceToNotify = new List<AudioSource>(activeSfxSource);
        activeSfxSource.Clear();

        foreach (AudioSource source in sourceToNotify)
        {
            SfxEnded?.Invoke(source);
        }
    }

    private IEnumerator RaiseSfxEndedAfterDelay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        activeSfxSource.Remove(source);
        SfxEnded?.Invoke(source);
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