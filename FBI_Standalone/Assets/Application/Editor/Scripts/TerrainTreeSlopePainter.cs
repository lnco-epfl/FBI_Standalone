using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainTreeSlopePainter : EditorWindow
{
    private Terrain terrain;
    private int treePrototypeIndex = 0;

    [Range(0, 90)]
    private float maxSlope = 30f;

    [Range(0f, 1f)]
    private float density = 0.8f;

    private int sampleCount = 2000;

    private bool appendToExisting = false;

    [Range(0.5f, 2f)]
    private float minScale = 0.9f;

    [Range(0.5f, 2f)]
    private float maxScale = 1.1f;

    [MenuItem("Tools/Terrain/Fill Trees By Slope")]
    public static void ShowWindow()
    {
        GetWindow<TerrainTreeSlopePainter>("Tree Painter");
    }

    private void OnGUI()
    {
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        if (terrain != null && terrain.terrainData.treePrototypes.Length > 0)
        {
            string[] names = new string[terrain.terrainData.treePrototypes.Length];
            for (int i = 0; i < names.Length; i++)
            {
                GameObject prefab = terrain.terrainData.treePrototypes[i].prefab;
                names[i] = prefab != null ? prefab.name : $"Tree {i}";
            }
            treePrototypeIndex = EditorGUILayout.Popup("Tree Prototype", treePrototypeIndex, names);
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a terrain with at least one Tree Prototype defined (Terrain Settings > Trees).", MessageType.Warning);
        }

        maxSlope = EditorGUILayout.Slider("Max Slope", maxSlope, 0, 90);
        density = EditorGUILayout.Slider("Density", density, 0f, 1f);
        sampleCount = EditorGUILayout.IntSlider("Sample Count", sampleCount, 1, 200000);

        GUILayout.Space(6);
        EditorGUILayout.LabelField("Scale Variation");
        minScale = EditorGUILayout.Slider("Min Scale", minScale, 0.1f, maxScale);
        maxScale = EditorGUILayout.Slider("Max Scale", maxScale, minScale, 3f);

        GUILayout.Space(6);
        appendToExisting = EditorGUILayout.Toggle("Append To Existing Trees", appendToExisting);

        GUILayout.Space(10);

        using (new EditorGUI.DisabledScope(terrain == null || terrain.terrainData.treePrototypes.Length == 0))
        {
            if (GUILayout.Button("Generate Trees"))
            {
                Generate();
            }
        }

        if (terrain != null && GUILayout.Button("Clear All Trees"))
        {
            terrain.terrainData.SetTreeInstances(new TreeInstance[0], true);
        }
    }

    private void Generate()
    {
        TerrainData data = terrain.terrainData;

        List<TreeInstance> instances = new List<TreeInstance>();

        if (appendToExisting)
            instances.AddRange(data.treeInstances);

        for (int i = 0; i < sampleCount; i++)
        {
            float nx = Random.value;
            float nz = Random.value;

            float slope = data.GetSteepness(nx, nz);

            if (slope > maxSlope)
                continue;

            if (Random.value > density)
                continue;

            TreeInstance instance = new TreeInstance
            {
                position = new Vector3(nx, 0f, nz),
                prototypeIndex = treePrototypeIndex,
                widthScale = Random.Range(minScale, maxScale),
                heightScale = Random.Range(minScale, maxScale),
                color = Color.white,
                lightmapColor = Color.white,
                rotation = Random.Range(0f, Mathf.PI * 2f)
            };

            instances.Add(instance);
        }

        data.SetTreeInstances(instances.ToArray(), true);

        Debug.Log($"Trees generated! Placed {instances.Count} instances.");
    }
}
