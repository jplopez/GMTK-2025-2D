namespace GMTK {

  /// <summary>
  /// Discrete parametrization of Physical material bounciness for playable elements and marble.<br/>
  /// The enum value represents the percentage of intensity
  /// </summary>
  public enum BouncinessLevel {
    Low = 90, Mid = 100, High = 120
  }

  public static class BouncinessExtensions {
    public static float NormalizedValue(this BouncinessLevel level) {
      return (float)level / 100f;
    }
  }

}