using UnityEngine;

namespace Ameba {

  /// <summary>
  /// Abstract MonoBehaviour class to serve as a base for implementing draggable capabilities 
  /// into objects implementing the IDraggable interface.
  /// </summary>
  public abstract class DraggingController : MonoBehaviour {

    [Header("Dragging Controller")]
    [SerializeField] protected bool _enableDragging = true;
    [SerializeField] protected bool _debugLogging = false;

    /// <summary>
    /// Current object being dragged
    /// </summary>
    public IDraggable CurrentDragged { get; protected set; }

    /// <summary>
    /// Current object being hovered over
    /// </summary>
    public IDraggable CurrentHovered { get; protected set; }

    /// <summary>
    /// Current active/selected object
    /// </summary>
    public IDraggable CurrentActive { get; protected set; }

    /// <summary>
    /// Whether dragging is currently enabled
    /// </summary>
    public bool DraggingEnabled {
      get => _enableDragging;
      set => _enableDragging = value;
    }

    /// <summary>
    /// Whether any object is currently being dragged
    /// </summary>
    public bool IsDragging => CurrentDragged != null;

    /// <summary>
    /// Whether any object is currently hovered
    /// </summary>
    public bool IsHovering => CurrentHovered != null;

    /// <summary>
    /// Whether any object is currently active
    /// </summary>
    public bool HasActiveElement => CurrentActive != null;

    #region Abstract Methods - Input Implementation

    /// <summary>
    /// Get the current pointer/cursor position in screen coordinates
    /// </summary>
    protected abstract Vector2 GetPointerScreenPosition();

    /// <summary>
    /// Get the current pointer/cursor position in world coordinates
    /// </summary>
    protected abstract Vector3 GetPointerWorldPosition();

    /// <summary>
    /// Check if primary button was pressed this frame
    /// </summary>
    protected abstract bool GetPrimaryButtonDown();

    /// <summary>
    /// Check if primary button was released this frame
    /// </summary>
    protected abstract bool GetPrimaryButtonUp();

    /// <summary>
    /// Check if primary button is currently held down
    /// </summary>
    protected abstract bool GetPrimaryButtonHeld();

    /// <summary>
    /// Check if secondary button was pressed this frame
    /// </summary>
    protected abstract bool GetSecondaryButtonDown();

    /// <summary>
    /// Check if secondary button was released this frame
    /// </summary>
    protected abstract bool GetSecondaryButtonUp();

    /// <summary>
    /// Check if there was a double-click on primary button this frame
    /// </summary>
    protected abstract bool GetPrimaryDoubleClick();

    /// <summary>
    /// Check if there was a double-click on secondary button this frame
    /// </summary>
    protected abstract bool GetSecondaryDoubleClick();

    #endregion

    #region Unity Lifecycle

    protected virtual void Update() {
      if (!_enableDragging) return;

      UpdateHovering();
      UpdateDragging();
      UpdateInput();
    }

    #endregion

    #region Core Dragging Logic

    protected virtual void UpdateHovering() {
      var hoveredObject = GetObjectAtPointer();

      if (hoveredObject != CurrentHovered) {
        // Exit previous hover
        if (CurrentHovered != null) {
          CurrentHovered.OnPointerExit();
          OnObjectHoverEnd(CurrentHovered);
        }

        // Enter new hover
        CurrentHovered = hoveredObject;
        if (CurrentHovered != null) {
          CurrentHovered.OnPointerEnter();
          OnObjectHoverStart(CurrentHovered);
        }
      }
    }

    protected virtual void UpdateDragging() {
      if (CurrentDragged != null) {
        Vector3 worldPos = GetPointerWorldPosition();
        CurrentDragged.OnDragUpdate(worldPos);
        OnObjectDragUpdate(CurrentDragged, worldPos);
      }
    }

    protected virtual void UpdateInput() {
      // Handle primary button press
      if (GetPrimaryButtonDown()) {
        HandlePrimaryPress();
      }

      // Handle primary button release
      if (GetPrimaryButtonUp()) {
        HandlePrimaryRelease();
      }

      // Handle secondary button press
      if (GetSecondaryButtonDown()) {
        HandleSecondaryPress();
      }

      // Handle secondary button release
      if (GetSecondaryButtonUp()) {
        HandleSecondaryRelease();
      }

      // Handle double clicks
      if (GetPrimaryDoubleClick()) {
        HandlePrimaryDoubleClick();
      }

      if (GetSecondaryDoubleClick()) {
        HandleSecondaryDoubleClick();
      }
    }

    #endregion

    #region Input Handlers

    protected virtual void HandlePrimaryPress() {
      var targetObject = GetObjectAtPointer();

      if (targetObject != null && targetObject.IsDraggable) {
        // Start dragging
        StartDragging(targetObject);
        // Set as active
        SetActiveObject(targetObject);
      }
      else {
        // Clicked on empty space - clear active object
        SetActiveObject(null);
      }

      OnPrimaryPress(GetPointerWorldPosition(), targetObject);
    }

    protected virtual void HandlePrimaryRelease() {
      if (CurrentDragged != null) {
        StopDragging(CurrentDragged);
      }

      OnPrimaryRelease(GetPointerWorldPosition(), CurrentDragged);
    }

    protected virtual void HandleSecondaryPress() {
      var targetObject = GetObjectAtPointer();
      OnSecondaryPress(GetPointerWorldPosition(), targetObject);
    }

    protected virtual void HandleSecondaryRelease() {
      var targetObject = GetObjectAtPointer();
      OnSecondaryRelease(GetPointerWorldPosition(), targetObject);
    }

    protected virtual void HandlePrimaryDoubleClick() {
      var targetObject = GetObjectAtPointer();
      OnPrimaryDoubleClick(GetPointerWorldPosition(), targetObject);
    }

    protected virtual void HandleSecondaryDoubleClick() {
      var targetObject = GetObjectAtPointer();
      OnSecondaryDoubleClick(GetPointerWorldPosition(), targetObject);
    }

    #endregion

    #region Drag Management

    protected virtual void StartDragging(IDraggable obj) {
      if (obj == null || !obj.IsDraggable) return;

      CurrentDragged = obj;
      obj.OnDragStart();
      OnObjectDragStart(obj);

      DebugLog($"Started dragging {obj}");
    }

    protected virtual void StopDragging(IDraggable obj) {
      if (obj == null) return;

      obj.OnDragEnd();
      OnObjectDragEnd(obj);
      CurrentDragged = null;

      DebugLog($"Stopped dragging {obj}");
    }

    protected virtual void SetActiveObject(IDraggable obj) {
      if (obj == CurrentActive) return;

      // Deactivate previous
      if (CurrentActive != null) {
        CurrentActive.IsActive = false;
        CurrentActive.OnBecomeInactive();
        OnObjectBecomeInactive(CurrentActive);
      }

      // Activate new
      CurrentActive = obj;
      if (CurrentActive != null) {
        CurrentActive.IsActive = true;
        CurrentActive.OnBecomeActive();
        OnObjectBecomeActive(CurrentActive);
      }

      DebugLog($"Active object changed to {obj}");
    }

    #endregion

    #region Object Detection

    /// <summary>
    /// Get the IDraggable object at the current pointer position
    /// </summary>
    protected virtual IDraggable GetObjectAtPointer() {
      Vector2 worldPos = GetPointerWorldPosition();
      RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

      if (hit.collider != null) {
        var draggable = hit.collider.GetComponent<IDraggable>();
        if (draggable != null) {
          return draggable;
        }

        // Also try getting from parent
        draggable = hit.collider.GetComponentInParent<IDraggable>();
        return draggable;
      }

      return null;
    }

    #endregion

    #region Virtual Event Methods - Override for custom behavior

    protected virtual void OnObjectHoverStart(IDraggable obj) { }
    protected virtual void OnObjectHoverEnd(IDraggable obj) { }
    protected virtual void OnObjectDragStart(IDraggable obj) { }
    protected virtual void OnObjectDragUpdate(IDraggable obj, Vector3 worldPosition) { }
    protected virtual void OnObjectDragEnd(IDraggable obj) { }
    protected virtual void OnObjectBecomeActive(IDraggable obj) { }
    protected virtual void OnObjectBecomeInactive(IDraggable obj) { }

    protected virtual void OnPrimaryPress(Vector3 worldPosition, IDraggable targetObject) { }
    protected virtual void OnPrimaryRelease(Vector3 worldPosition, IDraggable targetObject) { }
    protected virtual void OnSecondaryPress(Vector3 worldPosition, IDraggable targetObject) { }
    protected virtual void OnSecondaryRelease(Vector3 worldPosition, IDraggable targetObject) { }
    protected virtual void OnPrimaryDoubleClick(Vector3 worldPosition, IDraggable targetObject) { }
    protected virtual void OnSecondaryDoubleClick(Vector3 worldPosition, IDraggable targetObject) { }

    #endregion

    #region Utility

    protected void DebugLog(string message) {
      if (_debugLogging) {
        Debug.Log($"[{GetType().Name}] {message}");
      }
    }

    #endregion
  }
}
