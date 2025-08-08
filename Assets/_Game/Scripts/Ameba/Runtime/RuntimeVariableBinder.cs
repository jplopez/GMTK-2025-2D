using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Ameba.Runtime {

  public class RuntimeVariableBinder : MonoBehaviour {

    [SerializeField] private string resourcePath = ""; // Default: Resources root

    private RuntimeVariablePoller poller;

    private void Awake() => EnsurePollerExists();

    public void BindAll() {
      var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      var bindings = new List<RuntimeVariablePoller.Binding>();

      foreach (var field in fields) {
        var attr = field.GetCustomAttribute<RuntimeVariableAttribute>();
        if (attr == null) continue;

        var variable = FindVariable(attr.VariableId);
        if (variable == null) continue;

        object value = variable.GetValueRaw();
        if (TryConvert(value, field.FieldType, out object converted)) {
          field.SetValue(this, converted);

          if (attr.TwoWay) {
            bindings.Add(new RuntimeVariablePoller.Binding {
              Host = this,
              Field = field,
              Variable = variable,
              LastValue = converted
            });
          }
        }
        else {
          Debug.LogWarning($"[Binder] Failed to bind RuntimeVariable '{attr.VariableId}' to field '{field.Name}' on '{name}': incompatible types.");
        }
      }
      poller.SetBindings(bindings);
    }

    private void EnsurePollerExists() {
      if (!TryGetComponent(out poller)) {
        poller = gameObject.AddComponent<RuntimeVariablePoller>();
      }
    }

    private RuntimeVariable FindVariable(string variableId) {
      RuntimeVariableFinder.ResourcePath = resourcePath;
      var variable = RuntimeVariableFinder.FindVariable(variableId);
#if UNITY_EDITOR
      if (variable == null) {
        Debug.LogWarning($"[Binder] RuntimeVariable '{variableId}' not found in Resources/{(string.IsNullOrEmpty(resourcePath) ? "<root>" : resourcePath)}.");
      }
#endif
      return variable;
    }
    private bool TryConvert(object value, System.Type targetType, out object result) {
      try {
        if (value == null || targetType.IsAssignableFrom(value.GetType())) {
          result = value;
          return true;
        }

        result = System.Convert.ChangeType(value, targetType);
        return true;
      }
      catch {
        result = null;
        return false;
      }
    }

#if UNITY_EDITOR
    [ContextMenu("Bind All RuntimeVariables")]
    public void EditorBindAll() => BindAll();
#endif
  }

  public class RuntimeVariablePoller : MonoBehaviour {


    public class Binding {
      public MonoBehaviour Host;
      public FieldInfo Field;
      public RuntimeVariable Variable;
      public object LastValue;
    }

    private List<Binding> bindings = new();

    public void SetBindings(List<Binding> newBindings) {
      bindings = newBindings;
    }

    private static Dictionary<MonoBehaviour, List<Binding>> registry = new();

    private MonoBehaviour _host;

    public static void RegisterBinding(MonoBehaviour host, FieldInfo field, RuntimeVariable variable, object initialValue) {

      if (!registry.TryGetValue(host, out var list)) {
        list = new List<Binding>();
        registry[host] = list;
      }

      list.Add(new Binding {
        Field = field,
        Variable = variable,
        LastValue = initialValue
      });

      EnsurePollerExists(host);
    }

    private static void EnsurePollerExists(MonoBehaviour host) {
      if (!host.TryGetComponent<RuntimeVariablePoller>(out var poller)) {
        var newPoller = host.gameObject.AddComponent<RuntimeVariablePoller>();
        newPoller.Initialize(host);
      }
    }

    public void Initialize(MonoBehaviour bindingHost) { _host = bindingHost; }

    private void Update() {
      if (!registry.TryGetValue(this, out var bindings)) return;

      foreach (var binding in bindings) {
        object currentValue = binding.Field.GetValue(this);
        if (!Equals(currentValue, binding.LastValue)) {
          if (TryConvert(currentValue, binding.Variable.GetValueRaw().GetType(), out object converted)) {
            binding.Variable.SetValueRaw(converted);
            binding.LastValue = currentValue;
          }
          else {
            Debug.LogWarning($"[Poller] Failed to update RuntimeVariable '{binding.Variable.name}' from field '{binding.Field.Name}' on '{name}': incompatible types.");
          }
        }
      }
    }

    private static bool TryConvert(object value, System.Type targetType, out object result) {
      try {
        if (value == null || targetType.IsAssignableFrom(value.GetType())) {
          result = value;
          return true;
        }

        result = System.Convert.ChangeType(value, targetType);
        return true;
      }
      catch {
        result = null;
        return false;
      }
    }
  }

  public static class RuntimeVariableFinder {

    private static RuntimeVariable[] allVariables;

    public static string ResourcePath = "";
    public static RuntimeVariable FindVariable(string variableId) {
      if (allVariables == null || allVariables.Length == 0)
        allVariables = Resources.LoadAll<RuntimeVariable>(ResourcePath);

      foreach (var variable in allVariables) {
        if (variable.name == variableId)
          return variable;
      }
      return null;
    }
    public static void RefreshCache() {
      allVariables = Resources.LoadAll<RuntimeVariable>(ResourcePath);
    }
  }
}