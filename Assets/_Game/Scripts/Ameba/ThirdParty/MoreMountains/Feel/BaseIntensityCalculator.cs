using System.Collections.Generic;
using System.Linq;

namespace Ameba.MoreMountains.Feel {
  /// <summary>
  /// Base implementation for IFeedbackIntensityCalculators.
  /// </summary>
  public class BaseIntensityCalculator : IFeedbackIntensityCalculator {
    public FeedbackIntensityMethods IntensityMethod { get; set; }

    protected List<ICalculationFactor> _calculationFactors = new();
    public List<ICalculationFactor> CalculationFactors => _calculationFactors;

    public float ComputeIntensity() {
      if(_calculationFactors.Count == 0) {
        return 0f;
      }
      float[] calculatedFactors = _calculationFactors.ConvertAll(factor => factor.Validate() ? factor.GetFactorValue() : 0f).ToArray();
      return ComputeIntensityByMethod(calculatedFactors, IntensityMethod);
    }

    protected virtual float ComputeIntensityByMethod(float[] calculatedFactors, FeedbackIntensityMethods method) {
      return method switch {
        FeedbackIntensityMethods.Average => calculatedFactors.Average(),
        FeedbackIntensityMethods.Multiply => calculatedFactors.Aggregate(1f, (acc, val) => acc * val),
        FeedbackIntensityMethods.Minimum => calculatedFactors.Min(),
        FeedbackIntensityMethods.Maximum => calculatedFactors.Max(),
        _ => 0f
      };
    }

    public void AddCalculationFactor(ICalculationFactor factor) => _calculationFactors.Add(factor);

    public void ClearCalculationFactors() => _calculationFactors.Clear();

    public void RemoveCalculationFactor(ICalculationFactor factor) => _calculationFactors.Remove(factor);
  }
}