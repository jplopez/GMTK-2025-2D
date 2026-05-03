using GMTK.Extensions;
using System;
using Ameba;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GMTK {

  public enum ControlsBindingMode { Tags, Manual }
  
  public enum ControlsBindingTags { InputRotateCw, InputRotateCcw, InputFlipX, InputFlipY }

  /// <summary>
  /// Component responsible for show/hide the UI controls for the PlayableElement, and binding them to the element's actions.<br/>
  /// This component uses tags on the children of the ControlsPrefab to identify which input should be bound to which action of the PlayableElement.
  /// The expected tags are defined in the 'ControlsBindingTags' enum.<br/>
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Element Controls")]
  public class ElementUIControlsComponent : PlayableElementComponent , ISelectable {

    // Tags assigned to GameObjects in the ControlsPrefab to identify them as inputs to bind to the PlayableElement.
  
    [Header("Controls Prefab"), Space]
    public GameObject ControlsPrefab;
    
    [Tooltip("Offset from the element's position to instantiate the controls prefab")]
    public Vector2 PrefabOffset = new(0f, 0f);

    public bool StartVisible = false;

    [Header("Controls Binding"), Space]
    [Help("'BindingMode' tells how to setup the controls to listen player inputs. 'Tags' : the component looks for GameObjects with predefined tags (see Input_* tags). 'Manual': the component will not try to bind the controls, and will rely on external setup"), Space]
    public ControlsBindingMode BindingMode = ControlsBindingMode.Tags;

    // instance of ControlsPrefab used by this component
    private GameObject _controlsInstance;

    private static string[] InputTagsNames => Enum.GetNames(typeof(ControlsBindingTags));
    
    
    #region Initialization
    
    protected override void Initialize() {
      if (ControlsPrefab == null) {
        this.LogError("ControlsPrefab is not assigned.");
        return;
      }

      if (BindingMode == ControlsBindingMode.Tags) {
        InitializeControlsUsingTags();
      }
      
      if(StartVisible) CreateOrUpdateControlsPrefab(true);
    }

    private void InitializeControlsUsingTags() {
      if (_controlsInstance == null) {
        _controlsInstance = Instantiate(ControlsPrefab, _playableElement.transform);
      }
      //find children components by tag and bind them to the playable element
      var foundInputs = _controlsInstance.transform.FindChildrenWithAnyTag(InputTagsNames);
      foreach (var inputTransform in foundInputs) {
        SetupInputAction(inputTransform, ResolveMethodFromTag(inputTransform.tag));
      }
    }

    private UnityAction ResolveMethodFromTag(string controlBindingTag)
    {
      if (string.IsNullOrEmpty(controlBindingTag)) return null;
      UnityAction onClickAction = null;
      switch (controlBindingTag) {
        case "Input_RotateCW":
          onClickAction = RotateCw;
          break;
        case "Input_RotateCCW":
          onClickAction = RotateCcw;
          break;
        case "Input_FlipX":
          onClickAction = FlipX;
          break;
        case "Input_FlipY":
          onClickAction = FlipY;
          break;
        default:
          this.LogWarning($"Input tag not recognized: {controlBindingTag}. No action will be bound to this input.");
          break;
      }
      return onClickAction;
    }

    private void SetupInputAction(Transform inputTransform, UnityAction onClickAction) {
      if(!inputTransform || onClickAction == null) return;
      
      if (inputTransform.TryGetComponent<Button>(out var button)) {
        button.onClick.AddListener(onClickAction);
      } else {
        this.LogWarning($"Input '{inputTransform.name}' does not have a Button component.");
      }
    }

    #endregion
    
    
    #region PlayableElementComponent overrides

    private bool IsEventValid(PlayableElementEventArgs evt) => evt.Element == _playableElement;
    
    protected override bool Validate() => ControlsPrefab;
    
    public override void OnSelected(PlayableElementEventArgs evt) => HandleEvent(evt, true);

    public override void OnDeselected(PlayableElementEventArgs evt) => HandleEvent(evt, false);

    private void HandleEvent(PlayableElementEventArgs evt, bool setActive) {
      if (!IsEventValid(evt)) return;
      CreateOrUpdateControlsPrefab(setActive);
    }

    protected override void OnUpdate() {
      base.OnUpdate();
      UpdateControlsPosition();
    }

    #endregion
    
    
    #region UI Controls Management
    
    private void CreateOrUpdateControlsPrefab(bool setActive) {
      if (!_controlsInstance) {
        _controlsInstance = Instantiate(ControlsPrefab, _playableElement.transform);
      }
      _controlsInstance.SetActive(setActive);
      if(_controlsInstance.activeSelf) UpdateControlsPosition();
    }

    private void UpdateControlsPosition() {
      if (!_controlsInstance || !_controlsInstance.activeSelf) return; 
      _controlsInstance.transform.position = (Vector2)_playableElement.transform.position + PrefabOffset;
    }

    #endregion

    
    #region Binding Methods
    
    private void RotateCw() => _playableElement.RotateClockwise();
    private void RotateCcw() => _playableElement.RotateCounterClockwise();
    private void FlipX() => _playableElement.FlipX();
    private void FlipY() => _playableElement.FlipY();
    
    #endregion

    
    #region Selectable
    
    public bool CanSelect => _controlsInstance && _controlsInstance.activeSelf ;
    
    public bool IsSelected { get; private set; }
    
    public Transform SelectTransform => _controlsInstance ? _controlsInstance.transform : null;
    public Collider2D InteractionCollider => 
      _controlsInstance? 
      _controlsInstance.GetComponentInChildren<Collider2D>() 
      : null;
    
    public void MarkSelected(bool selected = true) => IsSelected = selected;

    public void OnSelect() => MarkSelected();

    public void OnDeselect() => MarkSelected(false);
    
    #endregion
  }
}