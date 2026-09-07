using UnityEngine;
using UnityEditor;

public class CheckProbePositions : MonoBehaviour
{
    [MenuItem("Tools/Check Probe Positions")]
    public static void Execute()
    {
        // Find Cube object
        GameObject cube = GameObject.Find("Cube");
        if (cube != null)
        {
            Debug.Log($"Cube Position: {cube.transform.position}");
        }
        
        // Check Light Probe positions
        if (LightmapSettings.lightProbes != null && LightmapSettings.lightProbes.positions != null)
        {
            Debug.Log($"\n=== LIGHT PROBE POSITIONS ({LightmapSettings.lightProbes.positions.Length}) ===");
            
            Vector3 minPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            
            foreach (var pos in LightmapSettings.lightProbes.positions)
            {
                minPos = Vector3.Min(minPos, pos);
                maxPos = Vector3.Max(maxPos, pos);
            }
            
            Debug.Log($"Light Probes Bounding Box:");
            Debug.Log($"  Min: {minPos}");
            Debug.Log($"  Max: {maxPos}");
            
            // Check if Cube is within the Light Probe bounds
            if (cube != null)
            {
                Vector3 cubePos = cube.transform.position;
                bool isInside = cubePos.x >= minPos.x && cubePos.x <= maxPos.x &&
                               cubePos.y >= minPos.y && cubePos.y <= maxPos.y &&
                               cubePos.z >= minPos.z && cubePos.z <= maxPos.z;
                               
                Debug.Log($"\nCube is inside Light Probe bounds: {isInside}");
                
                if (!isInside)
                {
                    Debug.LogWarning("PROBLEM FOUND: Cube is OUTSIDE the Light Probe coverage area!");
                    Debug.LogWarning($"Cube position ({cubePos}) is not within Light Probe bounds ({minPos} to {maxPos})");
                }
            }
        }
        
        // Find all LightProbeGroup components
        Debug.Log("\n=== LIGHT PROBE GROUPS IN SCENE ===");
        var probeGroups = FindObjectsOfType<LightProbeGroup>();
        foreach (var group in probeGroups)
        {
            Debug.Log($"LightProbeGroup on: {group.gameObject.name}");
            Debug.Log($"  Position: {group.transform.position}");
            Debug.Log($"  Probe Count: {group.probePositions?.Length ?? 0}");
            
            if (group.probePositions != null && group.probePositions.Length > 0)
            {
                Vector3 groupMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 groupMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                
                foreach (var localPos in group.probePositions)
                {
                    Vector3 worldPos = group.transform.TransformPoint(localPos);
                    groupMin = Vector3.Min(groupMin, worldPos);
                    groupMax = Vector3.Max(groupMax, worldPos);
                }
                
                Debug.Log($"  World Bounds: Min={groupMin}, Max={groupMax}");
            }
        }
    }
}
