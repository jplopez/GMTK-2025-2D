using UnityEngine;
using Ameba;

namespace GMTK.Examples {
    /// <summary>
    /// Example script demonstrating how to use the collision intensity calculation system.
    /// Shows basic integration, event handling, and performance monitoring.
    /// </summary>
    public class CollisionIntensityExample : MonoBehaviour {
        
        [Header("References")]
        [Tooltip("The PlayableMarbleController to monitor")]
        public PlayableMarbleController Marble;
        
        [Header("Configuration")]
        [Tooltip("Use Burst Jobs for collision intensity calculation")]
        public bool UseBurstJobs = true;
        
        [Tooltip("Enable collision intensity calculation")]
        public bool EnableCollisionIntensity = true;
        
        [Header("Effect Settings")]
        [Tooltip("Minimum intensity threshold for triggering effects")]
        public float EffectThreshold = 5.0f;
        
        [Header("Monitoring")]
        [SerializeField] private CollisionIntensityStats _lastStats;
        [SerializeField] private float _highestIntensity = 0f;
        [SerializeField] private int _effectsTriggered = 0;
        
        void Start() {
            if (Marble == null) {
                Marble = FindFirstObjectByType<PlayableMarbleController>();
            }
            
            if (Marble != null) {
                SetupCollisionIntensity();
            } else {
                this.LogError("No PlayableMarbleController found in scene!");
            }
        }
        
        void Update() {
            if (Marble != null && EnableCollisionIntensity) {
                MonitorCollisionIntensity();
            }
        }
        
        /// <summary>
        /// Sets up the collision intensity calculation system
        /// </summary>
        private void SetupCollisionIntensity() {
            this.Log($"Setting up collision intensity calculation (Burst Jobs: {UseBurstJobs})");
            
            // Configure collision intensity settings
            Marble.EnableCollisionIntensity = EnableCollisionIntensity;
            Marble.SwitchCollisionIntensityMethod(UseBurstJobs);
            
            // Reset statistics
            Marble.ResetCollisionIntensityStats();
            
            this.Log("Collision intensity system initialized successfully");
        }
        
        /// <summary>
        /// Monitors collision intensity and triggers effects based on intensity levels
        /// </summary>
        private void MonitorCollisionIntensity() {
            _lastStats = Marble.GetCollisionIntensityStats();
            
            // Check for new high-intensity collisions
            if (_lastStats.AverageIntensity > _highestIntensity) {
                _highestIntensity = _lastStats.AverageIntensity;
                this.Log($"New highest intensity recorded: {_highestIntensity:F2}");
            }
            
            // Trigger effects for high-intensity collisions
            if (_lastStats.AverageIntensity >= EffectThreshold) {
                TriggerCollisionEffect(_lastStats.AverageIntensity);
            }
            
            // Log performance information periodically
            if (Time.frameCount % 300 == 0) { // Every 5 seconds at 60 FPS
                LogPerformanceInfo();
            }
        }
        
        /// <summary>
        /// Triggers effects based on collision intensity
        /// </summary>
        private void TriggerCollisionEffect(float intensity) {
            _effectsTriggered++;
            this.Log($"Collision effect triggered! Intensity: {intensity:F2} (Effects so far: {_effectsTriggered})");
        }
        
        /// <summary>
        /// Logs current performance information
        /// </summary>
        private void LogPerformanceInfo() {
            if (_lastStats.TotalCollisions > 0) {
                this.Log($"Performance Info - Total Collisions: {_lastStats.TotalCollisions}, " +
                        $"Avg Intensity: {_lastStats.AverageIntensity:F2}, " +
                        $"Using Burst Jobs: {_lastStats.UsingBurstJobs}, " +
                        $"Last Execution Time: {_lastStats.LastExecutionTime:F2}ms");
            }
        }
        
#if UNITY_EDITOR
        [Button("Switch to Component-Based")]
        public void SwitchToComponent() {
            if (Marble != null) {
                UseBurstJobs = false;
                Marble.SwitchCollisionIntensityMethod(false);
                this.Log("Switched to component-based collision intensity calculation");
            }
        }
        
        [Button("Switch to Burst Jobs")]
        public void SwitchToBurstJobs() {
            if (Marble != null) {
                UseBurstJobs = true;
                Marble.SwitchCollisionIntensityMethod(true);
                this.Log("Switched to Burst Jobs collision intensity calculation");
            }
        }
        
        [Button("Reset Statistics")]
        public void ResetStatistics() {
            if (Marble != null) {
                Marble.ResetCollisionIntensityStats();
                _highestIntensity = 0f;
                _effectsTriggered = 0;
                this.Log("Collision intensity statistics reset");
            }
        }
#endif
        
        void OnGUI() {
            if (Marble == null || !EnableCollisionIntensity) return;
            
            // Display real-time collision intensity information
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Collision Intensity Monitor", GUI.skin.box);
            
            GUILayout.Label($"Method: {(_lastStats.UsingBurstJobs ? "Burst Jobs" : "Component")}");
            GUILayout.Label($"Total Collisions: {_lastStats.TotalCollisions}");
            GUILayout.Label($"Current Frame: {_lastStats.CurrentFrameCollisions}");
            GUILayout.Label($"Average Intensity: {_lastStats.AverageIntensity:F2}");
            GUILayout.Label($"Highest Intensity: {_highestIntensity:F2}");
            GUILayout.Label($"Effects Triggered: {_effectsTriggered}");
            
            if (_lastStats.UsingBurstJobs) {
                GUILayout.Label($"Execution Time: {_lastStats.LastExecutionTime:F2}ms");
            }
            
            GUILayout.EndArea();
        }
    }
}