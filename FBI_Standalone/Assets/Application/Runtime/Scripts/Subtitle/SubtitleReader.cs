using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class SubtitleReader : MonoBehaviour
{
    [Header("Configuration")]
    private VideoPlayer videoPlayer;
    private TextAutoSizer textAutoSizer;

    private string currentPath = string.Empty;

    private List<Subtitle> subtitles = new List<Subtitle>();
    private Subtitle currentSubtitle = null;
    private bool subtitlesLoaded = false;

    private List<Subtitle> audioSubtitles = new List<Subtitle>();
    private Subtitle currentAudioSubtitle = null;
    private bool audioSubtitlesActive = false;
    private AudioSource currentAudioSource;
    private float audioElapsed = 0f;

    private bool videoSubtitlesEnabled = true;
    private bool audioSubtitlesEnabled = true;

    private static SubtitleReader instance;
    public static SubtitleReader Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); }
        else
        {
            instance = this;
        }
    }

    void Start()
    {

        textAutoSizer = GetComponentInChildren<TextAutoSizer>();

        videoPlayer = WorldUIManager.Instance.GetVideoPlayer();

        if (videoPlayer != null)
        {
            videoPlayer.started += VideoStarted;
            videoPlayer.loopPointReached += VideoEnded;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.SfxStarted += AudioSfxStarted;
            AudioManager.instance.SfxEnded += AudioSfxEnded;
        }

        HideSubtitle();
    }

    public void HideSubtitle()
    {
        textAutoSizer.SetText(string.Empty);
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.started -= VideoStarted;
            videoPlayer.loopPointReached -= VideoEnded;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.SfxStarted -= AudioSfxStarted;
            AudioManager.instance.SfxEnded -= AudioSfxEnded;
        }
    }

    void Update()
    {
        bool videoSubtitlesReady = subtitlesLoaded && videoPlayer != null;

        if (videoSubtitlesReady && videoPlayer.isPlaying)
        {
            // La vidéo est prioritaire sur l'affichage si elle est en cours de lecture.
            DisplaySubtitle((float)videoPlayer.time);
            return;
        }

        if (audioSubtitlesActive)
        {
            DisplayAudioSubtitle();
        }
    }

    private void LoadSubtitles(string filePath)
    {
        List<Subtitle> loaded = SrtParser.ParseFile(filePath);

        if (loaded == null)
        {
            subtitlesLoaded = false;
            return;
        }

        subtitles = loaded;
        subtitlesLoaded = true;
    }

    private void DisplaySubtitle(float currentTime)
    {

        if (currentSubtitle != null &&
            currentTime >= currentSubtitle.startSeconds &&
            currentTime <= currentSubtitle.endSeconds)
        {
            return;
        }


        currentSubtitle = null;
        foreach (Subtitle subtitle in subtitles)
        {
            if (currentTime >= subtitle.startSeconds && currentTime <= subtitle.endSeconds)
            {
                currentSubtitle = subtitle;
                textAutoSizer.SetText(subtitle.text);
                return;
            }
        }

        textAutoSizer.SetText(string.Empty);
    }

    private void VideoStarted(VideoPlayer source)
    {
        if (!videoSubtitlesEnabled)
        {
            subtitlesLoaded = false;
            subtitles.Clear();
            textAutoSizer.SetText(string.Empty);
            return;
        }

        var path = source.url;

        if (path.StartsWith("file:///"))
            path = path.Substring(8);
        else if (path.StartsWith("file://"))
            path = path.Substring(7);

        path = Uri.UnescapeDataString(path);

        path = path.Replace(".mp4", ".srt");

        if (currentPath != path)
        {
            currentPath = path;
            LoadSubtitles(currentPath);
        }
    }

    private void VideoEnded(VideoPlayer source)
    {
        textAutoSizer.SetText(string.Empty);
        subtitles.Clear();
    }

    public void LoadNewSubtitleFile(string filePath, bool fromStreamingAssets = true)
    {
        subtitlesLoaded = false;

        if (fromStreamingAssets)
        {
            filePath = Path.Combine(Application.streamingAssetsPath, filePath);
        }

        LoadSubtitles(filePath);
    }

    

    private void AudioSfxStarted(AudioSource source)
    {
        if (!audioSubtitlesEnabled)
        {
            audioSubtitles = new List<Subtitle>();
            currentAudioSubtitle = null;
            audioSubtitlesActive = false;
            return;
        }

        string filePath = Path.Combine(AssetsManager.Instance.AudiosPath, source.clip.name + ".srt");
        List<Subtitle> loaded = SrtParser.ParseFile(filePath);

        audioSubtitles = loaded ?? new List<Subtitle>();
        currentAudioSubtitle = null;
        audioElapsed = 0f;
        audioSubtitlesActive = audioSubtitles.Count > 0;
        currentAudioSource = source;
    }

    private void AudioSfxEnded(AudioSource source)
    {
        if(currentAudioSource == source)
        {
            audioSubtitlesActive = false;
            currentAudioSubtitle = null;

            bool videoShowingText = subtitlesLoaded && videoPlayer != null && videoPlayer.isPlaying;
            if (!videoShowingText)
            {
                textAutoSizer.SetText(string.Empty);
            }
        }
    }

    private void DisplayAudioSubtitle()
    {
        audioElapsed += Time.deltaTime;

        if (currentAudioSubtitle != null && audioElapsed >= currentAudioSubtitle.startSeconds && audioElapsed <= currentAudioSubtitle.endSeconds)
        {
            return;
        }

        currentAudioSubtitle = null;
        foreach (Subtitle subtitle in audioSubtitles)
        {
            if (audioElapsed >= subtitle.startSeconds && audioElapsed <= subtitle.endSeconds)
            {
                currentAudioSubtitle = subtitle;
                textAutoSizer.SetText(subtitle.text);
                return;
            }
        }

        textAutoSizer.SetText(string.Empty);
    }

    public void SetVideoSubtitlesEnabled(bool enabled)
    {
        videoSubtitlesEnabled = enabled;
    }

    public void SetAudioSubtitlesEnabled(bool enabled)
    {
        audioSubtitlesEnabled = enabled;
    }
}