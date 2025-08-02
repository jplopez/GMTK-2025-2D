using UnityEngine;

namespace GMTK {
  [CreateAssetMenu(fileName = "IntVariable", menuName = "GMTK/Variables/IntVariable")]
  public class IntVariable: RuntimeValue<int> {
    public override int Value {
      get => _value;
      set {
        if (_value != value) {
          _value = value;
          OnValueChanged?.Invoke(_value);
        }
      }
    }
    protected int _value = 0;
    public void Add(int amount) {
      Value += amount;
      OnValueChanged?.Invoke(Value);
    }
    public void Subtract(int amount) {
      Value -= amount;
      OnValueChanged?.Invoke(Value);
    }
    public void Multiply(int factor) {
      Value *= factor;
      OnValueChanged?.Invoke(Value);
    }
    public void Divide(int divisor) {
      if (divisor != 0) {
        Value /= divisor;
        OnValueChanged?.Invoke(Value);
      }
      else {
        Debug.LogWarning("Attempted to divide by zero in IntVariable.");
      }
    }
    public void Clamp(int min, int max) {
      Value = Mathf.Clamp(Value, min, max);
      OnValueChanged?.Invoke(Value);
    }
    public override void Reset() {
      Value = 0;
      OnValueChanged?.Invoke(Value);
    }
  }

}