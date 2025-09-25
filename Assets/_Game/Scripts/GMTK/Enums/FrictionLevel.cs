namespace GMTK {

  /// <summary>
  /// Discrete parametrization of Physical material friction for playable elements and marble.<br/>
  /// The enum value represents the percentage of intensity
  /// </summary>
  public enum FrictionLevel { Low = 80, Mid = 100, High = 120 }

  public static class FrictionExtensions {
    public static float NormalizedValue(this FrictionLevel level) {
      return (float)level / 100f;
    }
  }
}