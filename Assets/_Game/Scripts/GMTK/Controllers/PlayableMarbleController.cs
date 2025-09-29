using Ameba;
using UnityEngine;

namespace GMTK {
  public class PlayableMarbleController : MonoBehaviour {

    [Header("Model")]
    public GameObject Model;

    [Header("Physics")]
    public float Mass = 5f;
    public float GravityScale = 1f;
    public float AngularDamping = 0.05f;
    public Vector2 InitialForce = Vector2.zero;
    [Tooltip("The minimum distance the Marble's position has to change between Update calls, to consider is moving")]
    [Min(0.001f)]
    public float MinimalMovementThreshold = 0.01f;

    [Header("Spawn")]
    [Tooltip("Where should the Marble spawned")]
    public Transform SpawnTransform;
    [Tooltip("The LayerMask the Marble should collide. Level and Interactables the most common")]
    public LayerMask GroundedMask;

    [Header("Collision Intensity")]
    [Tooltip("Use Burst Jobs for collision intensity calculation (recommended for high collision scenarios)")]
    public bool UseBurstJobs = true;
    [Tooltip("Enable collision intensity calculation")]
    public bool EnableCollisionIntensity = true;

    /// <summary>
    /// Whether the Marble is currently colliding with an object is considered ground (ex: wall, platform)
    /// </summary>
    public bool Grounded { get => IsGrounded(); }

    /// <summary>
    /// Whether the Marble has moved since the last Update call
    /// </summary>
    public bool IsMoving => _timeSinceLastMove > 0f;

    protected Rigidbody2D _rb;
    protected SpriteRenderer _sr;
    protected GameEventChannel _eventChannel;
    protected Vector2 _lastMarblePosition = Vector2.zero;
    protected float _timeSinceLastMove = 0f;
    
    // Collision intensity calculation components
    protected MarbleCollisionIntensityCalculator _componentCalculator;
    protected MarbleCollisionIntensityJobManager _jobManager;

    #region MonoBehaviour methods
    private void Awake() {

      if (_eventChannel == null) {
        _eventChannel = Resources.Load<GameEventChannel>("GameEventChannel");
      }

      if (Model == null) { Model = this.gameObject; }
      //at the start we make the last position equal to its current position.
      _lastMarblePosition = Model.transform.position;

      _rb = Model.GetComponent<Rigidbody2D>();
      _sr = Model.GetComponent<SpriteRenderer>();
      
      // Initialize collision intensity calculation
      InitializeCollisionIntensityCalculation();
      
      Spawn();
    }

    void Update() {
      //if rigidBody isn't loaded, we do nothing and wait until next frame
      if (_rb == null) return;
      if (!_rb.mass.Equals(Mass)) _rb.mass = Mass;
      if (!_rb.gravityScale.Equals(GravityScale)) _rb.gravityScale = GravityScale;
      if (!_rb.angularDamping.Equals(AngularDamping)) _rb.angularDamping = AngularDamping;

      //calculate if Marble has moved since last update
      Vector2 currentMarblePosition = Model.transform.position;
      if (Vector2.Distance(currentMarblePosition, _lastMarblePosition) <= MinimalMovementThreshold) {
        _timeSinceLastMove += Time.deltaTime;
      }
      else {
        _timeSinceLastMove = 0;
      }
    }

    private void LateUpdate() {
      //last position is updated last to prevent overridings
      _lastMarblePosition = Model.transform.position;
    }

    #endregion

    protected virtual bool IsGrounded() {
      RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, GroundedMask);
      return hit.collider != null;
    }

    #region Public API

    public void Spawn() {
      if (SpawnTransform != null) {
        Model.transform.position = SpawnTransform.position;
      }
      else { Model.transform.position = Vector3.zero; }
      Model.SetActive(true);
      StopMarble();
    }

    public void StopMarble() {
      GravityScale = 0f;
      ResetForces();
    }

    public void Launch() {
      ResetForces();
      GravityScale = 1f;
      Model.SetActive(true);
      _rb.AddForce(InitialForce, ForceMode2D.Impulse);
    }

    public void ResetForces() {
      if (_rb == null) return;
      _rb.linearVelocity = Vector2.zero;
      _rb.angularVelocity = 0f;
      _rb.rotation = 0f;
    }

    public void ApplyForce(Vector2 force) {
      if(_rb != null) _rb.AddForce(force, ForceMode2D.Impulse);
    }

    #endregion

    #region Collision Intensity Management
    
    /// <summary>
    /// Initializes the collision intensity calculation system based on settings
    /// </summary>
    private void InitializeCollisionIntensityCalculation() {
      if (!EnableCollisionIntensity) return;
      
      if (UseBurstJobs) {
        // Use Burst Jobs approach for high performance
        _jobManager = Model.GetComponent<MarbleCollisionIntensityJobManager>();
        if (_jobManager == null) {
          _jobManager = Model.AddComponent<MarbleCollisionIntensityJobManager>();
        }
        
        // Remove component calculator if it exists
        if (_componentCalculator != null) {
          DestroyImmediate(_componentCalculator);
          _componentCalculator = null;
        }
      } else {
        // Use component-based approach for baseline comparison
        _componentCalculator = Model.GetComponent<MarbleCollisionIntensityCalculator>();
        if (_componentCalculator == null) {
          _componentCalculator = Model.AddComponent<MarbleCollisionIntensityCalculator>();
        }
        
        // Remove job manager if it exists
        if (_jobManager != null) {
          DestroyImmediate(_jobManager);
          _jobManager = null;
        }
      }
    }
    
    /// <summary>
    /// Switches between component-based and job-based collision intensity calculation
    /// </summary>
    public void SwitchCollisionIntensityMethod(bool useBurstJobs) {
      if (UseBurstJobs == useBurstJobs) return;
      
      UseBurstJobs = useBurstJobs;
      InitializeCollisionIntensityCalculation();
    }
    
    /// <summary>
    /// Gets collision intensity statistics from the active calculator
    /// </summary>
    public CollisionIntensityStats GetCollisionIntensityStats() {
      if (!EnableCollisionIntensity) return new CollisionIntensityStats();
      
      if (UseBurstJobs && _jobManager != null) {
        return new CollisionIntensityStats {
          TotalCollisions = _jobManager.TotalCollisionsProcessed,
          AverageIntensity = _jobManager.AverageIntensity,
          CurrentFrameCollisions = _jobManager.CurrentFrameCollisions,
          LastExecutionTime = _jobManager.LastJobExecutionTime,
          UsingBurstJobs = true
        };
      } else if (!UseBurstJobs && _componentCalculator != null) {
        return new CollisionIntensityStats {
          TotalCollisions = _componentCalculator.CollisionCount,
          AverageIntensity = _componentCalculator.AverageIntensity,
          CurrentFrameCollisions = 0, // Component doesn't track per-frame
          LastExecutionTime = 0f, // Component doesn't track execution time
          UsingBurstJobs = false
        };
      }
      
      return new CollisionIntensityStats();
    }
    
    /// <summary>
    /// Resets collision intensity statistics
    /// </summary>
    public void ResetCollisionIntensityStats() {
      if (_jobManager != null) _jobManager.ResetStatistics();
      if (_componentCalculator != null) _componentCalculator.ResetStatistics();
    }
    
    #endregion

  }
  
  /// <summary>
  /// Data structure for collision intensity statistics
  /// </summary>
  [System.Serializable]
  public struct CollisionIntensityStats {
    public int TotalCollisions;
    public float AverageIntensity;
    public int CurrentFrameCollisions;
    public float LastExecutionTime;
    public bool UsingBurstJobs;
  }
}