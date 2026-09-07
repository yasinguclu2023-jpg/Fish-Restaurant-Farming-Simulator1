using UnityEngine;
using UnityEditor;

public class CheckLightProbeSettings : MonoBehaviour
{
    [MenuItem("Tools/Check Light Probe Settings")]
    public static void Execute()
    {
        // Find Cube object
        GameObject cube = GameObject.Find("Cube");
        if (cube == null)
        {
            Debug.LogError("Cube not found!");
            return;
        }

        MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogError("MeshRenderer not found on Cube!");
            return;
        }

        Debug.Log("=== CUBE MESHRENDERER SETTINGS ===");
        Debug.Log($"Static Flags: {GameObjectUtility.GetStaticEditorFlags(cube)}");
        Debug.Log($"Light Probe Usage: {renderer.lightProbeUsage}");
        Debug.Log($"Reflection Probe Usage: {renderer.reflectionProbeUsage}");
        Debug.Log($"Receive GI: {renderer.receiveGI}");
        Debug.Log($"Receive Shadows: {renderer.receiveShadows}");
        Debug.Log($"Cast Shadows: {renderer.shadowCastingMode}");
        Debug.Log($"Light Probe Proxy Volume Override: {renderer.lightProbeProxyVolumeOverride}");
        
        // Check all objects in scene
        Debug.Log("\n=== ALL OBJECTS STATIC FLAGS ===");
        foreach (var go in FindObjectsOfType<GameObject>())
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                Debug.Log($"{go.name}: Static={flags}, LightProbeUsage={mr.lightProbeUsage}, ReflectionProbeUsage={mr.reflectionProbeUsage}, ReceiveGI={mr.receiveGI}");
            }
        }
        
        // Check if there are any Light Probes baked
        Debug.Log("\n=== LIGHT PROBES INFO ===");
        if (LightmapSettings.lightProbes != null)
        {
            Debug.Log($"Light Probes Count: {LightmapSettings.lightProbes.count}");
            Debug.Log($"Light Probes Positions: {LightmapSettings.lightProbes.positions?.Length ?? 0}");
        }
        else
        {
            Debug.Log("No Light Probes baked!");
        }
        
        // Check Lightmap settings
        Debug.Log("\n=== LIGHTMAP SETTINGS ===");
        Debug.Log($"Lightmaps Mode: {LightmapSettings.lightmapsMode}");
        Debug.Log($"Lightmaps Count: {LightmapSettings.lightmaps?.Length ?? 0}");
    }
}
