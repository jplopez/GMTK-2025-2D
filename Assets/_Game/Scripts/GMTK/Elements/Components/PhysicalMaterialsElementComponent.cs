using UnityEngine;

namespace GMTK {
  /// <summary>
  /// Material component for PlayableElement that handles physics material properties.
  /// This component manages friction, bounciness, and physics materials for collision interactions.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Physical Material Element Component")]
  public class PhysicalMaterialsElementComponent : PlayableElementComponent {

    [Header("Material Settings")]
    [Tooltip("Friction the element puts on the Marble. High=slows down the marble. Mid=keeps the current speed. Low=smooth surface, Marble will gain speed")]
    public FrictionLevel Friction = FrictionLevel.Mid;
    [Tooltip("Bounce experienced by the Marble when colliding with this element. High=The Marble bounces and gains more speed. Mid=regular bounce, no force added to the ball. Low=minimum bouncing, no force added to the ball")]
    public BouncinessLevel Bounciness = BouncinessLevel.Mid;
    [Tooltip("Optional override for experimentation. If set, this will be used instead of auto-assigned material.")]
    public PhysicsMaterial2D OverrideMaterial;

    [Header("Advanced Material Settings")]
    [Tooltip("If true, material will be updated in real-time when values change")]
    public bool AutoUpdateMaterial = true;
    [Tooltip("If true, the material will affect the element's own movement (self-friction)")]
    public bool ApplyToSelf = true;

    [Header("Debug")]
    [SerializeField, DisplayWithoutEdit] private Rigidbody2D _rigidbody2D;
    [SerializeField, DisplayWithoutEdit] private Collider2D _collider2D;
    [SerializeField, DisplayWithoutEdit] private PhysicsMaterial2D _assignedMaterial;
    [SerializeField, DisplayWithoutEdit] private FrictionLevel _lastFriction;
    [SerializeField, DisplayWithoutEdit] private BouncinessLevel _lastBounciness;

    protected override void Initialize() {
      // Get required components
      _rigidbody2D = _playableElement.GetComponent<Rigidbody2D>();
      _collider2D = _playableElement.GetComponent<Collider2D>();

      if (_collider2D == null) {
        Debug.LogWarning($"[PlayableElementMaterial] No Collider2D found on {_playableElement.name}. Material component requires a collider to function.");
        return;
      }

      // Store initial values for change detection
      _lastFriction = Friction;
      _lastBounciness = Bounciness;

      // Apply initial material
      UpdateMaterial();
    }

    protected override bool Validate() => _collider2D != null;

    protected override void OnUpdate() {
      // Check for material changes if auto-update is enabled
      if (AutoUpdateMaterial && (_lastFriction != Friction || _lastBounciness != Bounciness)) {
        UpdateMaterial();
        _lastFriction = Friction;
        _lastBounciness = Bounciness;
      }
    }

    private void UpdateMaterial() {
      if (_collider2D == null) return;

      // Use override material if provided, otherwise generate from friction/bounciness
      if (OverrideMaterial != null) {
        _assignedMaterial = OverrideMaterial;
      }
      else {
        _assignedMaterial = SnappableMaterialStrategy.GetMaterial(Friction, Bounciness);
      }

      // Apply material to collider
      _collider2D.sharedMaterial = _assignedMaterial;

      // Apply to rigidbody if it exists and ApplyToSelf is true
      if (_rigidbody2D != null && ApplyToSelf) {
        _rigidbody2D.sharedMaterial = _assignedMaterial;
      }

      DebugLog($"Material updated: Friction={Friction}, Bounciness={Bounciness}, Material={_assignedMaterial?.name ?? "NULL"}");
    }

    // Public API
    public void SetFrictionLevel(FrictionLevel level) {
      Friction = level;
      if (!AutoUpdateMaterial) {
        UpdateMaterial();
      }
    }

    public void SetBouncinessLevel(BouncinessLevel level) {
      Bounciness = level;
      if (!AutoUpdateMaterial) {
        UpdateMaterial();
      }
    }

    public void SetMaterialProperties(FrictionLevel friction, BouncinessLevel bounciness) {
      Friction = friction;
      Bounciness = bounciness;
      if (!AutoUpdateMaterial) {
        UpdateMaterial();
      }
    }

    public void SetOverrideMaterial(PhysicsMaterial2D material) {
      OverrideMaterial = material;
      UpdateMaterial();
    }

    public void ClearOverrideMaterial() {
      OverrideMaterial = null;
      UpdateMaterial();
    }

    public PhysicsMaterial2D GetCurrentMaterial() => _assignedMaterial;

    public void ForceUpdateMaterial() {
      UpdateMaterial();
    }

    // Material presets
    public void SetIcyMaterial() {
      SetMaterialProperties(FrictionLevel.Low, BouncinessLevel.Low);
    }

    public void SetRubberMaterial() {
      SetMaterialProperties(FrictionLevel.High, BouncinessLevel.High);
    }

    public void SetStickyMaterial() {
      SetMaterialProperties(FrictionLevel.High, BouncinessLevel.Low);
    }

    public void SetBouncyMaterial() {
      SetMaterialProperties(FrictionLevel.Mid, BouncinessLevel.High);
    }

    public void SetNormalMaterial() {
      SetMaterialProperties(FrictionLevel.Mid, BouncinessLevel.Mid);
    }

    // Material property queries
    public bool IsSlippery() => Friction == FrictionLevel.Low;
    public bool IsSticky() => Friction == FrictionLevel.High;
    public bool IsBouncy() => Bounciness == BouncinessLevel.High;
    public bool IsAbsorbing() => Bounciness == BouncinessLevel.Low;

    protected override void ResetComponent() {
      // Reset to default values
      Friction = FrictionLevel.Mid;
      Bounciness = BouncinessLevel.Mid;
      OverrideMaterial = null;
      UpdateMaterial();
    }

    protected override void FinalizeComponent() {
      // Clean up material references
      if (_collider2D != null) {
        _collider2D.sharedMaterial = null;
      }
      if (_rigidbody2D != null) {
        _rigidbody2D.sharedMaterial = null;
      }
    }

    // Legacy compatibility handlers
    protected override void HandleElementSelected(GridSnappableEventArgs evt) {
      // Could add special material effects during selection
    }

    protected override void HandleElementDropped(GridSnappableEventArgs evt) {
      // Could add impact effects based on material
    }

    protected override void HandleElementHovered(GridSnappableEventArgs evt) {
      // Could add hover effects based on material
    }

    protected override void HandleElementUnhovered(GridSnappableEventArgs evt) {
      // Could remove hover effects
    }

    private void DebugLog(string message) {
      Debug.Log($"[PlayableElementMaterial] {message}");
    }

    // Gizmos for debugging material properties
    void OnDrawGizmosSelected() {
      if (_playableElement == null || _collider2D == null) return;

      Vector3 pos = _playableElement.SnapTransform.position;
      Vector3 size = _collider2D.bounds.size;

      // Color code based on friction
      switch (Friction) {
        case FrictionLevel.Low:
          Gizmos.color = Color.cyan; // Icy/slippery
          break;
        case FrictionLevel.Mid:
          Gizmos.color = Color.white; // Normal
          break;
        case FrictionLevel.High:
          Gizmos.color = Color.red; // Sticky
          break;
      }

      // Draw wireframe with friction color
      Gizmos.DrawWireCube(pos, size);

      // Add bounciness indicator
      if (Bounciness == BouncinessLevel.High) {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(pos, 0.2f);
      }
      else if (Bounciness == BouncinessLevel.Low) {
        Gizmos.color = Color.gray;
        Gizmos.DrawCube(pos, Vector3.one * 0.2f);
      }

      // Show override material indicator
      if (OverrideMaterial != null) {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, 0.5f);
      }
    }
  }
}