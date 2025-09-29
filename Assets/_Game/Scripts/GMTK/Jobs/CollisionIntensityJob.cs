#if UNITY_JOBS_AVAILABLE
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
#endif
using UnityEngine;

namespace GMTK.Jobs {
#if UNITY_JOBS_AVAILABLE
    /// <summary>
    /// Burst-compiled job for calculating collision intensity for multiple collisions in parallel.
    /// This should provide better performance when handling 50+ collisions per second.
    /// </summary>
    public struct CollisionIntensityJob : IJobParallelFor {
        
        // Input data (read-only)
        [ReadOnly] public NativeArray<float2> RelativeVelocities;
        [ReadOnly] public NativeArray<float2> CollisionNormals;
        [ReadOnly] public NativeArray<float> Mass1Values;
        [ReadOnly] public NativeArray<float> Mass2Values;
        [ReadOnly] public float IntensityMultiplier;
        
        // Output data
        public NativeArray<float> CollisionIntensities;
        
        public void Execute(int index) {
            // Get collision data for this index
            float2 relativeVelocity = RelativeVelocities[index];
            float2 collisionNormal = CollisionNormals[index];
            float mass1 = Mass1Values[index];
            float mass2 = Mass2Values[index];
            
            // Calculate velocity component along collision normal
            float normalVelocity = math.dot(relativeVelocity, -collisionNormal);
            
            // Calculate effective mass
            float effectiveMass;
            if (mass2 == float.MaxValue) {
                // Static object case
                effectiveMass = mass1;
            } else {
                effectiveMass = (mass1 * mass2) / (mass1 + mass2);
            }
            
            // Calculate intensity using kinetic energy approach
            float intensity = 0.5f * effectiveMass * normalVelocity * normalVelocity * IntensityMultiplier;
            
            // Store result (ensure positive intensity)
            CollisionIntensities[index] = math.abs(intensity);
        }
    }
    
    /// <summary>
    /// Data structure to hold collision information for job processing
    /// </summary>
    public struct CollisionData {
        public float2 RelativeVelocity;
        public float2 CollisionNormal;
        public float Mass1;
        public float Mass2;
        
        public CollisionData(Vector2 relativeVelocity, Vector2 collisionNormal, float mass1, float mass2) {
            RelativeVelocity = new float2(relativeVelocity.x, relativeVelocity.y);
            CollisionNormal = new float2(collisionNormal.x, collisionNormal.y);
            Mass1 = mass1;
            Mass2 = mass2;
        }
    }
#else
    /// <summary>
    /// Fallback collision data structure when Jobs system is not available
    /// </summary>
    public struct CollisionData {
        public Vector2 RelativeVelocity;
        public Vector2 CollisionNormal;
        public float Mass1;
        public float Mass2;
        
        public CollisionData(Vector2 relativeVelocity, Vector2 collisionNormal, float mass1, float mass2) {
            RelativeVelocity = relativeVelocity;
            CollisionNormal = collisionNormal;
            Mass1 = mass1;
            Mass2 = mass2;
        }
    }
    
    /// <summary>
    /// Fallback collision intensity calculation when Jobs system is not available
    /// </summary>
    public static class CollisionIntensityCalculation {
        public static float CalculateIntensity(CollisionData data, float intensityMultiplier) {
            // Calculate velocity component along collision normal
            float normalVelocity = Vector2.Dot(data.RelativeVelocity, -data.CollisionNormal);
            
            // Calculate effective mass
            float effectiveMass;
            if (data.Mass2 == float.MaxValue) {
                // Static object case
                effectiveMass = data.Mass1;
            } else {
                effectiveMass = (data.Mass1 * data.Mass2) / (data.Mass1 + data.Mass2);
            }
            
            // Calculate intensity using kinetic energy approach
            float intensity = 0.5f * effectiveMass * normalVelocity * normalVelocity * intensityMultiplier;
            
            // Return positive intensity
            return Mathf.Abs(intensity);
        }
    }
#endif
}