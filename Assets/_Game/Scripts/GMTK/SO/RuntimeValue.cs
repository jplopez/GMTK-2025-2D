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

}