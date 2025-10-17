
namespace Ameba.MoreMountains.Feel {
  public interface ICalculationFactor {

    float GetFactorValue();
    float MaxValue { get; set; }
    float MinValue { get; set; }
    bool Validate();
  }

}