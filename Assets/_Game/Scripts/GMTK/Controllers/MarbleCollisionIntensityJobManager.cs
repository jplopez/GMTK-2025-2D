using System.Collections.Generic;
#if UNITY_JOBS_AVAILABLE
using Unity.Collections;
using Unity.Jobs;
#endif
using UnityEngine;
using GMTK.Jobs;

namespace GMTK {
    /// <summary>
    /// Job-based collision intensity calculator using Unity's Burst compiler for high performance.
    /// Designed to handle 50+ collisions per second efficiently.
    /// </summary>
    public class MarbleCollisionIntensityJobManager : MonoBehaviour {
        
        [Header("Job Settings")]
        [Tooltip("Multiplier for the overall intensity calculation")]
        public float IntensityMultiplier = 1.0f;
        
        [Tooltip("Batch size for parallel job execution")]
        public int JobBatchSize = 32;
        
        [Tooltip("Maximum number of collisions to process per frame")]
        public int MaxCollisionsPerFrame = 100;
        
        [Header("Debug")]
        [SerializeField] private int _currentFrameCollisions = 0;
        [SerializeField] private int _totalCollisionsProcessed = 0;
        [SerializeField] private float _averageIntensity = 0f;
        [SerializeField] private float _lastJobExecutionTime = 0f;
        
        private Rigidbody2D _rigidbody;
        private Queue<CollisionData> _collisionQueue;
        private List<float> _recentIntensities;
        private float _totalIntensity = 0f;
        
#if UNITY_JOBS_AVAILABLE
        // Native arrays for job processing
        private NativeArray<float2> _relativeVelocities;
        private NativeArray<float2> _collisionNormals;
        private NativeArray<float> _mass1Values;
        private NativeArray<float> _mass2Values;
        private NativeArray<float> _collisionIntensities;
#endif
        
        void Awake() {
            _rigidbody = GetComponent<Rigidbody2D>();
            if (_rigidbody == null) {
                Debug.LogError($"MarbleCollisionIntensityJobManager requires a Rigidbody2D component on {gameObject.name}");
            }
            
            _collisionQueue = new Queue<CollisionData>(MaxCollisionsPerFrame);
            _recentIntensities = new List<float>(MaxCollisionsPerFrame);
            
#if UNITY_JOBS_AVAILABLE
            // Initialize native arrays
            _relativeVelocities = new NativeArray<float2>(MaxCollisionsPerFrame, Allocator.Persistent);
            _collisionNormals = new NativeArray<float2>(MaxCollisionsPerFrame, Allocator.Persistent);
            _mass1Values = new NativeArray<float>(MaxCollisionsPerFrame, Allocator.Persistent);
            _mass2Values = new NativeArray<float>(MaxCollisionsPerFrame, Allocator.Persistent);
            _collisionIntensities = new NativeArray<float>(MaxCollisionsPerFrame, Allocator.Persistent);
#endif
        }
        
        void OnDestroy() {
#if UNITY_JOBS_AVAILABLE
            // Clean up native arrays
            if (_relativeVelocities.IsCreated) _relativeVelocities.Dispose();
            if (_collisionNormals.IsCreated) _collisionNormals.Dispose();
            if (_mass1Values.IsCreated) _mass1Values.Dispose();
            if (_mass2Values.IsCreated) _mass2Values.Dispose();
            if (_collisionIntensities.IsCreated) _collisionIntensities.Dispose();
#endif
        }
        
        void OnCollisionEnter2D(Collision2D collision) {
            AddCollisionToQueue(collision);
        }
        
        void LateUpdate() {
            ProcessCollisionQueue();
        }
        
        /// <summary>
        /// Adds a collision to the processing queue
        /// </summary>
        private void AddCollisionToQueue(Collision2D collision) {
            if (_rigidbody == null || _collisionQueue.Count >= MaxCollisionsPerFrame) return;
            
            // Extract collision data
            ContactPoint2D contact = collision.contacts[0];
            Vector2 collisionNormal = contact.normal;
            
            // Calculate relative velocity
            Vector2 relativeVelocity = _rigidbody.linearVelocity;
            if (collision.rigidbody != null) {
                relativeVelocity -= collision.rigidbody.linearVelocity;
            }
            
            // Get masses
            float mass1 = _rigidbody.mass;
            float mass2 = collision.rigidbody != null ? collision.rigidbody.mass : float.MaxValue;
            
            // Create collision data and add to queue
            CollisionData collisionData = new CollisionData(relativeVelocity, collisionNormal, mass1, mass2);
            _collisionQueue.Enqueue(collisionData);
        }
        
        /// <summary>
        /// Processes queued collisions using Burst jobs or fallback calculation
        /// </summary>
        private void ProcessCollisionQueue() {
            if (_collisionQueue.Count == 0) {
                _currentFrameCollisions = 0;
                return;
            }
            
            _currentFrameCollisions = _collisionQueue.Count;
            
#if UNITY_JOBS_AVAILABLE
            ProcessCollisionQueueWithJobs();
#else
            ProcessCollisionQueueFallback();
#endif
            
            _totalCollisionsProcessed += _currentFrameCollisions;
        }
        
#if UNITY_JOBS_AVAILABLE
        /// <summary>
        /// Processes collisions using Unity Jobs system
        /// </summary>
        private void ProcessCollisionQueueWithJobs() {
            // Fill native arrays with collision data
            int index = 0;
            while (_collisionQueue.Count > 0 && index < MaxCollisionsPerFrame) {
                CollisionData data = _collisionQueue.Dequeue();
                _relativeVelocities[index] = data.RelativeVelocity;
                _collisionNormals[index] = data.CollisionNormal;
                _mass1Values[index] = data.Mass1;
                _mass2Values[index] = data.Mass2;
                index++;
            }
            
            if (index == 0) return;
            
            // Create and schedule job
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            CollisionIntensityJob job = new CollisionIntensityJob {
                RelativeVelocities = _relativeVelocities,
                CollisionNormals = _collisionNormals,
                Mass1Values = _mass1Values,
                Mass2Values = _mass2Values,
                IntensityMultiplier = IntensityMultiplier,
                CollisionIntensities = _collisionIntensities
            };
            
            // Schedule job with appropriate batch size
            JobHandle jobHandle = job.Schedule(index, JobBatchSize);
            jobHandle.Complete(); // Wait for completion
            
            stopwatch.Stop();
            _lastJobExecutionTime = (float)stopwatch.Elapsed.TotalMilliseconds;
            
            // Process results
            ProcessJobResults(index);
        }
#endif
        
        /// <summary>
        /// Fallback collision processing when Jobs system is not available
        /// </summary>
        private void ProcessCollisionQueueFallback() {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            _recentIntensities.Clear();
            
            while (_collisionQueue.Count > 0) {
                CollisionData data = _collisionQueue.Dequeue();
                float intensity = CollisionIntensityCalculation.CalculateIntensity(data, IntensityMultiplier);
                
                _recentIntensities.Add(intensity);
                _totalIntensity += intensity;
                
                // Optional: Trigger events or effects based on intensity
                OnCollisionIntensityCalculated(intensity);
            }
            
            stopwatch.Stop();
            _lastJobExecutionTime = (float)stopwatch.Elapsed.TotalMilliseconds;
            
            // Update average
            if (_totalCollisionsProcessed > 0) {
                _averageIntensity = _totalIntensity / (_totalCollisionsProcessed + _currentFrameCollisions);
            }
        }
        
        
#if UNITY_JOBS_AVAILABLE
        /// <summary>
        /// Processes the results from the collision intensity job
        /// </summary>
        private void ProcessJobResults(int count) {
            _recentIntensities.Clear();
            
            for (int i = 0; i < count; i++) {
                float intensity = _collisionIntensities[i];
                _recentIntensities.Add(intensity);
                _totalIntensity += intensity;
                
                // Optional: Trigger events or effects based on intensity
                OnCollisionIntensityCalculated(intensity);
            }
            
            // Update average
            if (_totalCollisionsProcessed > 0) {
                _averageIntensity = _totalIntensity / (_totalCollisionsProcessed + count);
            }
        }
#endif
        
        /// <summary>
        /// Called when collision intensities are calculated. Override for custom behavior.
        /// </summary>
        protected virtual void OnCollisionIntensityCalculated(float intensity) {
            // Base implementation does nothing - can be overridden by subclasses
        }
        
        /// <summary>
        /// Gets the most recent collision intensities from the last frame
        /// </summary>
        public IReadOnlyList<float> RecentIntensities => _recentIntensities;
        
        /// <summary>
        /// Gets the number of collisions processed in the current frame
        /// </summary>
        public int CurrentFrameCollisions => _currentFrameCollisions;
        
        /// <summary>
        /// Gets the total number of collisions processed
        /// </summary>
        public int TotalCollisionsProcessed => _totalCollisionsProcessed;
        
        /// <summary>
        /// Gets the average collision intensity
        /// </summary>
        public float AverageIntensity => _averageIntensity;
        
        /// <summary>
        /// Gets the last job execution time in milliseconds
        /// </summary>
        public float LastJobExecutionTime => _lastJobExecutionTime;
        
        /// <summary>
        /// Resets collision statistics
        /// </summary>
        public void ResetStatistics() {
            _totalCollisionsProcessed = 0;
            _totalIntensity = 0f;
            _averageIntensity = 0f;
            _currentFrameCollisions = 0;
            _recentIntensities.Clear();
        }
    }
}