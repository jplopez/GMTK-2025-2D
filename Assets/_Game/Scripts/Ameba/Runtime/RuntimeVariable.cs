using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ameba.Runtime {

  public class RuntimeVariable : ScriptableObject {

    protected object _value;

    public Type ValueType => _value?.GetType();

    //old and new _value
    public UnityEvent<RuntimeVariableEventArgs> OnValueChanged;

    public RuntimeVariable(object value) {
      this._value = value;
    }

    public object GetValueRaw() => _value;

    public object SetValueRaw(object value) => _value = value; 

    public T GetValue<T>() { return TypeSafeGetValue<T>(_value); }

    public void SetValue<T>(object value) { TypeSafeGetValue<T>(value); }

    public TEnum GetEnumValue<TEnum>() where TEnum : Enum {
      return TypeSafeGetValue<TEnum>(_value);
    }
    public int AsInt() => GetValue<int>();
    public float AsFloat() => GetValue<float>();
    public string AsString() => GetValue<string>();
    public bool AsBool() => GetValue<bool>();

    public void SetEnumValue<TEnum>(object rawValue) where TEnum : struct, IConvertible {
      if (!typeof(TEnum).IsEnum)
        throw new ArgumentException($"{typeof(TEnum).Name} is not an enum type");

      var oldValue = _value;
      if (rawValue is string str) {
        try {
          _value = Enum.Parse(typeof(TEnum), str);
          if(!Equals(oldValue, _value))
            OnValueChanged?.Invoke(new RuntimeVariableEventArgs(oldValue,_value));
        }
        catch {
          throw new InvalidCastException($"Cannot parse '{str}' to enum {typeof(TEnum).Name}");
        }
      }
      else if (rawValue is int intVal) {
        if (Enum.IsDefined(typeof(TEnum), intVal)) {
          _value = (TEnum)(object)intVal;
          if (!Equals(oldValue, _value))
            OnValueChanged?.Invoke(new RuntimeVariableEventArgs(oldValue, _value));
        }
        else {
          throw new InvalidCastException($"Value {intVal} is not defined in enum {typeof(TEnum).Name}");
        }
      }
      else {
        throw new InvalidCastException($"Unsupported raw _value type: {rawValue.GetType().Name}");
      }
    }


    protected virtual void TypeSafeSetValue<T>(object newValue) {
      if (newValue is T typedValue) {
        var oldValue = _value;
        _value = typedValue;
        if (!Equals(oldValue, _value))
          OnValueChanged?.Invoke(new RuntimeVariableEventArgs(oldValue, _value));
      }
      else {
        throw new InvalidCastException($"RuntimeVariable: Cannot cast {newValue?.GetType().Name} to {typeof(T).Name}");
      }
    }

    protected virtual T TypeSafeGetValue<T>(object storedValue) {
      if (storedValue is T typedValue) {
        return typedValue;
      }
      else {
        throw new InvalidCastException($"RuntimeVariable: Stored _value is of type {storedValue?.GetType().Name}, not {typeof(T).Name}");
      }
    }

  }

  [Serializable]
  public class RuntimeVariableEventArgs : EventArgs {
    public object OldValue { get; }
    public object NewValue { get; }

    public RuntimeVariableEventArgs(object oldValue, object newValue) {
      this.OldValue = oldValue;
      this.NewValue = newValue;
    }

    public Type ValueType => NewValue?.GetType();
    public bool HasChanged => !Equals(OldValue, NewValue);
    public override string ToString() =>
        $"Changed from {OldValue} ({OldValue?.GetType().Name}) to {NewValue} ({NewValue?.GetType().Name})";
  }
}