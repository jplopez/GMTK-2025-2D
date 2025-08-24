using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ameba.Input {

  /// <summary>
  /// <para>
  /// This Monobehaviour is intended to be extended by classes that want to use the <see cref="InputActionRegistry"/>
  /// in combination with Unity's auto-generated wrapper class to interact with Input Sytem.
  ///</para>
  ///<para>
  /// The wrapper provides interfaces for each action map, declaring methods for all actions defined in the map.  
  /// These interfaces can work quite well with <see cref="InputActionRegistry"/>, because is also scoped at the action map
  /// and registries use the action names as keys. 
  /// </para>
  /// <para>
  /// Let's say you have an Action Map called <c>'Player'</c>, Unity will generate the <c>IPlayerActions</c> interface.
  /// You can implement the <c>IPlayerActions</c> interface on a class that also extends from <see cref="InputHandlerBase"/>. Example:
  /// </para>
  /// 
  /// <example>
  /// <code>
  /// public abstract class PlayerInputBase : InputHandlerBase, InputActions.IPlayerActions {
  /// 
  ///   //Implementation of IPlayerActions interface. Each method maps to an Action defined in
  ///   //the input system. All we do is dispatching the calls to InputActionRegistry (_registry field)
  ///   //using the same name and context. 
  ///   
  ///   public void OnJump(InputAction.CallbackContext context) => _registry.Handle("Jump", context);
  ///   public void OnMove(InputAction.CallbackContext context) => _registry.Handle("Move", context);
  ///
  ///   }
  /// }
  /// </code>
  /// </example>
  /// 
  /// <para>
  /// The beauty of <c>PlayerInputBase</c> is that you can subclass as many times you'd like for the actions 
  /// and phases you are interested. This way, you can have a <c>PlayerCombatInput</c> or <c>PlayerExplorationInput</c> acting as partial implementations of an action Map, tailored to the use case you're trying to solve.
  /// </para>
  /// 
  /// </summary>
  /// 
  [Obsolete("Check InputActionEventChannel instead")]
  public abstract class InputHandlerBase : MonoBehaviour {

    [Tooltip("The reference to the InputActionRegistry Scriptable Object")]
    public InputActionRegistry Registry;

    private IInputActionCollection2 _inputWrapper;

    protected virtual void OnEnable() {
      Registry.Initialize();
      _inputWrapper ??= CreateInputWrapper();
      EnableActionMap(_inputWrapper, Registry.ActionMapName);
    }

    protected virtual void OnDisable() => DisableActionMap(_inputWrapper, Registry.ActionMapName);

    protected virtual void Awake() => Initialize();

    public virtual void Initialize() {
      Registry.Initialize();
      RegisterInputHandlers();
    }

    /// <summary>
    /// Automatically registers methods decorated with <see cref="InputHandlerAttribute"/> to the input _registry.
    /// </summary>
    private void RegisterInputHandlers() {
      Debug.Log($"[InputHandlerBase] RegisterInputHandlers start. Registry: '{nameof(Registry)}' ActionMapName: '{Registry.ActionMapName}' ");
      var methods = GetType()
          .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
      foreach (var method in methods) {
        var attributes = method.GetCustomAttributes(typeof(InputHandlerAttribute), true);
        foreach (InputHandlerAttribute attr in attributes.Cast<InputHandlerAttribute>()) {
          var del = (Action<InputAction.CallbackContext>)Delegate.CreateDelegate(
              typeof(Action<InputAction.CallbackContext>), this, method, false);

          if (del != null) {
            try {
              Registry.Bind(attr.ActionName, phases: attr.Phases, handler: del);

              //string phasesStr = "";
              //if (attr.Phases != null) {
              //  foreach (var p in attr.Phases) phasesStr += p.ToString() + " ";
              //}
              //else {
              //  phasesStr += InputActionPhase.Performed.ToString();
              //}
              //Debug.Log($"  Registered: {attr.ActionName}. Phases: {phasesStr} ");
            } catch(Exception e) {
              Debug.LogError($"  Error: {attr.ActionName}. Error '{e.Message}'");
#if UNITY_EDITOR
              throw e;
#endif
            }
          }
        }
      }
      Debug.Log($"[InputHandlerBase] RegisterInputHandlers finish. Registry: '{nameof(Registry)}'");
#if UNITY_EDITOR
      Debug.Log(ToString());
#endif
    }

    private IInputActionCollection2 CreateInputWrapper() {
      // Find all types that implement IInputActionCollection2
      var wrapperType = AppDomain.CurrentDomain.GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .FirstOrDefault(t => typeof(IInputActionCollection2).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

      if (wrapperType == null) {
        Debug.LogError("No input wrapper class found.");
        return null;
      }

      return Activator.CreateInstance(wrapperType) as IInputActionCollection2;
    }

    /// <summary>
    /// This method configures the Action Map this handler is using to this intance of InputHandlerBase.<br/>
    /// 
    /// The equivalent code would be: 
    /// <code>
    ///    //assume PlayerControls is the name of the auto-generated wrapper
    ///    //assume Gameplay is the name of the action map
    ///    PlayerControls controls = new PlayerControls();
    ///    controls.Gameplay.SetCallbacks(this);
    ///    controls.Gameplay.Enable();
    /// </code>
    /// </summary>
    /// <param name="wrapper"></param>
    /// <param name="mapName"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private void EnableActionMap(IInputActionCollection2 wrapper, string mapName) {

      // Get the object representing the map defined in _registry
      var mapProperty = wrapper.GetType().GetProperty(mapName) ?? throw new InvalidOperationException(
            $"InputHandlerBase failed to find Action Map '{mapName}' in wrapper '{wrapper.GetType().Name}'. " +
            $"Ensure that the map name matches the property name in the generated input class."
        );
      var mapInstance = mapProperty.GetValue(wrapper);

      // 
      var setCallbacksMethod = (mapInstance?.GetType().GetMethod("SetCallbacks")) ?? throw new InvalidOperationException(
            $"Action Map '{mapName}' does not contain a SetCallbacks method. " +
            $"Ensure it was generated correctly and supports callback interfaces."
        );
      setCallbacksMethod.Invoke(mapInstance, new object[] { this });

      var enableMethod = mapInstance.GetType().GetMethod("Enable") ?? throw new InvalidOperationException(
            $"Action Map '{mapName}' does not contain an Enable method. " +
            $"This likely indicates a malformed input wrapper."
        );
      enableMethod.Invoke(mapInstance, null);
    }

    private void DisableActionMap(IInputActionCollection2 wrapper, string mapName) {
      // Get the object representing the map defined in _registry
      var mapProperty = wrapper.GetType().GetProperty(mapName) ?? throw new InvalidOperationException(
            $"InputHandlerBase failed to find Action Map '{mapName}' in wrapper '{wrapper.GetType().Name}'. " +
            $"Ensure that the map name matches the property name in the generated input class."
        );
      var mapInstance = mapProperty.GetValue(wrapper);

      var disableMethod = mapInstance.GetType().GetMethod("DisableActionMap") ?? throw new InvalidOperationException(
      $"Action Map '{mapName}' does not contain a DisableActionMap method. " +
      $"This likely indicates a malformed input wrapper."
        );
      disableMethod.Invoke(mapInstance, null);
    }

    public override string ToString() {
      StringBuilder sb = new();
      sb.AppendLine(Registry.ActionMapName);
      foreach (InputBinding binding in Registry.Bindings) {
        sb.AppendLine(binding.ToString());
      }
      return sb.ToString();
    }

  }

}