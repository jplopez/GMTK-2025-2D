using UnityEngine;
using UnityEngine.Events;

namespace GMTK {


  public abstract class RuntimeValue<T> : ScriptableObject {

    public UnityEvent<T> OnValueChanged;

    public bool ResetOnDisable = true;
    public abstract T Value { get; set; }

    private void OnDisable() { if (ResetOnDisable) Reset(); }

    public abstract void Reset();

    public override string ToString() => Value.ToString();

  }


  [CreateAssetMenu(fileName = "FloatVariable", menuName = "GMTK/Variables/FloatVariable")]
  public class FloatVariable : RuntimeValue<float> {
    public override float Value {
      get => _value;
      set {
        if (_value != value) {
          _value = value;
          OnValueChanged?.Invoke(_value);
        }
      }
    }

    protected float _value = 0f;

    public void Add(float amount) {
      Value += amount;
      OnValueChanged?.Invoke(Value);
    }
    public void Subtract(float amount) {
      Value -= amount;
      OnValueChanged?.Invoke(Value);
    }
    public void Multiply(float factor) {
      Value *= factor;
      OnValueChanged?.Invoke(Value);
    }
    public void Divide(float divisor) {
      if (divisor != 0f) {
        Value /= divisor;
        OnValueChanged?.Invoke(Value);
      }
      else {
        Debug.LogWarning("Attempted to divide by zero in FloatVariable.");
      }
    }

    public void Clamp(float min, float max) {
      Value = Mathf.Clamp(Value, min, max);
      OnValueChanged?.Invoke(Value);
    }

    public override void Reset() {
      Value = 0f;
      OnValueChanged?.Invoke(Value);
    }

  }

}