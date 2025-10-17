using UnityEngine;

namespace Ameba {

  public interface IHoverable {

    public bool CanHover { get; }

    public bool IsHovered { get; }

    public bool IsActive { get; set; }

    Transform HoverTransform { get; }

    Collider2D InteractionCollider { get; }

    public void MarkHovered(bool hovered = true);

    public void OnPointerEnter();

    public void OnPointerExit();

    public void OnHover();
    public void OnHoverUpdate();
    public void OnUnhover();
    public void OnHoverEnabled();
    public void OnHoverDisabled();

  }
}