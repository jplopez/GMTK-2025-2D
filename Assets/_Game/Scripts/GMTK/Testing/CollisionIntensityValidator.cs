using UnityEngine;
using Ameba;

namespace GMTK.Testing {
    /// <summary>
    /// Simple validation script to test collision intensity calculation functionality.
    /// Can be used to verify that both component-based and Burst Jobs approaches work correctly.
    /// </summary>
    public class CollisionIntensityValidator : MonoBehaviour {
        
        [Header("Validation Settings")]
        [Tooltip("Reference to the PlayableMarbleController to validate")]
        public PlayableMarbleController TestMarble;
        
        [Tooltip("Test object to collide with the marble")]
        public GameObject TestCollisionObject;
        
        [Tooltip("Force to apply to trigger collision")]
        public float CollisionForce = 10f;
        
        [Header("Validation Results")]
        [SerializeField] private bool _componentTestPassed = false;
        [SerializeField] private bool _burstJobTestPassed = false;
        [SerializeField] private float _lastComponentIntensity = 0f;
        [SerializeField] private float _lastBurstJobIntensity = 0f;
        
        void Start() {
            if (TestMarble == null) {
                TestMarble = FindFirstObjectByType<PlayableMarbleController>();
            }
            
            if (TestCollisionObject == null) {
                CreateTestCollisionObject();
            }
        }
        
#if UNITY_EDITOR
        [Button("Validate Component Approach")]
#endif
        public void ValidateComponentApproach() {
            if (TestMarble == null) {
                this.LogError("TestMarble is not assigned!");
                return;
            }
            
            this.Log("Validating component-based collision intensity calculation...");
            
            // Switch to component approach
            TestMarble.SwitchCollisionIntensityMethod(false);
            TestMarble.ResetCollisionIntensityStats();
            
            // Trigger collision
            TriggerTestCollision();
            
            // Wait a frame and check results
            StartCoroutine(CheckComponentResults());
        }
        
#if UNITY_EDITOR
        [Button("Validate Burst Jobs Approach")]
#endif
        public void ValidateBurstJobsApproach() {
            if (TestMarble == null) {
                this.LogError("TestMarble is not assigned!");
                return;
            }
            
            this.Log("Validating Burst Jobs collision intensity calculation...");
            
            // Switch to Burst Jobs approach
            TestMarble.SwitchCollisionIntensityMethod(true);
            TestMarble.ResetCollisionIntensityStats();
            
            // Trigger collision
            TriggerTestCollision();
            
            // Wait a frame and check results
            StartCoroutine(CheckBurstJobResults());
        }
        
#if UNITY_EDITOR
        [Button("Validate Both Approaches")]
#endif
        public void ValidateBothApproaches() {
            StartCoroutine(RunFullValidation());
        }
        
        private void CreateTestCollisionObject() {
            TestCollisionObject = new GameObject("TestCollisionObject");
            TestCollisionObject.transform.position = TestMarble.transform.position + Vector3.right * 2f;
            
            // Add physics components
            CircleCollider2D collider = TestCollisionObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            
            Rigidbody2D rb = TestCollisionObject.AddComponent<Rigidbody2D>();
            rb.mass = 2f;
            rb.gravityScale = 0f;
            
            this.Log("Created test collision object");
        }
        
        private void TriggerTestCollision() {
            if (TestCollisionObject == null) return;
            
            Rigidbody2D rb = TestCollisionObject.GetComponent<Rigidbody2D>();
            if (rb != null) {
                // Apply force towards the marble
                Vector2 direction = (TestMarble.transform.position - TestCollisionObject.transform.position).normalized;
                rb.AddForce(direction * CollisionForce, ForceMode2D.Impulse);
                
                this.Log($"Applied force {CollisionForce} towards marble");
            }
        }
        
        private System.Collections.IEnumerator CheckComponentResults() {
            // Wait a bit for collision to occur and be processed
            yield return new WaitForSeconds(0.5f);
            
            var stats = TestMarble.GetCollisionIntensityStats();
            
            if (stats.TotalCollisions > 0) {
                _componentTestPassed = true;
                _lastComponentIntensity = stats.AverageIntensity;
                this.Log($"✓ Component approach validation PASSED. Collisions: {stats.TotalCollisions}, Average Intensity: {stats.AverageIntensity:F2}");
            } else {
                _componentTestPassed = false;
                this.LogWarning("✗ Component approach validation FAILED. No collisions detected.");
            }
        }
        
        private System.Collections.IEnumerator CheckBurstJobResults() {
            // Wait a bit for collision to occur and be processed
            yield return new WaitForSeconds(0.5f);
            
            var stats = TestMarble.GetCollisionIntensityStats();
            
            if (stats.TotalCollisions > 0) {
                _burstJobTestPassed = true;
                _lastBurstJobIntensity = stats.AverageIntensity;
                this.Log($"✓ Burst Jobs approach validation PASSED. Collisions: {stats.TotalCollisions}, Average Intensity: {stats.AverageIntensity:F2}, Execution Time: {stats.LastExecutionTime:F2}ms");
            } else {
                _burstJobTestPassed = false;
                this.LogWarning("✗ Burst Jobs approach validation FAILED. No collisions detected.");
            }
        }
        
        private System.Collections.IEnumerator RunFullValidation() {
            this.Log("Starting full validation of both approaches...");
            
            // Reset test collision object position
            if (TestCollisionObject != null) {
                TestCollisionObject.transform.position = TestMarble.transform.position + Vector3.right * 2f;
                TestCollisionObject.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            }
            
            // Test component approach
            ValidateComponentApproach();
            yield return new WaitForSeconds(2f);
            
            // Reset test collision object position
            if (TestCollisionObject != null) {
                TestCollisionObject.transform.position = TestMarble.transform.position + Vector3.right * 2f;
                TestCollisionObject.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            }
            
            // Test Burst Jobs approach
            ValidateBurstJobsApproach();
            yield return new WaitForSeconds(2f);
            
            // Display final results
            DisplayValidationSummary();
        }
        
        private void DisplayValidationSummary() {
            this.Log("=== COLLISION INTENSITY VALIDATION SUMMARY ===");
            this.Log($"Component Approach: {(_componentTestPassed ? "PASSED" : "FAILED")} - Intensity: {_lastComponentIntensity:F2}");
            this.Log($"Burst Jobs Approach: {(_burstJobTestPassed ? "PASSED" : "FAILED")} - Intensity: {_lastBurstJobIntensity:F2}");
            
            if (_componentTestPassed && _burstJobTestPassed) {
                float intensityDifference = Mathf.Abs(_lastComponentIntensity - _lastBurstJobIntensity);
                float tolerancePercent = intensityDifference / Mathf.Max(_lastComponentIntensity, _lastBurstJobIntensity) * 100f;
                
                this.Log($"Intensity difference: {intensityDifference:F2} ({tolerancePercent:F1}%)");
                
                if (tolerancePercent < 5f) {
                    this.Log("✓ Both approaches produce similar results - Implementation is correct!");
                } else {
                    this.LogWarning("⚠ Large difference in results - Check implementation!");
                }
            }
            
            bool allTestsPassed = _componentTestPassed && _burstJobTestPassed;
            this.Log($"Overall validation: {(allTestsPassed ? "PASSED" : "FAILED")}");
        }
        
        void OnDrawGizmosSelected() {
            if (TestMarble != null && TestCollisionObject != null) {
                // Draw line between marble and test object
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(TestMarble.transform.position, TestCollisionObject.transform.position);
                
                // Draw collision force direction
                Vector3 direction = (TestMarble.transform.position - TestCollisionObject.transform.position).normalized;
                Gizmos.color = Color.red;
                Gizmos.DrawRay(TestCollisionObject.transform.position, direction * 2f);
            }
        }
    }
}