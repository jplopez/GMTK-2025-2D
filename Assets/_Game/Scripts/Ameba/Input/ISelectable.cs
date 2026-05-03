using UnityEngine;

namespace Ameba {

  public interface ISelectable {

    public bool CanSelect { get; }

    public bool IsSelected { get; }

    Transform SelectTransform { get; }

    Collider2D InteractionCollider { get; }

    public void MarkSelected(bool selected = true);

    public void OnSelect();
    
    public void OnDeselect();

  }
}