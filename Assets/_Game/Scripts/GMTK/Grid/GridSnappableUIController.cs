using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GMTK {
  public class GridSnappableUIController : MonoBehaviour {
    [Header("UI Buttons")]
    [SerializeField] private Button rotateCWButton;
    [SerializeField] private Button rotateCCWButton;
    [SerializeField] private Button flipXButton;
    [SerializeField] private Button flipYButton;
    [SerializeField] private Button removeButton;

    [Header("OnHover Settings")]
    [SerializeField] private float verticalOffset = 1.5f;
    [SerializeField] private float hoverThreshold = 0.3f;

    public static event Action<GridSnappable> OnRemoveRequested;

    private Coroutine hoverCoroutine;
    private GridSnappable target;

    void OnEnable() {
      PlayerInputController.OnElementHovered += HandleHoverStart;
      PlayerInputController.OnElementUnhovered += HandleHoverEnd;
    }

    void OnDisable() {
      PlayerInputController.OnElementHovered -= HandleHoverStart;
      PlayerInputController.OnElementUnhovered -= HandleHoverEnd;
    }

    private void HandleHoverStart(GridSnappable target) {
      if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
      hoverCoroutine = StartCoroutine(DelayedShow(target));
    }

    private void HandleHoverEnd() {
      if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
      hoverCoroutine = StartCoroutine(DelayedHide());
    }

    private IEnumerator DelayedShow(GridSnappable snappable) {
      yield return new WaitForSeconds(hoverThreshold);
      target = snappable;
      transform.position = snappable.transform.position + Vector3.up * verticalOffset;
      Bind(snappable);
      gameObject.SetActive(true);
    }

    private IEnumerator DelayedHide() {
      yield return new WaitForSeconds(hoverThreshold);
      Unbind();
      target = null;
      gameObject.SetActive(false);
    }


    public void Bind(GridSnappable snappable) {
      target = snappable;

      // Enable/disable buttons based on capabilities
      rotateCWButton.gameObject.SetActive(snappable.CanRotate);
      rotateCCWButton.gameObject.SetActive(snappable.CanRotate);
      flipXButton.gameObject.SetActive(snappable.Flippable);
      flipYButton.gameObject.SetActive(snappable.Flippable);

      // Wire up button events
      rotateCWButton.onClick.AddListener(() => target.RotateClockwise());
      rotateCCWButton.onClick.AddListener(() => target.RotateCounterClockwise());
      flipXButton.onClick.AddListener(() => target.FlipX());
      flipYButton.onClick.AddListener(() => target.FlipY());
      removeButton.onClick.AddListener(() => OnRemoveRequested?.Invoke(target));
    }

    public void Unbind() {
      rotateCWButton.onClick.RemoveAllListeners();
      rotateCCWButton.onClick.RemoveAllListeners();
      flipXButton.onClick.RemoveAllListeners();
      flipYButton.onClick.RemoveAllListeners();
      removeButton.onClick.RemoveAllListeners();
      target = null;
    }
  }
}