using GMTK.Extensions;
using System;
using Ameba;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GMTK
{
  public enum ControlsBindingMode
  {
    Tags,
    Manual
  }

  public enum ControlsBindingTags
  {
    InputRotateCw,
    InputRotateCcw,
    InputFlipX,
    InputFlipY
  }

  /// <summary>
  /// Component responsible for show/hide the UI controls for the PlayableElement, and binding them to the element's actions.<br/>
  /// This component uses tags on the children of the ControlsPrefab to identify which input should be bound to which action of the PlayableElement.
  /// The expected tags are defined in the 'ControlsBindingTags' enum.<br/>
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Element Controls")]
  public class ElementUIControlsComponent : PlayableElementComponent, ISelectable
  {
    // Tags assigned to GameObjects in the ControlsPrefab to identify them as inputs to bind to the PlayableElement.

    [Header("Controls Prefab"), Space] public Transform ControlsPrefab;

    [Tooltip("Offset from the element's position to instantiate the controls prefab")]
    public Vector2 PrefabOffset = new(0f, 0f);

    public bool StartVisible;

    [Header("Controls Binding"), Space]
    [Help(
       "'BindingMode' tells how to setup the controls to listen player inputs. 'Tags' : the component looks for GameObjects with predefined tags (see Input_* tags). 'Manual': the component will not try to bind the controls, and will rely on external setup"),
     Space]
    public ControlsBindingMode BindingMode = ControlsBindingMode.Tags;

    [Header("Feedbacks")] public MMF_Player OnSelectedFeedback;

    private static string[] InputTagsNames => Enum.GetNames(typeof(ControlsBindingTags));

    private GameObject _controlsInstance;
    private bool _inputsBound;
    
    #region Initialization


    protected override void Initialize()
    {
      if (!TryResolveOrCreateControlsInstance(StartVisible))
      {
        this.LogError("Controls instance could not be resolved.");
        return;
      }

      if (BindingMode == ControlsBindingMode.Tags && !_inputsBound)
      {
        InitializeControlsUsingTags();
        _inputsBound = true;
      }
      UpdateControlsPosition();
    }

    private bool TryResolveOrCreateControlsInstance(bool setActive)
    {
      // If we already have a live instance, just update active state.
      if (_controlsInstance)
      {
        _controlsInstance.SetActive(setActive);
        return true;
      }

      // Prefer existing embedded child under this element (your current prefab setup).
      if (TryGetExistingEmbeddedControls(out var embedded))
      {
        _controlsInstance = embedded;
        _controlsInstance.SetActive(setActive);
        return true;
      }

      // Fallback: instantiate from assigned prefab reference.
      if (!ControlsPrefab)
      {
        return false;
      }

      _controlsInstance = Instantiate(ControlsPrefab.gameObject, _playableElement.transform);
      _controlsInstance.SetActive(setActive);
      return true;
    }

    private bool TryGetExistingEmbeddedControls(out GameObject controlsObject)
    {
      controlsObject = null;

      if (!_playableElement) return false;

      // If ControlsPrefab is a valid scene/prefab-instance reference and already parented to this element, use it directly.
      if (ControlsPrefab && ControlsPrefab.IsChildOf(_playableElement.transform))
      {
        controlsObject = ControlsPrefab.gameObject;
        return true;
      }

      // Name-based fallback: find first child with same name as ControlsPrefab (if assigned).
      if (!ControlsPrefab) return false;
      var t = _playableElement.transform.Find(ControlsPrefab.name);
      
      if (!t) return false;
      controlsObject = t.gameObject;
      return true;
    }
    
    private void InitializeControlsUsingTags()
    {
      if (!_controlsInstance) return;
      
      //find children components by tag and bind them to the playable element
      var foundInputs = _controlsInstance.transform.FindChildrenWithAnyTag(InputTagsNames);
      foreach (var inputTransform in foundInputs)
      {
        SetupInputAction(inputTransform, ResolveMethodFromTag(inputTransform.tag));
      }
    }

    private UnityAction ResolveMethodFromTag(string controlBindingTag)
    {
      if (string.IsNullOrEmpty(controlBindingTag)) return null;
      UnityAction onClickAction = null;
      switch (controlBindingTag)
      {
        case "InputRotateCw":
          onClickAction = RotateCw;
          break;
        case "InputRotateCcw":
          onClickAction = RotateCcw;
          break;
        case "InputFlipX":
          onClickAction = FlipX;
          break;
        case "InputFlipY":
          onClickAction = FlipY;
          break;
        default:
          this.LogWarning($"Input tag not recognized: {controlBindingTag}. No action will be bound to this input.");
          break;
      }

      return onClickAction;
    }

    private void SetupInputAction(Transform inputTransform, UnityAction onClickAction)
    {
      if (!inputTransform || onClickAction == null) return;

      if (inputTransform.TryGetComponent<Button>(out var button))
      {
        button.onClick.AddListener(onClickAction);
      }
      else
      {
        this.LogWarning($"Input '{inputTransform.name}' does not have a Button component.");
      }
    }

    #endregion


    #region PlayableElementComponent overrides

    private bool IsEventValid(PlayableElementEventArgs evt) => evt.Element == _playableElement;

    protected override bool Validate() => ControlsPrefab;

    public override void OnSelected(PlayableElementEventArgs evt) => HandleEvent(evt, true);

    public override void OnDeselected(PlayableElementEventArgs evt) => HandleEvent(evt, false);

    private void HandleEvent(PlayableElementEventArgs evt, bool setActive)
    {
      if (!IsEventValid(evt)) return;
      TryResolveOrCreateControlsInstance(setActive);
    }

    protected override void OnUpdate()
    {
      base.OnUpdate();
      UpdateControlsPosition();
    }

    #endregion


    #region UI Controls Management

    // private void CreateOrUpdateControlsPrefab(bool setActive)
    // {
    //   if (!ControlsPrefab) return;
    //   if(!_controlsInstance) _controlsInstance = Instantiate(ControlsPrefab.gameObject, _playableElement.transform);
    //   _controlsInstance.SetActive(setActive);
    // }

    private void UpdateControlsPosition()
    {
      if (!_controlsInstance || !_playableElement) return;
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

    public bool CanSelect => _controlsInstance && _controlsInstance.activeSelf;

    public bool IsSelected { get; private set; }

    public Transform SelectTransform => _controlsInstance ? _controlsInstance.transform : gameObject.transform;

    public Collider2D InteractionCollider =>
      _controlsInstance
        ? _controlsInstance.transform.GetComponentInChildren<Collider2D>()
        : gameObject.GetComponent<Collider2D>();

    public void MarkSelected(bool selected = true) => IsSelected = selected;

    public void OnSelect() => MarkSelected();

    public void OnDeselect() => MarkSelected(false);

    #endregion
  }
}