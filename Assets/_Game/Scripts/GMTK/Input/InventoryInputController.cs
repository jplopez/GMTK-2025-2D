using GMTK;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryInputController : MonoBehaviour {

  public DrawerController Controller;
  public InventoryManager Manager;

  private PlayerControls _controls;

  //public static event Action<GridSnappable> OnElementHovered;
  //public static event Action OnElementUnhovered;
  //public static event Action<GridSnappable> OnElementRegistered;
  public static event Action<GridSnappable> OnInventoryExit;

  private void OnEnable() {
    _controls ??= new PlayerControls();
    _controls.Gameplay.Enable();
    _controls.Gameplay.Select.performed += OnClick;
  }

  private void OnDisable() {
    _controls.Gameplay.Select.performed -= OnClick;
    _controls.Gameplay.Disable();
  }

  private void OnClick(InputAction.CallbackContext context) {
    if (Controller == null || Controller.DrawerCollider == null) return;

    Vector2 screenPos = Mouse.current.position.ReadValue();
    Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
    if (Controller.DrawerCollider.OverlapPoint(worldPos)) {
      Debug.Log($"[InventoryInputController] Clicked at screen {screenPos}, world {worldPos}");

      if (Controller.IsOpen) {
        //check if we clicked on an element inside the drawer
        //if we did, and it's registered, we unregister it
        var hit = Physics2D.OverlapPoint(worldPos);
        if (hit != null) {
          var element = hit.GetComponentInParent<GridSnappable>();
          if (element != null && Manager != null && Manager.Contains(element)) {
            HandleUnregisterElement(element);
            return; //don't close the drawer if we just unregistered an element
          }
        }
      }
      // if not clicking on an element, toggle the drawer
      Controller.ToggleDrawer();
      Debug.Log($"Toggled drawer: {Controller.IsOpen}");
    }
  }

  private void HandleUnregisterElement(GridSnappable element) {
    if (Manager != null && element != null && Manager.Contains(element)) {
      Manager.Unregister(element);
      OnInventoryExit?.Invoke(element); // GridManager will list to this event and register the element in the grid
    }
    if (Controller != null && Controller.IsOpen) {
      Controller.ToggleDrawer();
    } 
  }


  //private void HandleRegisterElement(GridSnappable element) {
  //  if (Controller != null && !Controller.IsOpen) {
  //    Controller.ToggleDrawer();
  //  }
  //  if (Manager != null && element != null && !Manager.Contains(element)) {
  //    Manager.Register(element);
  //    OnElementRegistered?.Invoke(element);
  //  }
  //  //TODO play feedbacks 
  //}
}