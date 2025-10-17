using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;

namespace Ameba.MoreMountains.Feedbacks {
  /// <summary>
  /// This feedback allows you to hold values (numbers, strings, booleans) that can then be used by other feedbacks 
  /// to automatically set their properties. It doesn't do anything when played.
  /// </summary>
  [AddComponentMenu("")]
  [FeedbackHelp("This feedback allows you to hold values (numbers, strings, booleans) that can then be used by other feedbacks to automatically set their properties. It doesn't do anything when played.")]
  [MovedFrom(false, null, "MoreMountains.Feedbacks")]
  [FeedbackPath("Feedbacks/MMF Value Holder")]
  public class ValueHolderFeedback : MMF_Feedback {
    /// a static bool used to disable all feedbacks of this type at once
    public static bool FeedbackTypeAuthorized = true;
    /// sets the inspector color for this feedback
#if UNITY_EDITOR
    public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.FeedbacksColor; } }
    public override Color DisplayColor { get { return MMFeedbacksInspectorColors.FeedbacksColor.MMDarken(0.15f); } }
    public override string RequiredTargetText => !string.IsNullOrEmpty(Label) ? Label : "Unnamed";
#endif
    /// the duration of this feedback is 0
    public override float FeedbackDuration => 0f;
    public override bool DisplayFullHeaderColor => true;

    [MMFInspectorGroup("Value Holder", true, 37, true)]
    /// whether or not to force this value holder on all compatible feedbacks in the MMF Player's list
    [Tooltip("whether or not to force this value holder on all compatible feedbacks in the MMF Player's list")]
    public bool ForceValueOnAll = false;

    [MMFInspectorGroup("Value Source", true, 38, true)]
    /// the source of the value injection
    [Tooltip("The source of the value injection")]
    public ValueSource Source = ValueSource.PredefinedValue;

    /// the type of values this holder contains (for predefined values)
    [Tooltip("The type of values this holder contains")]
    [MMFEnumCondition("Source", (int)ValueSource.PredefinedValue)]
    public ValueType SelectedValueType = ValueType.Float;

    /// when to update the values
    [Tooltip("When to update the values")]
    [MMFEnumCondition("Source", (int)ValueSource.ComponentProperty)]
    public UpdateTiming UpdateTiming = UpdateTiming.OnInitialization;

    /// delay between updates when using UpdateTiming.DuringUpdate
    [Tooltip("Delay in seconds between value updates")]
    [MMFEnumCondition("UpdateTiming", (int)UpdateTiming.DuringUpdate)]
    public float UpdateDelay = 1f;

    [MMFInspectorGroup("Component Reference", true, 39, true)]
    /// target GameObject containing the component
    [Tooltip("Target GameObject containing the component")]
    //[MMFEnumCondition("Source", (int)ValueSource.ComponentProperty)]
    public GameObject TargetGameObject;

    /// name of the component type
    [Tooltip("Name of the component type (e.g., 'Transform', 'Rigidbody')")]
    //[MMFEnumCondition("Source", (int)ValueSource.ComponentProperty)]
    public string ComponentName;

    /// name of the property to read
    [Tooltip("Name of the property to read (e.g., 'position', 'velocity')")]
    //[MMFEnumCondition("Source", (int)ValueSource.ComponentProperty)]
    public string PropertyName;

    /// name to use for the injected value
    [Tooltip("Name to use for the injected value")]
    //[MMFEnumCondition("Source", (int)ValueSource.ComponentProperty)]
    public string ValueName = "ComponentValue";

    [MMFInspectorGroup("Predefined Values", true, 40, true)]
    /// float values that can be injected into other feedbacks
    [Tooltip("Float values that can be injected into other feedbacks")]
    //[MMFEnumCondition("SelectedValueType", (int)ValueType.Float)]
    public List<MMFValuePair<float>> FloatValues = new();

    /// int values that can be injected into other feedbacks
    [Tooltip("Int values that can be injected into other feedbacks")]
    //[MMFEnumCondition("SelectedValueType", (int)ValueType.Int)]
    public List<MMFValuePair<int>> IntValues = new();

    /// bool values that can be injected into other feedbacks
    [Tooltip("Bool values that can be injected into other feedbacks")]
    //[MMFEnumCondition("SelectedValueType", (int)ValueType.Bool)]
    public List<MMFValuePair<bool>> BoolValues = new();

    /// string values that can be injected into other feedbacks
    [Tooltip("String values that can be injected into other feedbacks")]
    //[MMFEnumCondition("SelectedValueType", (int)ValueType.String)]
    public List<MMFValuePair<string>> StringValues = new();

    // Runtime fields
    private Component _targetComponent;
    private PropertyInfo _targetProperty;
    private FieldInfo _targetField;
    private Coroutine _updateCoroutine;
    private object _lastComponentValue;

    /// <summary>
    /// On init we force our values on all feedbacks if needed and setup dynamic value injection
    /// </summary>
    /// <param name="owner"></param>
    protected override void CustomInitialization(MMF_Player owner) {
      base.CustomInitialization(owner);

      // Setup component reference if using ComponentProperty source
      if (Source == ValueSource.ComponentProperty) {
        SetupComponentReference();
        if (UpdateTiming == UpdateTiming.OnInitialization) {
          UpdateValueFromComponent();
        }
      }

      if (ForceValueOnAll) {
        for (int index = 0; index < Owner.FeedbacksList.Count; index++) {
          if (Owner.FeedbacksList[index] is IValueReceiver valueReceiver && valueReceiver.HasAutomatedValueInjection) {
            Owner.FeedbacksList[index].SetIndexInFeedbacksList(index);
            valueReceiver.ForcedValueHolder = this;
            valueReceiver.ForceAutomateValueInjection();
          }
        }
      }
    }

    /// <summary>
    /// Setup component reference for dynamic value injection
    /// </summary>
    private void SetupComponentReference() {
      if (TargetGameObject == null || string.IsNullOrEmpty(ComponentName) || string.IsNullOrEmpty(PropertyName)) {
        return;
      }

      // Find component by name
      _targetComponent = TargetGameObject.GetComponent(ComponentName);
      if (_targetComponent == null) {
        Debug.LogWarning($"[ValueHolderFeedback] Component '{ComponentName}' not found on '{TargetGameObject.name}'");
        return;
      }

      // Try to find property first, then field
      Type componentType = _targetComponent.GetType();
      _targetProperty = componentType.GetProperty(PropertyName, BindingFlags.Public | BindingFlags.Instance);
      if (_targetProperty == null) {
        _targetField = componentType.GetField(PropertyName, BindingFlags.Public | BindingFlags.Instance);
        if (_targetField == null) {
          Debug.LogWarning($"[ValueHolderFeedback] Property or field '{PropertyName}' not found on component '{ComponentName}'");
          return;
        }
      }
    }

    /// <summary>
    /// Update value from component property/field
    /// </summary>
    private void UpdateValueFromComponent() {
      if (_targetComponent == null) {
        return;
      }

      object value = null;
      if (_targetProperty != null && _targetProperty.CanRead) {
        value = _targetProperty.GetValue(_targetComponent);
      }
      else if (_targetField != null) {
        value = _targetField.GetValue(_targetComponent);
      }

      if (value != null && !Equals(value, _lastComponentValue)) {
        _lastComponentValue = value;
        InjectComponentValue(value);
      }
    }

    /// <summary>
    /// Inject component value into appropriate value collection
    /// </summary>
    private void InjectComponentValue(object value) {
      if (string.IsNullOrEmpty(ValueName)) {
        return;
      }

      // Remove existing value with same name
      RemoveValueByName(ValueName);

      // Add new value based on type
      switch (value) {
        case float floatVal:
          FloatValues.Add(new MMFValuePair<float>(ValueName, floatVal));
          break;
        case int intVal:
          IntValues.Add(new MMFValuePair<int>(ValueName, intVal));
          break;
        case bool boolVal:
          BoolValues.Add(new MMFValuePair<bool>(ValueName, boolVal));
          break;
        case string stringVal:
          StringValues.Add(new MMFValuePair<string>(ValueName, stringVal));
          break;
        case Vector2 vec2:
          // Convert Vector2 to string representation
          StringValues.Add(new MMFValuePair<string>(ValueName, $"{vec2.x},{vec2.y}"));
          break;
        case Vector3 vec3:
          // Convert Vector3 to string representation  
          StringValues.Add(new MMFValuePair<string>(ValueName, $"{vec3.x},{vec3.y},{vec3.z}"));
          break;
        default:
          // Fallback to string representation
          StringValues.Add(new MMFValuePair<string>(ValueName, value.ToString()));
          break;
      }
    }

    /// <summary>
    /// Remove value by name from all collections
    /// </summary>
    private void RemoveValueByName(string valueName) {
      FloatValues.RemoveAll(v => v.Name.Equals(valueName, StringComparison.OrdinalIgnoreCase));
      IntValues.RemoveAll(v => v.Name.Equals(valueName, StringComparison.OrdinalIgnoreCase));
      BoolValues.RemoveAll(v => v.Name.Equals(valueName, StringComparison.OrdinalIgnoreCase));
      StringValues.RemoveAll(v => v.Name.Equals(valueName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Coroutine for periodic updates
    /// </summary>
    private IEnumerator UpdateCoroutine() {
      while (true) {
        yield return new WaitForSeconds(UpdateDelay);
        UpdateValueFromComponent();
      }
    }

    /// <summary>
    /// Gets a float value by name
    /// </summary>
    public bool TryGetFloatValue(string valueName, out float value) {
      value = 0f;
      if (Source == ValueSource.PredefinedValue && SelectedValueType != ValueType.Float) return false;

      var valuePair = FloatValues.Find(v => v.Name.Equals(valueName, StringComparison.OrdinalIgnoreCase));
      if (valuePair != null) {
        value = valuePair.Value;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Gets an int value by name
    /// </summary>
    public bool TryGetIntValue(string valueName, out int value) {
      value = 0;
      if (Source == ValueSource.PredefinedValue && SelectedValueType != ValueType.Int) return false;

      var valuePair = IntValues.Find(v => v.Name.Equals(valueName, StringComparison.OrdinalIgnoreCase));
      if (valuePair != null) {
        value = valuePair.Value;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Gets a bool value by name
    /// </summary>
    public bool TryGetBoolValue(string valueName, out bool value) {
      value = false;
      if (Source == ValueSource.PredefinedValue && SelectedValueType != ValueType.Bool) return false;

      var valuePair = BoolValues.Find(v => v.Name.Equals(valueName, StringComparison.OrdinalIgnoreCase));
      if (valuePair != null) {
        value = valuePair.Value;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Gets a string value by name
    /// </summary>
    public bool TryGetStringValue(string valueName, out string value) {
      value = string.Empty;
      if (Source == ValueSource.PredefinedValue && SelectedValueType != ValueType.String) return false;

      var valuePair = StringValues.Find(v => v.Name.Equals(valueName, StringComparison.OrdinalIgnoreCase));
      if (valuePair != null) {
        value = valuePair.Value;
        return true;
      }
      return false;
    }

    /// <summary>
    /// On Play we handle timing-based updates
    /// </summary>
    protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f) {
      if (Source == ValueSource.ComponentProperty) {
        switch (UpdateTiming) {
          case UpdateTiming.OnPlayFeedbacks:
            UpdateValueFromComponent();
            break;
          case UpdateTiming.DuringUpdate:
            if (_updateCoroutine != null) {
              Owner.StopCoroutine(_updateCoroutine);
            }
            _updateCoroutine = Owner.StartCoroutine(UpdateCoroutine());
            break;
        }
      }
    }

    /// <summary>
    /// Stop any running update coroutines when feedback stops
    /// </summary>
    protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f) {
      if (_updateCoroutine != null) {
        Owner.StopCoroutine(_updateCoroutine);
        _updateCoroutine = null;
      }
    }

    /// <summary>
    /// Clean up when feedback is reset
    /// </summary>
    public override void ResetFeedback() {
      base.ResetFeedback();
      if (_updateCoroutine != null) {
        Owner.StopCoroutine(_updateCoroutine);
        _updateCoroutine = null;
      }
    }
  }

  /// <summary>
  /// Enum to define the source of values in a ValueHolder
  /// </summary>
  public enum ValueSource {
    PredefinedValue,
    ComponentProperty
  }

  /// <summary>
  /// Enum to define the type of values stored in a ValueHolder
  /// </summary>
  public enum ValueType {
    Float,
    Int,
    Bool,
    String
  }

  /// <summary>
  /// Enum to define when to update component property values
  /// </summary>
  public enum UpdateTiming {
    OnInitialization,
    OnPlayFeedbacks,
    DuringUpdate
  }

  /// <summary>
  /// A serializable class to hold name-value pairs for the value holder
  /// </summary>
  [Serializable]
  public class MMFValuePair<T> {
    [Tooltip("The name/identifier for this value")]
    public string Name;
    [Tooltip("The value to store")]
    public T Value;

    public MMFValuePair() {
      Name = string.Empty;
      Value = default(T);
    }

    public MMFValuePair(string name, T value) {
      Name = name;
      Value = value;
    }
  }
}