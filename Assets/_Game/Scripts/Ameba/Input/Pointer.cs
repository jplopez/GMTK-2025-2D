
using UnityEngine;

namespace Ameba {

  /// <summary>
  /// Represents an object that can interact with a Pointer (mouse, touchscreen, etc).
  /// Pointable objects can be selected, dragged, and hovered over.
  /// 
  /// TODO: implement common behaviour to simplify extensions. For example GameObjectPointable, UIElementPointable, etc.
  /// </summary>
  public abstract class Pointable : ISelectable, IDraggable, IHoverable {
    public abstract bool CanSelect { get; }
    public abstract bool IsSelected { get; }
    public abstract bool IsActive { get; set; }
    public abstract Transform SelectTransform { get; }
    public abstract Collider2D InteractionCollider { get; }
    public abstract bool IsDraggable { get; }
    public abstract bool IsBeingDragged { get; }
    public abstract Transform DragTransform { get; }
    public abstract bool CanHover { get; }
    public abstract bool IsHovered { get; }
    public abstract Transform HoverTransform { get; }

    public abstract void EnableSelectable(bool selectable = true);
    public abstract void MarkHovered(bool hovered = true);
    public abstract void MarkSelected(bool selected = true);
    public abstract void OnBecomeActive();
    public abstract void OnBecomeInactive();
    public abstract void OnDeselect();
    public abstract void OnDragEnd();
    public abstract void OnDragStart();
    public abstract void OnDragUpdate(Vector3 worldPosition);
    public abstract void OnHover();
    public abstract void OnHoverDisabled();
    public abstract void OnHoverEnabled();
    public abstract void OnHoverUpdate();
    public abstract void OnPointerEnter();
    public abstract void OnPointerExit();
    public abstract void OnSelect();
    public abstract void OnSelectDisabled();
    public abstract void OnSelectEnabled();
    public abstract void OnSelectUpdate();
    public abstract void OnUnhover();
  }

  /// <summary>
  /// A pointer is an object that can select, drag or hover over Pointable objects (mouse, touchscreen, etc).
  /// In most cases, Pointables are GameObjects or MonoBehaviours that represent elements of the game world.
  /// 
  /// TODO: implement common behaviours to simplify extensions. For example: GameObjectPointer, UIElementPointer, etc.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public abstract class Pointer<T> : IDragger<Pointable>, IHover<Pointable>, ISelector<Pointable> where T : Pointable {
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
}