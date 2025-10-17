using UnityEngine;

namespace Ameba.MoreMountains.Feel {

  public class ClampedFactor : ICalculationFactor {
    public float MaxValue { get; set; } = 1f;
    public float MinValue { get; set; } = 0f;
    public bool Use01Range { get; set; } = false;

    protected float _value = 0f;
    /// <summary>
    /// Sets the value. Should be between min and max values.
    /// </summary>
    /// <param name="value">Value.</param>
    public virtual void SetValue(float value) {
      _value = value;
    }
    /// <summary>
    /// Returns the current factor value, clamped between min and max values.
    /// </summary>
    /// <returns>The factor value.</returns>
    public virtual float GetFactorValue() {
      if(Use01Range) {
        _value = (_value - MinValue) / (MaxValue - MinValue);
        _value = Mathf.Clamp01(_value);
        _value = MinValue + (_value * (MaxValue - MinValue));
      } else {
        _value = Mathf.Clamp(_value, MinValue, MaxValue);
      }
      return _value;
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