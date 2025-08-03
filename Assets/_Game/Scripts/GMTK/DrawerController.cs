using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GMTK {

  public class DrawerController : MonoBehaviour, IPointerClickHandler {

    [Header("Drawer Model")]
    public Transform DrawerTransform;
    public Collider2D DrawerCollider;

    [Header("Drawer Animation")]
    public Vector3 ClosedPosition;
    public Vector3 OpenedPosition;
    public float SlideDuration = 0.5f;

    public bool IsOpen => isOpen;

    private bool isOpen = false;
    private Coroutine animationRoutine;

    public void Awake() { 
      if (DrawerTransform == null) {
        DrawerTransform = transform;
      }
      DrawerTransform.position = ClosedPosition;

      if (DrawerCollider == null) {
        DrawerCollider = GetComponent<Collider2D>();
      }
    }

    public void ToggleDrawer() {
      if (animationRoutine != null) StopCoroutine(animationRoutine);
      animationRoutine = StartCoroutine(SlideDrawer(isOpen ? ClosedPosition : OpenedPosition));
      isOpen = !isOpen;
    }

    private IEnumerator SlideDrawer(Vector3 targetPos) {
      Vector3 startPos = DrawerTransform.position;
      float elapsed = 0f;

      while (elapsed < SlideDuration) {
        DrawerTransform.position = Vector3.Lerp(startPos, targetPos, elapsed / SlideDuration);
        elapsed += Time.deltaTime;
        yield return null;
      }

      DrawerTransform.position = targetPos;
    }

    //private void OnEnable() {
    //  InventoryManager.OnInventoryExit += HandleInventoryExit;
    //}

    //private void OnDisable() {
    //  InventoryManager.OnInventoryExit -= HandleInventoryExit;
    //}

    //private void HandleInventoryExit(GridSnappable element) {
    //  if (isOpen) ToggleDrawer();
    //}

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
      var clickPos = Camera.main.ScreenToWorldPoint(eventData.pressPosition);
      if(DrawerCollider != null 
        && DrawerCollider.OverlapPoint(clickPos)
        && eventData.button.Equals(PointerEventData.InputButton.Left)) {
        ToggleDrawer();
      }
    }
  }
}