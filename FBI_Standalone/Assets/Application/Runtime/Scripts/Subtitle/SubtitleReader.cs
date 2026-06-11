using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Video;

[Serializable]
public class Subtitle
{
    public int index;
    public string startTime;
    public string endTime;
    public string text;
    public float startSeconds;
    public float endSeconds;

    public Subtitle(int index, string startTime, string endTime, string text)
    {
        this.index = index;
        this.startTime = startTime;
        this.endTime = endTime;
        this.text = text;
        this.startSeconds = TimeToSeconds(startTime);
        this.endSeconds = TimeToSeconds(endTime);
    }

    private float TimeToSeconds(string timeString)
    {
        // Format: 00:00:00,000
        string[] parts = timeString.Split(new char[] { ':', ',' });
        int hours = int.Parse(parts[0]);
        int minutes = int.Parse(parts[1]);
        int seconds = int.Parse(parts[2]);
        int milliseconds = int.Parse(parts[3]);

        return hours * 3600 + minutes * 60 + seconds + milliseconds / 1000f;
    }
}

public class SubtitleReader : MonoBehaviour
{
    [Header("Configuration")]
    private VideoPlayer videoPlayer;
    private TextAutoSizer textAutoSizer;

    private string currentPath = string.Empty;

    private List<Subtitle> subtitles = new List<Subtitle>();
    private Subtitle currentSubtitle = null;
    private bool subtitlesLoaded = false;

    void Start()
    {

        textAutoSizer = GetComponentInChildren<TextAutoSizer>();

        videoPlayer = WorldUIManager.Instance.GetVideoPlayer();

        if (videoPlayer != null)
        {
            videoPlayer.started += VideoStarted;
            videoPlayer.loopPointReached += VideoEnded;
        }

        textAutoSizer.SetText(string.Empty);
    }

    void Update()
    {
        if (!subtitlesLoaded || videoPlayer == null)
            return;

        if (videoPlayer.isPlaying)
        {
            DisplaySubtitle((float)videoPlayer.time);
        }
        else
        {
            textAutoSizer.SetText(string.Empty);
        }


    }

    private void LoadSubtitles(string filePath)
    {
        if (!File.Exists(filePath))
        {
            EventFileManager.Error("[SubtitleReader] SRT file not found: " + filePath);
            return;
        }

        try
        {
            string content = File.ReadAllText(filePath, Encoding.UTF8);
            ParseSrtContent(content);
            subtitlesLoaded = true;
            EventFileManager.Log($"[SubtitleReader] Successfully loaded {subtitles.Count} subtitles from {filePath}");
        }
        catch (Exception e)
        {
            EventFileManager.Error($"[SubtitleReader] Error loading subtitles: {e.Message}");
        }
    }

    private void ParseSrtContent(string content)
    {
        subtitles.Clear();


        string[] entries = Regex.Split(content, @"\r\n\r\n|\n\n");

        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            string[] lines = Regex.Split(entry, @"\r\n|\n");
            if (lines.Length < 3)
                continue;


            if (!int.TryParse(lines[0].Trim(), out int index))
                continue;


            string[] timeComponents = lines[1].Split(new string[] { " --> " }, StringSplitOptions.None);
            if (timeComponents.Length != 2)
                continue;

            string startTime = timeComponents[0].Trim();
            string endTime = timeComponents[1].Trim();


            StringBuilder textBuilder = new StringBuilder();
            for (int i = 2; i < lines.Length; i++)
            {
                if (i > 2)
                    textBuilder.AppendLine();
                textBuilder.Append(lines[i]);
            }
            string text = textBuilder.ToString();

            subtitles.Add(new Subtitle(index, startTime, endTime, text));
        }
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
}