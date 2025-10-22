
using MoreMountains.Tools;
using UnityEngine;

namespace GMTK {

  public class ControlsElementComponent : PlayableElementComponent {

    [Header("Controls Prefab")]
    public GameObject ControlsPrefab;
    [Tooltip("Offset from the element's position to instantiate the controls prefab")]
    public Vector2 PrefabOffset = new(0f, -2f);

    [Header("Selection Trigger")]
    [Tooltip("If true, the selection trigger will be the same as the element's selection trigger. If false, you can override it below.")]
    public bool SameAsElement = true;
    [Tooltip("If 'Same As Element' is false, use this to override the selection trigger.")]
    [MMCondition("SameAsElement", false)]
    public SelectionTrigger OverrideSelectionTriggers = SelectionTrigger.None;

    protected GameObject _controls;
    protected bool _inUse = false;

    protected override void Initialize() {
      if (ControlsPrefab == null) {
        Debug.LogError("ControlsPrefab is not assigned.");
      }
    }

    protected override bool Validate() => ControlsPrefab != null;

    public override void OnSelected(PlayableElementEventArgs evt) {
      if (evt.Element == _playableElement && ControlsPrefab != null) {
        if (_controls == null) {
          _controls = Instantiate(ControlsPrefab, _playableElement.transform);
        }
        _controls.transform.position = (Vector2)_playableElement.transform.position + PrefabOffset;
        _controls.SetActive(true);
        _inUse = true;
      }
    }

    public override void OnDeselected(PlayableElementEventArgs evt) {
      if (evt.Element == _playableElement && _controls != null) {
        _controls.SetActive(false);
        _inUse = false;
      }
    }

    protected override void OnUpdate() {
      base.OnUpdate();
      if(_inUse && _controls != null && _controls.activeSelf) {
        _controls.transform.position = (Vector2)_playableElement.transform.position + PrefabOffset;
      }
    }

  }
}