
namespace GMTK {
  public class CollisionIntensityContext : IntensityContext {
    public float Mass { get; set; } = 1f;
    public float Velocity { get; set; } = 0f;
    public float FallDistance { get; set; } = 0f;
    public float CollisionAngle { get; set; } = 0f; // 0 = parallel, 90 = perpendicular
    public CollisionType CollisionType { get; set; } = CollisionType.Generic;
    public MaterialProperties Material { get; set; } = new MaterialProperties();

    public float MaterialIntensityMultiplier {
      get {
        if (!Material.HasCustomProperties) return 1f;
        return Material.Friction.NormalizedValue() * Material.Bounciness.NormalizedValue();
      }
    }

  }
}