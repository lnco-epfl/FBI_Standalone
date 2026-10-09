using System;
using System.Text;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class LetterSeries : MonoBehaviour
{
    [Header("Lines (top to bottom)")]
    public TMP_Text[] lines;

    [Header("Generation")]
    public string alphabet = "CDHKNORSVZ";
    public int lettersPerLine = 5;
    public bool randomizeOnStart = true;

    [Header("Presets (optional)")]
    public Preset[] presets;

    [Serializable]
    public class Preset
    {
        public string name;
        public string[] series;
    }

    public event Action<string[]> OnSeriesChanged;

    int _presetIndex = -1;

    void Start()
    {
        if (randomizeOnStart) Randomize();
    }

    [ContextMenu("Randomize")]
    public void Randomize()
    {
        var series = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string s;
            do { s = RandomString(lettersPerLine); }
            while (i > 0 && s == series[i - 1]);
            series[i] = s;
        }
        SetSeries(series);
    }

    public void SetSeries(string[] series)
    {
        for (int i = 0; i < lines.Length; i++)
            lines[i].text = i < series.Length ? series[i] : "";
        OnSeriesChanged?.Invoke(GetSeries());
    }

    public void SetLine(int index, string text)
    {
        if (!InRange(index)) return;
        lines[index].text = text;
        OnSeriesChanged?.Invoke(GetSeries());
    }

    public void RandomizeLine(int index)
    {
        SetLine(index, RandomString(lettersPerLine));
    }

    public string[] GetSeries()
    {
        var result = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++) result[i] = lines[i].text;
        return result;
    }

    public void ShowLine(int index, bool visible)
    {
        if (InRange(index)) lines[index].gameObject.SetActive(visible);
    }

    public void ShowFirstLines(int count)
    {
        for (int i = 0; i < lines.Length; i++)
            lines[i].gameObject.SetActive(i < count);
    }

    public void ShowAll() => ShowFirstLines(lines.Length);

    public void Clear() => SetSeries(new string[0]);

    public void NextPreset() => ApplyPreset(_presetIndex + 1);

    public void PreviousPreset() => ApplyPreset(_presetIndex - 1);

    public void ApplyPreset(int index)
    {
        if (presets == null || presets.Length == 0) return;
        _presetIndex = (index % presets.Length + presets.Length) % presets.Length;
        SetSeries(presets[_presetIndex].series);
    }

    string RandomString(int length)
    {
        var sb = new StringBuilder(length);
        char last = ' ';
        for (int i = 0; i < length; i++)
        {
            char c;
            do { c = alphabet[Random.Range(0, alphabet.Length)]; } while (c == last);
            sb.Append(c);
            last = c;
        }
        return sb.ToString();
    }

    bool InRange(int index) => index >= 0 && index < lines.Length;
}
