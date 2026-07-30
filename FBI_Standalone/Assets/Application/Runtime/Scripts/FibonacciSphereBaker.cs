using UnityEngine;

public class FibonacciSphereBaker : MonoBehaviour
{
    public static Texture2D BakeSphereTexture(int pointCount, float radius)
    {
        int texSize = Mathf.CeilToInt(Mathf.Sqrt(pointCount));
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBAFloat, false);
        Color[] pixels = new Color[texSize * texSize];

        double goldenAngle = Mathf.PI * (3.0 - Mathf.Sqrt(5.0f)); // ~2.39996

        for (int i = 0; i < pointCount; i++)
        {
            double t = (i + 0.5) / pointCount;
            double phi = System.Math.Acos(1.0 - 2.0 * t);
            double theta = goldenAngle * i;

            float x = (float)(radius * System.Math.Sin(phi) * System.Math.Cos(theta));
            float y = (float)(radius * System.Math.Sin(phi) * System.Math.Sin(theta));
            float z = (float)(radius * System.Math.Cos(phi));

            pixels[i] = new Color(x, y, z, 1f);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        return tex;
    }
}