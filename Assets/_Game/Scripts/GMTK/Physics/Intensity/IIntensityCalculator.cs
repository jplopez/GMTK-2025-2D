using UnityEngine;

namespace GMTK {


  public abstract class IntensityContext { }

  /// <summary>
  /// Interface for calculating collision intensity based on various factors.
  /// Allows for different intensity calculation algorithms to be implemented and swapped out.
  /// </summary>
  public interface IIntensityCalculator {

    /// <summary>
    /// Calculates the intensity of a collision based on the provided context.
    /// </summary>
    /// <param name="context">The collision context containing speed, fall distance, angle, etc.</param>
    /// <returns>A normalized intensity value between 0 and 1</returns>
    float CalculateIntensity(IntensityContext context);

    bool TryCalculateIntensity(IntensityContext context, out float intensity);

  }
}