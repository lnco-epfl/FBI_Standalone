using System.Text;
using TMPro;
using UnityEngine;

public class SharpnessChart : MonoBehaviour
{
    [Header("Placement")]
    public Transform cameraTransform;
    public float distance = 2f;
    public float heightOffset = 0f;
    public bool placeInFrontOnStart = true;

    [Header("Text")]
    public TMP_FontAsset font;
    public Color textColor = new Color(0.08f, 0.08f, 0.08f);
    [TextArea] public string instruction =
        "Read the last line you can see clearly.\n" +
        "If everything is blurry, adjust the headset or the lens spacing.";
    public float instructionAngle = 0.9f;

    [Header("Letter lines (cap height in degrees)")]
    public float[] lineAngles = { 0.60f, 0.40f, 0.28f, 0.20f, 0.14f, 0.10f };
    public int lettersPerLine = 5;
    public float lineSpacingFactor = 2.2f;

    [Header("Cross / ring")]
    public float ringRadiusDeg = 2.0f;
    public float lineWidthDeg = 0.05f;

    const string SloanLetters = "CDHKNORSVZ";

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (placeInFrontOnStart && cameraTransform != null)
        {
            Vector3 fwd = cameraTransform.forward; fwd.y = 0f; fwd.Normalize();
            transform.position = cameraTransform.position + fwd * distance + Vector3.up * heightOffset;
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        Build();
    }

    float DegToMeters(float deg) => 2f * distance * Mathf.Tan(deg * 0.5f * Mathf.Deg2Rad);

    void Build()
    {
        BuildRing();
        BuildCross();

        float y = DegToMeters(ringRadiusDeg) + DegToMeters(1.2f);
        CreateText(instruction, instructionAngle, y, false);

        y = -(DegToMeters(ringRadiusDeg) + DegToMeters(1.0f));
        foreach (float angle in lineAngles)
        {
            float h = DegToMeters(angle);
            CreateText(RandomLetters(lettersPerLine), angle, y, true);
            y -= h * lineSpacingFactor + DegToMeters(0.25f);
        }
    }

    string RandomLetters(int n)
    {
        var sb = new StringBuilder();
        char last = ' ';
        for (int i = 0; i < n; i++)
        {
            char c;
            do { c = SloanLetters[Random.Range(0, SloanLetters.Length)]; } while (c == last);
            sb.Append(c);
            last = c;
        }
        return sb.ToString();
    }

    TextMeshPro CreateText(string content, float capHeightDeg, float localY, bool spaced)
    {
        var go = new GameObject("Text_" + capHeightDeg.ToString("0.00") + "deg");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, localY, 0f);

        var t = go.AddComponent<TextMeshPro>();
        if (font != null) t.font = font;
        t.color = textColor;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.rectTransform.sizeDelta = new Vector2(DegToMeters(30f), DegToMeters(5f));
        if (spaced) t.characterSpacing = 40f;

        // Measure a real "H" to match the target cap height exactly
        float targetMeters = DegToMeters(capHeightDeg);
        float probeSize = 10f;
        t.fontSize = probeSize;
        t.text = "H";
        t.ForceMeshUpdate();
        float probeHeight = t.textBounds.size.y;
        float size = probeHeight > 0f ? probeSize * targetMeters / probeHeight : probeSize;

        t.fontSize = size;
        t.text = content;
        t.ForceMeshUpdate();
        return t;
    }

    void BuildRing()
    {
        var go = new GameObject("Ring");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = DegToMeters(lineWidthDeg);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = textColor;

        const int n = 128;
        float r = DegToMeters(ringRadiusDeg) * 0.5f;
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
    }

    void BuildCross()
    {
        float half = DegToMeters(ringRadiusDeg) * 0.5f * 0.7f;
        MakeSegment("CrossH", new Vector3(-half, 0, 0), new Vector3(half, 0, 0));
        MakeSegment("CrossV", new Vector3(0, -half, 0), new Vector3(0, half, 0));
    }

    void MakeSegment(string name, Vector3 a, Vector3 b)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.widthMultiplier = DegToMeters(lineWidthDeg);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = textColor;
    }
}
