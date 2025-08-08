using UnityEngine;

namespace GMTK {
  public class InventoryManager : SnappableZoneManager {

    //public static event Action<GridSnappable> OnInventoryExit;

    //public virtual void OnEnable() {
    //  //InventoryInputController.OnElementRegistered += HandleRegisterRequest;
    //  InventoryInputController.OnInventoryExit += HandleRemoveRequest;

    //  //This is to handle the case where an element is unregistered from the grid and needs to be re-registered in the inventory
    //  PlayerInputController.OnElementSecondary += HandleRegisterRequest;
    //  //This is to handle the case where an element is registered in the grid and needs to be removed from the inventory
    //  PlayerInputController.OnElementDropped += HandleRemoveRequest;
    //}
    //public virtual void OnDisable() {
    //  //InventoryInputController.OnElementRegistered -= HandleRegisterRequest;
    //  InventoryInputController.OnInventoryExit -= HandleRemoveRequest;

    //  PlayerInputController.OnElementSecondary -= HandleRegisterRequest;
    //  PlayerInputController.OnElementDropped -= HandleRemoveRequest;
    //}

    
    //public override bool Register(GridSnappable element) {
    //  if (base.Register(element)) {
    //    //move element to inventory position 
    //    element.transform.position = transform.position; //move to inventory position
    //    Debug.Log($"[InventoryManager] Element '{element.name}' added to inventory at position {transform.position}");
    //    return true;
    //  }
    //  return false;
    //}

  }

}