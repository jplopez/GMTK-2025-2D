using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ameba.Input {

  /// <summary>
  /// <para>
  /// Represents a _registry for input actions within a single Action Map.
  /// It dispatches input events (e.g., button presses, clicks, touches) to corresponding <see cref="InputBinding"/> instances.
  /// Each <see cref="InputBinding"/> can handle all input phases: <c>Started</c>, <c>Performed</c>, and <c>Canceled</c>.
  /// </para>
  ///
  /// <para>
  /// <see cref="InputActionRegistry"/> is scoped to a single Action Map, but bindings can be reused across multiple registries.
  /// For example, a "Pause" action might behave the same in both gameplay and menu contexts. You can reuse the same <see cref="InputBinding"/> instance.
  /// </para>
  ///
  /// <para>
  /// You can also use a centralized class with method-based bindings for each action. This approach is ideal for small projects or rapid prototyping. See <see cref="InputHandlerBase"/>.
  /// </para>
  /// </summary>
  [CreateAssetMenu(menuName = "Ameba/Input/Action Map Registry", order = 1)]
  public class InputActionRegistry : ScriptableObject {

    [Header("Action Map")]
    [Tooltip("name of the ActionMap this _registry represents")]
    public string ActionMapName;
    [Tooltip("Registered bindings")]
    public List<InputBinding> Bindings;

    private Dictionary<string, InputBinding> _bindingLookup;

    [ContextMenu("Initialize")]
    public void Initialize() {
      _bindingLookup ??= new Dictionary<string, InputBinding>();
      _bindingLookup.Clear();
      foreach (var b in Bindings) _bindingLookup[b.ActionName] = b;
    }

    public bool ContainsAction(string actionName) => _bindingLookup.TryGetValue(actionName, out _);

    private void OnEnable() {
      if (_bindingLookup == null || _bindingLookup.Count == 0)
        Initialize();
    }

    public void Handle(string actionName, InputAction.CallbackContext context) {
      if (_bindingLookup.TryGetValue(actionName, out var binding)) {
        binding.Invoke(context);
      }
    }

    public void Bind(string actionName,
                 UnityEvent<InputAction.CallbackContext> onStarted = null,
                 UnityEvent<InputAction.CallbackContext> onPerformed = null,
                 UnityEvent<InputAction.CallbackContext> onCanceled = null) {
      if (string.IsNullOrEmpty(actionName)) throw new ArgumentException($"actionName cannot be null or empty string");
      if (_bindingLookup.TryGetValue(actionName, out var binding)) {
        binding.Started = onStarted;
        binding.Performed = onPerformed;
        binding.Canceled = onCanceled;
      } else {
        Debug.LogWarning($"Bind Failed: Registry for '{ActionMapName}' doesn't have the '{actionName}' action");
      }
    }

    public void Bind(string actionName, InputActionPhase phase, UnityEvent<InputAction.CallbackContext> handler) {
      if(handler == null ) {
        Debug.LogWarning("Bind Failed: UnityEvent can't be null");
        return;
      }
      if (_bindingLookup.TryGetValue(actionName, out var binding)) {
        switch (phase) {
          case InputActionPhase.Started: binding.Started = handler; break;
          case InputActionPhase.Performed: binding.Performed = handler; break;
          case InputActionPhase.Canceled: binding.Canceled = handler; break;
        }
      } else {
        Debug.LogWarning($"Bind Failed: Registry for '{ActionMapName}' doesn't have the '{actionName}' action");
      }
    }

    public void Bind(string actionName, InputActionPhase[] phases, Action<InputAction.CallbackContext> handler) {
      if (_bindingLookup.TryGetValue(actionName, out var binding)) {
        foreach (var phase in phases) {
          UnityEvent<InputAction.CallbackContext> unityHandler = new();
          unityHandler.AddListener(new UnityAction<InputAction.CallbackContext>(handler));
          Bind(actionName, phase, unityHandler);
        }
      }
    }


  }
}