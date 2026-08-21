using UnityEngine;

public class FibonacciSphereBaker
{
    private int pointCount = 0;
    private float radius = 0f;
    private Texture2D texture;

    public Texture2D BakeSphereTexture(int _pointCount, float _radius)
    {
        if (_pointCount <= 0)
        {
            Debug.LogError("Point count must be greater than 0.");
            return null;
        }
        if (_radius <= 0)
        {
            Debug.LogError("Radius must be greater than 0.");
            return null;
        }

        if (_pointCount != pointCount || _radius != radius)
        {
            pointCount = _pointCount;
            radius = _radius;

            if (texture != null)
            {
                Object.Destroy(texture);
            }

            int texSize = Mathf.CeilToInt(Mathf.Sqrt(pointCount));
            texture = new Texture2D(texSize, texSize, TextureFormat.RGBAFloat, false);
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

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        return texture;
    }
}