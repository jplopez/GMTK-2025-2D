
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.InputSystem;

namespace Ameba.Input {


  public class InputActionCollectionHelper {

    private IInputActionCollection2 _inputWrapper;

    private Dictionary<string, object> _propertyInstances = new();

    public InputActionCollectionHelper(IInputActionCollection2 inputWrapper) {
      _inputWrapper = inputWrapper;
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
    public void EnableActionMap(string mapName) {
      var mapInstance = GetOrCreateMapInstance(mapName);
      InvokeMapMethod(mapInstance, "Enable");
    }

    /// <summary>
    /// This method configures the Action Map this handler is using to this intance of InputHandlerBase.<br/>
    /// 
    /// The equivalent code would be: 
    /// <code>
    ///
    ///    //assume 'controls' is the name of the auto-generated wrapper
    ///    //assume 'Gameplay' is the name of the action map and is enabled
    ///    controls.Gameplay.DisableActionMap()
    ///
    /// </code>
    /// </summary>
    /// <param name="wrapper"></param>
    /// <param name="mapName"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void DisableActionMap(string mapName) {
      var mapInstance = GetOrCreateMapInstance(mapName); 
      InvokeMapMethod(mapInstance, "DisableActionMap");
    }

    private PropertyInfo GetMapProp(string mapName) {
      return _inputWrapper.GetType().GetProperty(mapName) ?? throw new InvalidOperationException(
            $"InputHandlerBase failed to find Action Map '{mapName}' in wrapper '{_inputWrapper.GetType().Name}'. " +
            $"Ensure that the map name matches the property name in the generated input class."
        );
    }

    private object GetOrCreateMapInstance(string mapName) {
      var mapProp = GetMapProp(mapName);
      if(!_propertyInstances.TryGetValue(mapName, out var mapInstance)) {
        mapInstance = mapProp.GetValue(_inputWrapper);
        _propertyInstances.Add(mapName, mapInstance);
      }
      return mapInstance;
    }

    private MethodInfo GetMapMethod(object mapInstance, string methodName) {
      return mapInstance.GetType().GetMethod(methodName) ?? throw new InvalidOperationException(
           $"Action Map '{mapInstance.GetType().Name}' does not contain '{methodName}' method. " +
           $"This likely indicates a malformed input wrapper."
       );
    }

    private object InvokeMapMethod(object mapInstance, string methodName, object[] parameters=null) {
      var method = GetMapMethod(mapInstance, methodName);
      if (method != null) {
        return method.Invoke(mapInstance, parameters);
      }
      return null;
    }

  }


}