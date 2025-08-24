using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ameba.Input {
  public abstract class InputActionBinder<T> {

    public const string SET_CALLBACK_METHOD_NAME = "AddCallbacks";
    public const string ENABLE_METHOD_NAME = "Enable";
    public const string DISABLE_METHOD_NAME = "Disable";

    protected InputActionRegistry _registry;
    protected T _actionMap;

    public virtual void Initialize(InputActionRegistry registry, T actionMap) {
      _registry = registry;
      _actionMap = actionMap;
      _registry.Initialize();
      RegisterTaggedHandlers();
      SetCallbacks();
      EnableActionMap();
    }

    public virtual void SetCallbacks() {
      // Set callbacks if supported
      var setCallbacksMethod = typeof(T).GetMethod(SET_CALLBACK_METHOD_NAME);
      setCallbacksMethod?.Invoke(_actionMap, new object[] { this });
    }

    public virtual void EnableActionMap() {
      // Enable the map
      var enableMethod = typeof(T).GetMethod(ENABLE_METHOD_NAME);
      enableMethod?.Invoke(_actionMap, null);
    }

    public virtual void DisableActionMap() {
      var disableMethod = typeof(T).GetMethod(DISABLE_METHOD_NAME);
      disableMethod?.Invoke(_actionMap, null);
    }

    private void RegisterTaggedHandlers() {
      var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
      foreach (var method in methods) {
        var attributes = method.GetCustomAttributes(typeof(InputHandlerAttribute), true);
        foreach (InputHandlerAttribute attr in attributes.Cast<InputHandlerAttribute>()) {
          var del = (Action<InputAction.CallbackContext>)Delegate.CreateDelegate(
              typeof(Action<InputAction.CallbackContext>), this, method, false);

          if (del != null) {
            try {
              _registry.Bind(attr.ActionName, attr.Phases, del);
            }
            catch (Exception e) {
              Debug.LogError($"Error binding {attr.ActionName}: {e.Message}");
#if UNITY_EDITOR
              throw;
#endif
            }
          }
        }
      }
    }
  }
}