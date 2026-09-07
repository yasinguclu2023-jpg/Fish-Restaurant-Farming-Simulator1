using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class CheckLightingSettings : MonoBehaviour
{
    [MenuItem("Tools/Check Lighting Settings")]
    public static void Execute()
    {
        Debug.Log("=== LIGHTING SETTINGS ===");
        
        // Check Lightmapping settings
        Debug.Log($"Lightmapper: {Lightmapping.lightingSettings?.lightmapper}");
        Debug.Log($"Baked GI: {Lightmapping.lightingSettings?.bakedGI}");
        Debug.Log($"Realtime GI: {Lightmapping.lightingSettings?.realtimeGI}");
        Debug.Log($"Mixed Bake Mode: {Lightmapping.lightingSettings?.mixedBakeMode}");
        
        // Check all lights
        Debug.Log("\n=== LIGHTS IN SCENE ===");
        var lights = FindObjectsOfType<Light>();
        foreach (var light in lights)
        {
            Debug.Log($"Light: {light.gameObject.name}");
            Debug.Log($"  Type: {light.type}");
            Debug.Log($"  Mode: {light.lightmapBakeType}");
            Debug.Log($"  Intensity: {light.intensity}");
            Debug.Log($"  Bounce Intensity: {light.bounceIntensity}");
        }
        
        // Check Environment Lighting
        Debug.Log("\n=== ENVIRONMENT LIGHTING ===");
        Debug.Log($"Ambient Mode: {RenderSettings.ambientMode}");
        Debug.Log($"Ambient Intensity: {RenderSettings.ambientIntensity}");
        Debug.Log($"Ambient Light: {RenderSettings.ambientLight}");
        Debug.Log($"Ambient Sky Color: {RenderSettings.ambientSkyColor}");
        Debug.Log($"Ambient Equator Color: {RenderSettings.ambientEquatorColor}");
        Debug.Log($"Ambient Ground Color: {RenderSettings.ambientGroundColor}");
        
        // Check if there's a Reflection Probe
        Debug.Log("\n=== REFLECTION PROBES ===");
        var reflectionProbes = FindObjectsOfType<ReflectionProbe>();
        if (reflectionProbes.Length == 0)
        {
            Debug.LogWarning("NO REFLECTION PROBES FOUND! This is why reflections are not working!");
        }
        else
        {
            foreach (var probe in reflectionProbes)
            {
                Debug.Log($"Reflection Probe: {probe.gameObject.name}");
                Debug.Log($"  Mode: {probe.mode}");
                Debug.Log($"  Importance: {probe.importance}");
                Debug.Log($"  Box Size: {probe.size}");
            }
        }
        
        // Check Default Reflection
        Debug.Log("\n=== DEFAULT REFLECTION ===");
        Debug.Log($"Default Reflection Mode: {RenderSettings.defaultReflectionMode}");
        Debug.Log($"Default Reflection Resolution: {RenderSettings.defaultReflectionResolution}");
        Debug.Log($"Reflection Intensity: {RenderSettings.reflectionIntensity}");
        Debug.Log($"Reflection Bounces: {RenderSettings.reflectionBounces}");
    }
}
