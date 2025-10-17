using UnityEngine;

namespace Ameba {

  public interface ISelector<T> where T : ISelectable {
    bool CanSelect { get; set; }
    bool IsSelecting { get; }
    T SelectedElement { get; }
    bool TrySelect(Vector3 worldPosition, out T element);
    bool TrySelect(Vector2 screenPosition, out T element);
    bool TrySelect(T element);
    bool TryDeselect();
  }
}