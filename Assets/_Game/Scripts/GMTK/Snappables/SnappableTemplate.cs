using UnityEngine;

namespace GMTK {
  public enum FrictionLevel { Low, Mid, High }
  public enum BouncinessLevel { Low, Mid, High }

  public enum PivotDirectionTypes {
    Center,
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest
  }

  [CreateAssetMenu(fileName = "SnappableTemplate", menuName = "GMTK/Grid/Snappable Template")]
  public class SnappableTemplate : ScriptableObject {

    [Header("Sprite")]
    public Sprite Sprite;
    [Tooltip("Size of the object in grid units (1 unit = 1 cell).")]
    public Vector2Int SizeInGridUnits = Vector2Int.one;

    [Header("Physics")]
    [Tooltip("If true, the object will not rotate when colliding with other objects.")]
    public bool ForceStaticRigidBody = true;
    public PivotDirectionTypes PivotDirection = PivotDirectionTypes.Center;


    [Header("RigidBody Settings (only when ForceStaticRigidBody is false)")]
    [Tooltip("Mass of the object. Higher mass means more inertia.")]
    public float Mass = 1f;
    [Tooltip("Linear drag of the object. Higher values mean more resistance to movement.")]
    public float AngularDamping = 0.5f;
    [Tooltip("If false, the object will not be affected by gravity.")]
    public bool Gravity = true;

    [Header("Physics Material")]
    [Tooltip("Friction level of the object. Higher friction means more resistance to sliding.")]
    public FrictionLevel Friction = FrictionLevel.Mid;
    [Tooltip("Bounciness level of the object. Higher bounciness means more rebound on collision.")]
    public BouncinessLevel Bounciness = BouncinessLevel.Mid;

  }

}