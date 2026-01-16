using UnityEngine;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Sequences", menuName = "Application Data/Sequences")]
public class Sequences : ScriptableObject
{
    private static Sequences instance;

    public static Sequences Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<Sequences>("Sequences");

                if (instance == null)
                {
                    Debug.LogError("Sequences ScriptableObject not found in Sequences/Resources");
                }
            }
            return instance;
        }
    }

    public List<Sequence> sequences = new List<Sequence>();

#if UNITY_EDITOR
    [ContextMenu("GetSequenceFromFolder")]
    public void GetSequenceFromFolder()
    {
        var assetsGUID = AssetDatabase.FindAssets("t:Sequence", new string[] { "Assets/Application/Runtime/Data/Sequences/Resources/" });

        sequences.Clear();

        for (int i = 0; i < assetsGUID.Length; i++)
        {
            sequences.Add(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetsGUID[i]), typeof(Sequence)) as Sequence);
        }
    }
#endif
}
