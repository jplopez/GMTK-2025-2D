
namespace Ameba.MoreMountains.Feel {


  /// <summary>
  /// How to combine multiple intensity factors
  /// </summary>
  public enum FeedbackIntensityMethods {
    Average,
    Multiply,
    Minimum,
    Maximum
  }

  /// <summary>
  /// The operation to apply to a calculation factor
  /// </summary>
  public enum FactorOperation {
    Add,
    Subtract,
    Multiply,
    Divide,
    Boolean
  }
  /// <summary>
  /// An interface to be implemented by classes that can compute an intensity value for feedbacks.
  /// </summary>
  public interface IFeedbackIntensityCalculator {
    /// <summary>
    /// Computes the intensity value for feedbacks.
    /// </summary>
    /// <returns>A float representing the intensity value.</returns>
    float ComputeIntensity();
    FeedbackIntensityMethods IntensityMethod { get; set; }

    void AddCalculationFactor(ICalculationFactor factor);

    void RemoveCalculationFactor(ICalculationFactor factor);

    void ClearCalculationFactors();

    System.Collections.Generic.List<ICalculationFactor> CalculationFactors { get; }
  }



}