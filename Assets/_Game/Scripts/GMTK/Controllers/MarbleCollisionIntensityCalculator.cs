using UnityEngine;

namespace GMTK {
    /// <summary>
    /// Component-based collision intensity calculator for baseline performance comparison.
    /// This component calculates collision intensity based on velocity, mass, and impact angle.
    /// </summary>
    public class MarbleCollisionIntensityCalculator : MonoBehaviour {
        
        [Header("Collision Intensity Settings")]
        [Tooltip("Multiplier for the overall intensity calculation")]
        public float IntensityMultiplier = 1.0f;
        
        [Header("Debug")]
        [SerializeField] private float _lastCalculatedIntensity = 0f;
        [SerializeField] private int _collisionCount = 0;
        [SerializeField] private float _averageIntensity = 0f;
        
        private Rigidbody2D _rigidbody;
        private float _totalIntensity = 0f;
        
        void Awake() {
            _rigidbody = GetComponent<Rigidbody2D>();
            if (_rigidbody == null) {
                Debug.LogError($"MarbleCollisionIntensityCalculator requires a Rigidbody2D component on {gameObject.name}");
            }
        }
        
        void OnCollisionEnter2D(Collision2D collision) {
            float intensity = CalculateCollisionIntensity(collision);
            _lastCalculatedIntensity = intensity;
            _collisionCount++;
            _totalIntensity += intensity;
            _averageIntensity = _totalIntensity / _collisionCount;
            
            // Optional: Trigger events or effects based on intensity
            OnCollisionIntensityCalculated(intensity, collision);
        }
        
        /// <summary>
        /// Calculates collision intensity based on relative velocity, mass, and contact normal.
        /// </summary>
        /// <param name="collision">The collision information</param>
        /// <returns>Calculated intensity value</returns>
        public float CalculateCollisionIntensity(Collision2D collision) {
            if (_rigidbody == null) return 0f;
            
            // Get collision contact point
            ContactPoint2D contact = collision.contacts[0];
            Vector2 collisionNormal = contact.normal;
            
            // Get relative velocity at collision point
            Vector2 relativeVelocity = _rigidbody.linearVelocity;
            if (collision.rigidbody != null) {
                relativeVelocity -= collision.rigidbody.linearVelocity;
            }
            
            // Calculate velocity component along collision normal
            float normalVelocity = Vector2.Dot(relativeVelocity, -collisionNormal);
            
            // Consider mass for intensity calculation
            float mass1 = _rigidbody.mass;
            float mass2 = collision.rigidbody != null ? collision.rigidbody.mass : float.MaxValue; // Treat static objects as infinite mass
            
            // Calculate effective mass
            float effectiveMass = (mass1 * mass2) / (mass1 + mass2);
            if (mass2 == float.MaxValue) effectiveMass = mass1; // For static objects
            
            // Calculate intensity using kinetic energy approach
            float intensity = 0.5f * effectiveMass * normalVelocity * normalVelocity * IntensityMultiplier;
            
            // Ensure positive intensity
            return Mathf.Abs(intensity);
        }
        
        /// <summary>
        /// Called when collision intensity is calculated. Override for custom behavior.
        /// </summary>
        protected virtual void OnCollisionIntensityCalculated(float intensity, Collision2D collision) {
            // Base implementation does nothing - can be overridden by subclasses
        }
        
        /// <summary>
        /// Gets the last calculated collision intensity
        /// </summary>
        public float LastIntensity => _lastCalculatedIntensity;
        
        /// <summary>
        /// Gets the total number of collisions processed
        /// </summary>
        public int CollisionCount => _collisionCount;
        
        /// <summary>
        /// Gets the average collision intensity
        /// </summary>
        public float AverageIntensity => _averageIntensity;
        
        /// <summary>
        /// Resets collision statistics
        /// </summary>
        public void ResetStatistics() {
            _collisionCount = 0;
            _totalIntensity = 0f;
            _averageIntensity = 0f;
            _lastCalculatedIntensity = 0f;
        }
    }
}