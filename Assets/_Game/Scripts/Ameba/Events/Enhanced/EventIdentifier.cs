using System;
using UnityEngine;

namespace Ameba.Events {

  /// <summary>
  /// Represents an event identifier that can be an int, string, or enum value.
  /// This allows for flexible event identification without requiring generics.
  /// </summary>
  [Serializable]
  public struct EventIdentifier : IEquatable<EventIdentifier> {
    
    [SerializeField] private EventIdentifierType _type;
    [SerializeField] private int _intValue;
    [SerializeField] private string _stringValue;
    [SerializeField] private string _enumTypeName;
    [SerializeField] private string _enumValueName;

    public readonly EventIdentifierType Type => _type;
    public readonly int IntValue => _intValue;
    public readonly string StringValue => _stringValue;
    public readonly string EnumTypeName => _enumTypeName;
    public readonly string EnumValueName => _enumValueName;

    // Constructors
    public EventIdentifier(int value) {
      _type = EventIdentifierType.Int;
      _intValue = value;
      _stringValue = null;
      _enumTypeName = null;
      _enumValueName = null;
    }

    public EventIdentifier(string value) {
      _type = EventIdentifierType.String;
      _intValue = 0;
      _stringValue = value ?? string.Empty;
      _enumTypeName = null;
      _enumValueName = null;
    }

    public EventIdentifier(Enum enumValue) {
      _type = EventIdentifierType.Enum;
      _intValue = 0;
      _stringValue = null;
      _enumTypeName = enumValue.GetType().AssemblyQualifiedName;
      _enumValueName = enumValue.ToString();
    }

    // Implicit conversions
    public static implicit operator EventIdentifier(int value) => new(value);
    public static implicit operator EventIdentifier(string value) => new(value);
    public static implicit operator EventIdentifier(Enum enumValue) => new(enumValue);

    // Explicit conversions
    public static explicit operator int(EventIdentifier id) => id.ToInt();
    public static explicit operator string(EventIdentifier id) => id.ToStringValue();

    public readonly int ToInt() => _type == EventIdentifierType.Int ? _intValue : throw new InvalidOperationException($"EventIdentifier is of type {_type}, not Int");
    public readonly string ToStringValue() => _type == EventIdentifierType.String ? _stringValue : throw new InvalidOperationException($"EventIdentifier is of type {_type}, not String");
    
    public readonly T ToEnum<T>() where T : Enum {
      if (_type != EventIdentifierType.Enum) {
        throw new InvalidOperationException($"EventIdentifier is of type {_type}, not Enum");
      }
      
      var targetType = typeof(T);
      if (targetType.AssemblyQualifiedName != _enumTypeName) {
        throw new InvalidOperationException($"EventIdentifier enum type {_enumTypeName} does not match requested type {targetType.AssemblyQualifiedName}");
      }
      
      return (T)Enum.Parse(typeof(T), _enumValueName);
    }

    // Equality
    public readonly bool Equals(EventIdentifier other) {
      if (_type != other._type) return false;

      return _type switch {
        EventIdentifierType.Int => _intValue == other._intValue,
        EventIdentifierType.String => _stringValue == other._stringValue,
        EventIdentifierType.Enum => _enumTypeName == other._enumTypeName && _enumValueName == other._enumValueName,
        _ => false
      };
    }

    public override readonly bool Equals(object obj) => obj is EventIdentifier other && Equals(other);

    public override readonly int GetHashCode() {
      return _type switch {
        EventIdentifierType.Int => HashCode.Combine(_type, _intValue),
        EventIdentifierType.String => HashCode.Combine(_type, _stringValue),
        EventIdentifierType.Enum => HashCode.Combine(_type, _enumTypeName, _enumValueName),
        _ => _type.GetHashCode()
      };
    }

    public static bool operator ==(EventIdentifier left, EventIdentifier right) => left.Equals(right);
    public static bool operator !=(EventIdentifier left, EventIdentifier right) => !left.Equals(right);

    public override readonly string ToString() {
      return _type switch {
        EventIdentifierType.Int => _intValue.ToString(),
        EventIdentifierType.String => _stringValue ?? "null",
        EventIdentifierType.Enum => $"{_enumTypeName?.Split('.')[^1] ?? "Unknown"}.{_enumValueName}",
        _ => "Unknown"
      };
    }
  }

  public enum EventIdentifierType {
    Int,
    String,
    Enum
  }
}