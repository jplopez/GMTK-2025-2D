
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
    public abstract void OnPointerOver();
    public abstract void OnPointerOut();
    public abstract void OnSelect();
    public abstract void OnSelectDisabled();
    public abstract void OnSelectEnabled();
    public abstract void OnSelectUpdate();
    public abstract void OnUnhover();
  }
}