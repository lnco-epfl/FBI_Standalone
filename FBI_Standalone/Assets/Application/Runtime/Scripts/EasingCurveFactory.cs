using UnityEngine;

public static class EasingCurveFactory
{
    public static AnimationCurve Create(EasingType type, int samples = 20)
    {
        var curve = new AnimationCurve();
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float value = Evaluate(type, t);
            curve.AddKey(new Keyframe(t, value));
        }

        // Tangentes lisses pour éviter les segments cassés entre les clés
        for (int i = 0; i < curve.keys.Length; i++)
            curve.SmoothTangents(i, 0f);

        return curve;
    }

    static float Evaluate(EasingType type, float t)
    {
        switch (type)
        {
            case EasingType.Linear:
                return t;

            case EasingType.InOutSine:
                return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

            case EasingType.InOutQuad:
                return t < 0.5f
                    ? 2f * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 2) / 2f;

            case EasingType.InOutCubic:
                return t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 3) / 2f;

            case EasingType.InOutQuart:
                return t < 0.5f
                    ? 8f * t * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 4) / 2f;

            case EasingType.InOutExpo:
                if (t == 0f) return 0f;
                if (t == 1f) return 1f;
                return t < 0.5f
                    ? Mathf.Pow(2f, 20f * t - 10f) / 2f
                    : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;

            default: // Default
                return t;
        }
    }
}