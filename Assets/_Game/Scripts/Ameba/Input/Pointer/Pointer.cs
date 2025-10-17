
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ameba {

  /// <summary>
  /// A pointer is an object that can select, drag or hover over Pointable objects (mouse, touchscreen, etc).
  /// In most cases, Pointables are GameObjects or MonoBehaviours that represent elements of the game world.
  /// 
  /// TODO: implement common behaviours to simplify extensions. For example: GameObjectPointer, UIElementPointer, etc.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public abstract class Pointer<T> : IDragger<Pointable>, IHover<Pointable>, ISelector<Pointable> where T : Pointable {
    
    public enum UpdateMethod { Update, FixedUpdate, LateUpdate }
    public enum UpdateMode { Discrete, Continuous }
    [Flags] public enum SelectionTrigger { OnPrimaryAction, OnSecondaryAction, OnMiddleAction, OnHover, }
    public enum EventSystem { UnityEvents, CSharpEvents, EventChannel }

    [Header("Pointer Visuals")]
    [Tooltip("Whether to show a visual representation of the pointer in the game world")]
    public bool ShowPointer = true;
    [Tooltip("The sprite renderer used to visualize the pointer in the game world. If null, no visualization will be shown")]
    public SpriteRenderer PointerSpriteRenderer;
    [Tooltip("The offset applied to the pointer sprite relative to the pointer's world position")]
    public Vector3 PointerSpriteOffset = Vector3.zero;
    [Tooltip("(optional) The sprite renderer used to visualize the primary action state of the pointer (e.g., left click or tap)")]
    public SpriteRenderer PrimaryActionPointerRenderer;
    [Tooltip("(optional) The sprite renderer used to visualize the secondary action state of the pointer (e.g., right click or two-finger tap)")]
    public SpriteRenderer SecondaryActionPointerRenderer;
    [Tooltip("(optional) The sprite renderer used to visualize the middle action state of the pointer (e.g., middle click or three-finger tap)")]
    public SpriteRenderer MiddleActionPointerRenderer;

    [Header("Input actions")]
    [Tooltip("The input action that provides the pointer position in screen space.")]
    public InputAction PointerPosition;
    [Tooltip("The input action that provides the primary action (usually left click or tap)")]
    public InputAction PrimaryAction;
    [Tooltip("The input action that provides the secondary action (usually right click or two-finger tap)")]
    public InputAction SecondaryAction;
    [Tooltip("The input action that provides the middle action (usually middle click or three-finger tap)")]
    public InputAction MiddleAction;

    [Header("Dragging settings")]
    [Tooltip("The minimum distance the pointer must move to start a drag operation")]
    [Min(0f)]
    public float DragThreshold = 0.1f;
    [Tooltip("The update mode for drag operations")]
    public UpdateMethod DragUpdateMethod = UpdateMethod.Update;
    [Tooltip("The update mode for drag operations")]
    public UpdateMode DragUpdateMode = UpdateMode.Continuous;
    [Tooltip("The interval in seconds between drag updates when using Discrete update mode")]
    public float DragUpdateInterval = 0.1f;

    [Header("Hovering settings")]
    [Tooltip("The time in seconds the pointer must hover an object it triggers the hover behavior")]
    public float HoverDelay = 0.5f;
    [Tooltip("The update mode for hover operations.")]
    public UpdateMethod HoverUpdateMethod = UpdateMethod.Update;
    [Tooltip("The update mode for hover operations.")]
    public UpdateMode HoverUpdateMode = UpdateMode.Continuous;
    [Tooltip("The interval in seconds between hover updates when using Discrete update mode")]
    public float HoverUpdateInterval = 0.1f;

    [Header("Selection settings")]
    [Help("Regardless of the selection trigger, the pointer will still notify all events (enter, exit, select, deselect), unless events are disabled")]
    [Tooltip("The triggers that cause selection operations")]
    public SelectionTrigger SelectionTriggers = SelectionTrigger.OnPrimaryAction;
    [Tooltip("The triggers that cause unselection operations")]
    public SelectionTrigger UnselectionTriggers = SelectionTrigger.OnPrimaryAction | SelectionTrigger.OnSecondaryAction | SelectionTrigger.OnMiddleAction;
    [Tooltip("The update mode for selection operations")]
    public UpdateMethod SelectionUpdateMethod = UpdateMethod.Update;
    [Tooltip("The update mode for selection operations")]
    public UpdateMode SelectionUpdateMode = UpdateMode.Discrete;
    [Tooltip("The interval in seconds between selection updates when using Discrete update mode")]
    public float SelectionUpdateInterval = 0.1f;

    [Header("Camera and Raycast settings")]
    [Tooltip("The camera used to convert screen positions to world positions. If null, Camera.main will be used.")]
    public Camera Camera;
    [Tooltip("The layer mask used to filter raycast hits. Only objects on these layers will be considered for pointer interactions.")]
    public LayerMask InteractionLayerMask = ~0;
    [Tooltip("The maximum distance for raycast hits. Objects beyond this distance will be ignored.")]
    public float MaxRaycastDistance = 100f;
    [Tooltip("The z position in world space used when converting screen positions to world positions for 2D interactions.")]
    public float WorldZPosition = 0f;
    [Tooltip("The number of results to return when performing raycasts. Higher values may impact performance.")]
    [Min(1)]
    public int MaxRaycastResults = 10;

    [Header("Events")]
    [Tooltip("Whether to disable all pointer events.")]
    public bool DisableAllEvents = false;
    [Tooltip("Whether to disable drag events.")]
    public bool DisableDragEvents = false;
    [Tooltip("Whether to disable hover events.")]
    public bool DisableHoverEvents = false;
    [Tooltip("Whether to disable selection events.")]
    public bool DisableSelectionEvents = false;
    [Tooltip("The event systems to use for pointer events")]
    public EventSystem TriggerEventsVia = EventSystem.UnityEvents;

    [Header("Event Handlers")]
    [Tooltip("UnityEvents handler for pointer events")]
    public UnityEvent<PointerEvents> PointerEvents;
    [Tooltip("EventChannel handler for pointer events")]
    public EventChannel<PointerEvents> PointerChannel;

    // C# events handler for pointer events
    public delegate void PointerEventHandler(PointerEventArgs eventArgs);
    public event PointerEventHandler OnPointerEnter;
    public event PointerEventHandler OnPointerExit;
    public event PointerEventHandler OnPointerSelect;
    public event PointerEventHandler OnPointerDeselect;
    public event PointerEventHandler OnPointerDragStart;
    public event PointerEventHandler OnPointerDragging;
    public event PointerEventHandler OnPointerDragEnd;


    public abstract bool CanShowPointer { get; set; }
    public abstract bool IsPointerVisible { get; }
    public abstract SpriteRenderer PointerSprite { get; }


    public abstract bool CanDrag { get; set; }
    public abstract bool IsDragging { get; }
    public abstract Pointable DraggedElement { get; }
    public abstract bool CanHover { get; set; }
    public abstract bool IsHovering { get; }
    public abstract Pointable HoveredElement { get; }
    public abstract bool CanSelect { get; set; }
    public abstract bool IsSelecting { get; }
    public abstract Pointable SelectedElement { get; }

    public abstract void StartHover(Pointable element);
    public abstract void StopHover();
    public abstract bool TryDeselect();
    public abstract bool TryGetHoverableAt(UnityEngine.Vector3 worldPosition, out Pointable element);
    public abstract bool TryGetHoverableAt(UnityEngine.Vector2 screenPosition, out Pointable element);
    public abstract bool TrySelect(UnityEngine.Vector3 worldPosition, out Pointable element);
    public abstract bool TrySelect(UnityEngine.Vector2 screenPosition, out Pointable element);
    public abstract bool TrySelect(Pointable element);
    public abstract bool TryStartDrag(Pointable element);
    public abstract bool TryStartDrag(Vector3 worldPosition, out Pointable element);
    public abstract bool TryStartDrag(Vector2 screenPosition, out Pointable element);
    public abstract bool TryStopDrag();
    public abstract void UpdateDrag(Vector3 worldPosition);
    public abstract void UpdateHover();
  }

  public enum PointerEvents {
    PointerEnter,
    PointerExit,
    PointerSelect,
    PointerDeselect,
    PointerDragStart,
    PointerDragging,
    PointerDragEnd,
  }

  public class PointerEventArgs : EventArgs {

    public PointerEvents EventType;
    public Pointer<Pointable> Pointer;
    public Pointable Pointable;

    public Vector2 screenPosition;
    public Vector3 worldPosition;
    public bool isPrimaryAction;
    public bool isSecondaryAction;
    public bool isMiddleAction;

    public PointerEventArgs(PointerEvents eventType, Pointer<Pointable> pointer, Pointable pointable) {
      Pointer = pointer;
      Pointable = pointable;
      EventType = eventType;
    }
  }
}