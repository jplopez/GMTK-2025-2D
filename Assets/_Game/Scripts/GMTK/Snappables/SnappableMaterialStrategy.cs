using UnityEngine;

namespace GMTK {
  public enum FrictionLevel { Low, Mid, High }
  public enum BouncinessLevel { Low, Mid, High }

  public static class SnappableMaterialStrategy {

    public const string MATERIALS_PATH = "Assets/_Game/Materials/Physics";

    public static PhysicsMaterial2D GetMaterial(FrictionLevel friction, BouncinessLevel bounce) {
      string name = $"F{friction}_B{bounce}_Material";
      return Resources.Load<PhysicsMaterial2D>($"{MATERIALS_PATH}/{name}");
    }
  }

}