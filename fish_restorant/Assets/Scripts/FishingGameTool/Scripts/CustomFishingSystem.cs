using UnityEngine;
using FishingGameTool.Fishing;
using FishingGameTool.Fishing.Rod;
using System.Collections;

namespace FishingGame
{
    /// <summary>
    /// Custom fishing system that overrides the cast direction for more realistic casting
    /// </summary>
    public class CustomFishingSystem : MonoBehaviour
    {
        [Header("References")]
        public FishingSystem fishingSystem;
        public FishingRod fishingRod;
        
        [Header("Cast Settings")]
        [Tooltip("How much forward vs up (0 = only forward, 1 = equal forward and up)")]
        [Range(0f, 1f)]
        public float upwardAngle = 0.35f;
        
        [Tooltip("Maximum cast distance multiplier")]
        [Range(1f, 20f)]
        public float maxDistance = 8f;
        
        [Tooltip("Minimum cast distance")]
        [Range(0.5f, 5f)]
        public float minDistance = 1f;
        
        [Tooltip("Power curve - higher = more power needed for distance (1=linear, 2=quadratic)")]
        [Range(1f, 3f)]
        public float powerCurve = 2f;
        
        [Tooltip("Use camera direction for casting")]
        public bool useCameraDirection = true;
        public Transform cameraTransform;
        
        private bool wasCasting = false;
        private float savedForcePercent = 0f;
        
        void Start()
        {
            if (fishingSystem == null)
                fishingSystem = GetComponent<FishingSystem>();
            
            if (fishingRod == null && fishingSystem != null)
                fishingRod = fishingSystem._fishingRod;
                
            if (cameraTransform == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                    cameraTransform = cam.transform;
            }
        }
        
        void Update()
        {
            if (fishingSystem == null || fishingRod == null)
                return;
            
            // Detect when cast button is released
            bool isCasting = fishingSystem._castInput;
            
            if (wasCasting && !isCasting && fishingSystem._currentCastForce > 0)
            {
                // Save force percentage before it resets
                savedForcePercent = fishingSystem._currentCastForce / fishingSystem._maxCastForce;
                StartCoroutine(CustomCast());
            }
            
            wasCasting = isCasting;
        }
        
        IEnumerator CustomCast()
        {
            // Wait for the original system to spawn the float
            yield return new WaitForSeconds(fishingSystem._spawnFloatDelay + 0.1f);
            
            // Find the spawned float and adjust its velocity
            if (fishingRod._fishingFloat != null)
            {
                Rigidbody floatRb = fishingRod._fishingFloat.GetComponent<Rigidbody>();
                if (floatRb != null)
                {
                    // Stop current velocity first
                    floatRb.velocity = Vector3.zero;
                    
                    // Calculate direction
                    Vector3 forward = useCameraDirection && cameraTransform != null 
                        ? cameraTransform.forward 
                        : transform.forward;
                    
                    // Create a direction that's mostly forward with slight upward angle
                    Vector3 castDirection = (forward + Vector3.up * upwardAngle).normalized;
                    
                    // Apply power curve for balanced feel
                    // powerCurve=2: 25% charge = 6% power, 50% = 25%, 75% = 56%, 100% = 100%
                    float curvedPower = Mathf.Pow(savedForcePercent, powerCurve);
                    
                    // Calculate final velocity (lerp between min and max distance)
                    float finalSpeed = Mathf.Lerp(minDistance, maxDistance, curvedPower);
                    
                    // Apply the new velocity
                    floatRb.velocity = castDirection * finalSpeed;
                    
                    Debug.Log($"Cast: {savedForcePercent*100:F0}% power -> {finalSpeed:F1} speed");
                }
            }
        }
    }
}
