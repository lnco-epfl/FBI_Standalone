using UnityEngine;
using UnityEditor;

public class TerrainDetailSlopePainter : EditorWindow
{
    private Terrain terrain;
    private int detailLayer = 0;

    [Range(0, 90)]
    private float maxSlope = 30f;

    [Range(0f, 1f)]
    private float density = 0.8f;

    private int detailCount = 16;

    [MenuItem("Tools/Terrain/Fill Details By Slope")]
    public static void ShowWindow()
    {
        GetWindow<TerrainDetailSlopePainter>("Detail Painter");
    }

    private void OnGUI()
    {
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        detailLayer = EditorGUILayout.IntField("Detail Layer", detailLayer);
        maxSlope = EditorGUILayout.Slider("Max Slope", maxSlope, 0, 90);
        density = EditorGUILayout.Slider("Density", density, 0f, 1f);
        detailCount = EditorGUILayout.IntSlider("Detail Count", detailCount, 1, 1024);

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Details"))
        {
            if (terrain != null)
                Generate();
        }
    }

    private void Generate()
    {
        TerrainData data = terrain.terrainData;

        int width = data.detailWidth;
        int height = data.detailHeight;

        int[,] details = new int[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / (width - 1);
                float ny = (float)y / (height - 1);

                float slope = data.GetSteepness(nx, ny);

                if (slope <= maxSlope)
                {
                    details[y, x] = Mathf.RoundToInt(detailCount * density);
                }
                else
                {
                    details[y, x] = 0;
                }
            }
        }

        data.SetDetailLayer(0, 0, detailLayer, details);

        Debug.Log("Details generated!");
    }
}
