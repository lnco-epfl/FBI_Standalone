using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PeripheralLetters : MonoBehaviour
{
    public TMP_Text[] targets = new TMP_Text[4];
    public string alphabet = "CDHKNORSVZ";
    public bool randomizeOnStart = true;

    void Start()
    {
        if (randomizeOnStart) Randomize();
    }

    [ContextMenu("Randomize")]
    public void Randomize()
    {
        var pool = new List<char>(alphabet);
        foreach (var t in targets)
        {
            if (t == null) continue;
            int i = Random.Range(0, pool.Count);
            t.text = pool[i].ToString();
            pool.RemoveAt(i);
        }
    }
}
