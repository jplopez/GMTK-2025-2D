

namespace Ameba.MoreMountains.Feel {
  /// <summary>
  /// A calculation factor based on a tweened value.
  /// </summary>
  public class TweenFactor : ICalculationFactor {
    public float MaxValue { get; set; } = 1f;
    public float MinValue { get; set; } = 0f;

    protected float _tweenedValue = 0f;
    /// <summary>
    /// Sets the tweened value. Should be between 0 and 1.
    /// </summary>
    /// <param name="value">Value.</param>
    public virtual void SetTweenedValue(float value) {
      _tweenedValue = value;
    }
    /// <summary>
    /// Returns the current factor value, based on the tweened value remapped between min and max values.
    /// </summary>
    /// <returns>The factor value.</returns>
    public virtual float GetFactorValue() {
      //return MMMaths.Remap(_tweenedValue, 0f, 1f, MinValue, MaxValue);
      return MinValue + (_tweenedValue * (MaxValue - MinValue));
    }
    /// <summary>
    /// Returns true if this factor is valid (in this case, if min and max values are different).
    /// </summary>
    /// <returns>True if valid.</returns>
    public virtual bool Validate() {
      return MinValue != MaxValue;
    }
  }
}