using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class SahneMaterialSayaci
{
    [MenuItem("Araclar/Sahne Material Say")]
    static void Say()
    {
        var materialler = new HashSet<Material>();
        var rendererlar = Object.FindObjectsOfType<Renderer>();

        foreach (var r in rendererlar)
            foreach (var m in r.sharedMaterials)
                if (m != null) materialler.Add(m);

        Debug.Log($"Sahnede {rendererlar.Length} Renderer, {materialler.Count} benzersiz material var.");
    }
}