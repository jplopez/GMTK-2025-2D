using UnityEngine;

namespace Ameba {

  public interface ISelector<T> where T : ISelectable {
    bool CanSelect { get; set; }
    bool IsSelecting { get; }
    T SelectedElement { get; }
    bool TrySelect(T element);
    bool TryDeselect();
  }
}