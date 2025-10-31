using GMTK.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GMTK {

  public enum ControlsBindingMode { Tags, Manual }

  [AddComponentMenu("GMTK/Playable Element Components/Element Controls")]
  public class ElementControlsComponent : PlayableElementComponent {

    [Header("Controls Prefab")]
    [Space]
    public GameObject ControlsPrefab;
    [Tooltip("Offset from the element's position to instantiate the controls prefab")]
    public Vector2 PrefabOffset = new(0f, 0f);

    [Header("Controls Binding")]
    [Space]
    [Help("'BindingMode' tells how to setup the controls to listen player inputs. 'Tags' : the component looks for GameObjects with predefined tags (see Input_* tags). 'Manual': the component will not try to bind the controls, and will rely on external setup")]
    [Space]
    public ControlsBindingMode BindingMode = ControlsBindingMode.Tags;

    protected GameObject _controls;
    protected bool _inUse = false;

    protected override void Initialize() {
      if (ControlsPrefab == null) {
        this.LogError("ControlsPrefab is not assigned.");
        return;
      }

      if (BindingMode == ControlsBindingMode.Tags) {
        InitializeControlsUsingTags();
      }
    }

    protected virtual void InitializeControlsUsingTags() {
      if (_controls == null) {
        _controls = Instantiate(ControlsPrefab, _playableElement.transform);
      }
      //find children components by tag and bind them to the playable element
      var foundInputs = _controls.transform.FindChildrenWithAnyTag(new string[] {
        "Input_RotateCW",
        "Input_RotateCCW",
        "Input_FlipX",
        "Input_FlipY"
      });
      foreach (var inputTransform in foundInputs) {
        var onClickAction = (UnityAction)null;
        switch (inputTransform.tag) {
          case "Input_RotateCW":
            onClickAction = RotateCW;
            break;
          case "Input_RotateCCW":
            onClickAction = RotateCCW;
            break;
          case "Input_FlipX":
            onClickAction = FlipX;
            break;
          case "Input_FlipY":
            onClickAction = FlipY;
            break;
          default:
            this.LogWarning("Input tag not recognized: " + inputTransform.tag);
            return;
        }
        this.LogDebug($"{_playableElement.name} Binding input '{inputTransform.name}' with tag '{inputTransform.tag}' to '{onClickAction.Method}'");
        SetupInputAction(inputTransform, onClickAction);
      }
    }

    private void SetupInputAction(Transform inputTransform, UnityAction onClickAction) {
      if (inputTransform.TryGetComponent<Button>(out var button)) {
        button.onClick.AddListener(onClickAction);
      } else {
        this.LogWarning($"Input '{inputTransform.name}' does not have a Button component.");
      }
    }

    protected override bool Validate() => ControlsPrefab != null;

    public override void OnSelected(PlayableElementEventArgs evt) {
      if (evt.Element == _playableElement && ControlsPrefab != null) {
        CreateOrUpdateControlsPrefab(setActive:true);
        _inUse = true;
      }
    }

    private void CreateOrUpdateControlsPrefab(bool setActive = true, bool updatePosition = true) {
      if (_controls == null) {
        _controls = Instantiate(ControlsPrefab, _playableElement.transform);
      }
      if (updatePosition) {
        _controls.transform.position = (Vector2)_playableElement.transform.position + PrefabOffset;
      }
      _controls.SetActive(setActive);
    }

    public override void OnDeselected(PlayableElementEventArgs evt) {
      if (evt.Element == _playableElement && _controls != null) {
        CreateOrUpdateControlsPrefab(setActive: false, updatePosition:false);
        _inUse = false;
      }
    }

    protected override void OnUpdate() {
      base.OnUpdate();
      if(_inUse && _controls != null && _controls.activeSelf) {
        CreateOrUpdateControlsPrefab(setActive: true, updatePosition: true);
      }
    }

    public void RotateCW() => _playableElement.RotateClockwise();
    public void RotateCCW() => _playableElement.RotateCounterClockwise();
    public void FlipX() => _playableElement.FlipX();
    public void FlipY() => _playableElement.FlipY();

  }
}