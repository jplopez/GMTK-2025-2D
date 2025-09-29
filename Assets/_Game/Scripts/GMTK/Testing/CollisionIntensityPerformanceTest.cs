using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ameba;

namespace GMTK.Testing {
    /// <summary>
    /// Performance testing script to compare component-based vs Burst Jobs collision intensity calculation.
    /// Creates multiple collision scenarios to simulate 50+ collisions per second.
    /// </summary>
    public class CollisionIntensityPerformanceTest : MonoBehaviour {
        
        [Header("Test Configuration")]
        [Tooltip("Reference to the PlayableMarbleController to test")]
        public PlayableMarbleController TestMarble;
        
        [Tooltip("Number of test collisions to simulate per second")]
        public int CollisionsPerSecond = 60;
        
        [Tooltip("Duration of each test in seconds")]
        public float TestDuration = 10f;
        
        [Tooltip("Number of times to run each test for averaging")]
        public int TestIterations = 3;
        
        [Header("Test Objects")]
        [Tooltip("Prefab to spawn as collision objects")]
        public GameObject CollisionObjectPrefab;
        
        [Tooltip("Number of collision objects to create")]
        public int NumberOfCollisionObjects = 20;
        
        [Tooltip("Area around the marble where collision objects will be spawned")]
        public float SpawnRadius = 5f;
        
        [Header("Test Results")]
        [SerializeField] private PerformanceTestResults _componentResults;
        [SerializeField] private PerformanceTestResults _burstJobResults;
        [SerializeField] private bool _testInProgress = false;
        
        private List<GameObject> _spawnedObjects = new List<GameObject>();
        private System.Diagnostics.Stopwatch _testStopwatch;
        
        void Start() {
            if (TestMarble == null) {
                TestMarble = FindFirstObjectByType<PlayableMarbleController>();
            }
            
            _testStopwatch = new System.Diagnostics.Stopwatch();
        }
        
#if UNITY_EDITOR
        [Button("Run Performance Test")]
#endif
        public void RunPerformanceTest() {
            if (_testInProgress) {
                this.LogWarning("Test already in progress!");
                return;
            }
            
            if (TestMarble == null) {
                this.LogError("TestMarble is not assigned!");
                return;
            }
            
            StartCoroutine(RunFullPerformanceTest());
        }
        
        private IEnumerator RunFullPerformanceTest() {
            _testInProgress = true;
            this.Log("Starting collision intensity performance test...");
            
            // Setup test environment
            yield return StartCoroutine(SetupTestEnvironment());
            
            // Test component-based approach
            this.Log("Testing component-based collision intensity calculation...");
            _componentResults = yield return StartCoroutine(TestCollisionIntensityMethod(false));
            
            yield return new WaitForSeconds(1f); // Brief pause between tests
            
            // Test Burst Jobs approach
            this.Log("Testing Burst Jobs collision intensity calculation...");
            _burstJobResults = yield return StartCoroutine(TestCollisionIntensityMethod(true));
            
            // Cleanup test environment
            CleanupTestEnvironment();
            
            // Display results
            DisplayResults();
            
            _testInProgress = false;
            this.Log("Performance test completed!");
        }
        
        private IEnumerator SetupTestEnvironment() {
            this.Log("Setting up test environment...");
            
            // Create collision objects around the marble
            Vector3 marblePos = TestMarble.transform.position;
            
            for (int i = 0; i < NumberOfCollisionObjects; i++) {
                Vector2 randomOffset = Random.insideUnitCircle * SpawnRadius;
                Vector3 spawnPos = marblePos + new Vector3(randomOffset.x, randomOffset.y, 0);
                
                GameObject collisionObj;
                if (CollisionObjectPrefab != null) {
                    collisionObj = Instantiate(CollisionObjectPrefab, spawnPos, Quaternion.identity);
                } else {
                    // Create basic collision object
                    collisionObj = CreateBasicCollisionObject(spawnPos);
                }
                
                _spawnedObjects.Add(collisionObj);
            }
            
            yield return null;
        }
        
        private GameObject CreateBasicCollisionObject(Vector3 position) {
            GameObject obj = new GameObject($"TestCollisionObject_{_spawnedObjects.Count}");
            obj.transform.position = position;
            
            // Add collider
            CircleCollider2D collider = obj.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            
            // Add rigidbody
            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.mass = Random.Range(1f, 5f);
            rb.gravityScale = 0f; // No gravity for controlled movement
            
            return obj;
        }
        
        private IEnumerator TestCollisionIntensityMethod(bool useBurstJobs) {
            PerformanceTestResults results = new PerformanceTestResults();
            results.UsedBurstJobs = useBurstJobs;
            
            // Configure marble for this test
            TestMarble.SwitchCollisionIntensityMethod(useBurstJobs);
            TestMarble.ResetCollisionIntensityStats();
            
            float totalTestTime = 0f;
            int totalCollisions = 0;
            List<float> frameTimes = new List<float>();
            
            for (int iteration = 0; iteration < TestIterations; iteration++) {
                this.Log($"Running iteration {iteration + 1}/{TestIterations} for {(useBurstJobs ? "Burst Jobs" : "Component")} method...");
                
                TestMarble.ResetCollisionIntensityStats();
                _testStopwatch.Restart();
                
                float iterationStartTime = Time.time;
                float nextCollisionTime = iterationStartTime;
                float collisionInterval = 1f / CollisionsPerSecond;
                
                while (Time.time - iterationStartTime < TestDuration) {
                    // Trigger collisions at the specified rate
                    if (Time.time >= nextCollisionTime) {
                        TriggerRandomCollision();
                        nextCollisionTime += collisionInterval;
                    }
                    
                    // Measure frame time
                    float frameStartTime = Time.realtimeSinceStartup;
                    yield return null;
                    float frameTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f; // Convert to ms
                    frameTimes.Add(frameTime);
                }
                
                _testStopwatch.Stop();
                
                var stats = TestMarble.GetCollisionIntensityStats();
                totalTestTime += (float)_testStopwatch.Elapsed.TotalMilliseconds;
                totalCollisions += stats.TotalCollisions;
                
                yield return new WaitForSeconds(0.5f); // Brief pause between iterations
            }
            
            // Calculate results
            results.AverageExecutionTime = totalTestTime / TestIterations;
            results.TotalCollisionsProcessed = totalCollisions;
            results.AverageCollisionsPerSecond = totalCollisions / (TestDuration * TestIterations);
            results.AverageFrameTime = frameTimes.Count > 0 ? CalculateAverage(frameTimes) : 0f;
            results.MaxFrameTime = frameTimes.Count > 0 ? CalculateMax(frameTimes) : 0f;
            
            var finalStats = TestMarble.GetCollisionIntensityStats();
            results.AverageIntensity = finalStats.AverageIntensity;
            
            return results;
        }
        
        private void TriggerRandomCollision() {
            if (_spawnedObjects.Count == 0) return;
            
            // Move a random collision object towards the marble to trigger collision
            GameObject randomObj = _spawnedObjects[Random.Range(0, _spawnedObjects.Count)];
            Rigidbody2D objRb = randomObj.GetComponent<Rigidbody2D>();
            
            if (objRb != null) {
                Vector2 direction = (TestMarble.transform.position - randomObj.transform.position).normalized;
                objRb.AddForce(direction * Random.Range(5f, 15f), ForceMode2D.Impulse);
            }
        }
        
        private void CleanupTestEnvironment() {
            foreach (GameObject obj in _spawnedObjects) {
                if (obj != null) {
                    DestroyImmediate(obj);
                }
            }
            _spawnedObjects.Clear();
        }
        
        private void DisplayResults() {
            this.Log("=== COLLISION INTENSITY PERFORMANCE TEST RESULTS ===");
            this.Log($"Component-based approach:");
            this.Log($"  - Average execution time: {_componentResults.AverageExecutionTime:F2} ms");
            this.Log($"  - Total collisions processed: {_componentResults.TotalCollisionsProcessed}");
            this.Log($"  - Average collisions per second: {_componentResults.AverageCollisionsPerSecond:F1}");
            this.Log($"  - Average frame time: {_componentResults.AverageFrameTime:F2} ms");
            this.Log($"  - Max frame time: {_componentResults.MaxFrameTime:F2} ms");
            
            this.Log($"Burst Jobs approach:");
            this.Log($"  - Average execution time: {_burstJobResults.AverageExecutionTime:F2} ms");
            this.Log($"  - Total collisions processed: {_burstJobResults.TotalCollisionsProcessed}");
            this.Log($"  - Average collisions per second: {_burstJobResults.AverageCollisionsPerSecond:F1}");
            this.Log($"  - Average frame time: {_burstJobResults.AverageFrameTime:F2} ms");
            this.Log($"  - Max frame time: {_burstJobResults.MaxFrameTime:F2} ms");
            
            // Performance comparison
            if (_burstJobResults.AverageExecutionTime > 0) {
                float performanceGain = _componentResults.AverageExecutionTime / _burstJobResults.AverageExecutionTime;
                this.Log($"Performance gain: {performanceGain:F2}x ({(performanceGain - 1) * 100:F1}% improvement)");
            }
        }
        
        private float CalculateAverage(List<float> values) {
            if (values.Count == 0) return 0f;
            float sum = 0f;
            foreach (float value in values) sum += value;
            return sum / values.Count;
        }
        
        private float CalculateMax(List<float> values) {
            if (values.Count == 0) return 0f;
            float max = values[0];
            foreach (float value in values) {
                if (value > max) max = value;
            }
            return max;
        }
        
        [System.Serializable]
        public struct PerformanceTestResults {
            public bool UsedBurstJobs;
            public float AverageExecutionTime;
            public int TotalCollisionsProcessed;
            public float AverageCollisionsPerSecond;
            public float AverageFrameTime;
            public float MaxFrameTime;
            public float AverageIntensity;
        }
    }
}