using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ameba {

  public interface ISelectable {

    public bool CanSelect { get; }

    public bool IsSelected { get; }

    public bool IsActive { get; set; }

    Transform SelectTransform { get; }

    Collider2D InteractionCollider { get; }

    public void MarkSelected(bool selected = true);

    public void EnableSelectable(bool selectable = true);


    public void OnSelect();
    public void OnSelectUpdate();
    public void OnDeselect();
    public void OnSelectDisabled();
    public void OnSelectEnabled();
  }
}