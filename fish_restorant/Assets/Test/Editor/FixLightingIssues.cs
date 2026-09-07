using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class FixLightingIssues : MonoBehaviour
{
    [MenuItem("Tools/Fix Lighting Issues")]
    public static void Execute()
    {
        // Find Cube
        GameObject cube = GameObject.Find("Cube");
        if (cube == null)
        {
            Debug.LogError("Cube not found!");
            return;
        }
        
        Debug.Log("=== FIXING LIGHTING ISSUES ===");
        
        // 1. Create a Reflection Probe near the Cube
        Debug.Log("1. Creating Reflection Probe...");
        GameObject reflectionProbeGO = new GameObject("Reflection Probe");
        ReflectionProbe reflectionProbe = reflectionProbeGO.AddComponent<ReflectionProbe>();
        
        // Position it near the Cube
        reflectionProbeGO.transform.position = cube.transform.position;
        
        // Configure the Reflection Probe
        reflectionProbe.mode = ReflectionProbeMode.Baked;
        reflectionProbe.size = new Vector3(20, 10, 20); // Large enough to cover the area
        reflectionProbe.resolution = 256;
        reflectionProbe.hdr = true;
        reflectionProbe.importance = 1;
        
        Debug.Log($"Reflection Probe created at {reflectionProbeGO.transform.position}");
        
        // 2. Set the Directional Light to Mixed mode for baking
        Debug.Log("2. Setting Directional Light to Mixed mode...");
        Light directionalLight = FindObjectOfType<Light>();
        if (directionalLight != null && directionalLight.type == LightType.Directional)
        {
            directionalLight.lightmapBakeType = LightmapBakeType.Mixed;
            Debug.Log("Directional Light set to Mixed mode");
        }
        
        // 3. Make sure Cube's MeshRenderer is set to receive Light Probes
        Debug.Log("3. Configuring Cube's MeshRenderer...");
        MeshRenderer cubeRenderer = cube.GetComponent<MeshRenderer>();
        if (cubeRenderer != null)
        {
            cubeRenderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            cubeRenderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            cubeRenderer.receiveGI = ReceiveGI.LightProbes;
            Debug.Log("Cube MeshRenderer configured for Light Probes and Reflection Probes");
        }
        
        // 4. Mark the scene as dirty so changes are saved
        EditorUtility.SetDirty(cube);
        EditorUtility.SetDirty(reflectionProbeGO);
        if (directionalLight != null)
            EditorUtility.SetDirty(directionalLight.gameObject);
        
        // Save the scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        
        Debug.Log("\n=== FIXES APPLIED ===");
        Debug.Log("Now you need to:");
        Debug.Log("1. Go to Window > Rendering > Lighting");
        Debug.Log("2. Click 'Generate Lighting' to bake the lighting");
        Debug.Log("3. The Reflection Probe will also be baked automatically");
        
        // Try to bake lighting automatically
        Debug.Log("\nAttempting to bake lighting...");
        Lightmapping.BakeAsync();
        Debug.Log("Lighting bake started!");
    }
}
