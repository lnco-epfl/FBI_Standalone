using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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

/// <summary>
/// Parseur SRT partagé, utilisé à la fois par SubtitleReader (vidéo)
/// et AudioManager (sons/SFX) pour éviter de dupliquer la logique.
/// </summary>
public static class SrtParser
{
    /// <summary>
    /// Charge et parse un fichier .srt depuis le disque.
    /// Retourne null si le fichier n'existe pas ou si une erreur survient.
    /// </summary>
    public static List<Subtitle> ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            EventFileManager.Error("[SrtParser] SRT file not found: " + filePath);
            return null;
        }

        try
        {
            string content = File.ReadAllText(filePath, Encoding.UTF8);
            List<Subtitle> subtitles = ParseContent(content);
            EventFileManager.Log($"[SrtParser] Successfully loaded {subtitles.Count} subtitles from {filePath}");
            return subtitles;
        }
        catch (Exception e)
        {
            EventFileManager.Error($"[SrtParser] Error loading subtitles: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parse le contenu textuel brut d'un fichier .srt.
    /// </summary>
    public static List<Subtitle> ParseContent(string content)
    {
        List<Subtitle> subtitles = new List<Subtitle>();

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

        return subtitles;
    }
}
