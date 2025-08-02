using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// Manages a _grid system for snapping positions and calculating _grid coordinates.
  /// </summary>
  /// <remarks>This class provides functionality to align positions to a _grid and determine the _grid coordinates
  /// of a given position. The _grid is defined by a cell size and an Origin point, which can be configured using the
  /// <see cref="CellSize"/> and <see cref="Origin"/> fields.</remarks>
  public class GridManager : MonoBehaviour {

    public float CellSize = 1f; // Matches your peg spacing
    public Vector2 Origin = Vector2.zero;

    [SerializeField] private Material ghostMaterial; // Transparent shader
    private GameObject _ghostPreview;

    [SerializeField] private GameObject snappableUIPrefab;
    private GameObject _activeUI;


#if UNITY_EDITOR
    public IReadOnlyList<GridElementView> EditorGridView => _editorGridView;
#endif

    [SerializeField, HideInInspector]
    private List<GridElementView> _editorGridView = new();

    private Dictionary<Vector2Int, GridSnappable> _gridElements = new();

    public event Action<GridSnappable, Vector2Int> OnElementRegistered;
    public event Action<GridSnappable, Vector2Int> OnElementRemoved;


    #region MonoBehaviour methods

    void Start() {
      // Find all GridInteractive components in the scene
      GridSnappable[] foundElements = FindObjectsByType<GridSnappable>(FindObjectsSortMode.None);

      foreach (var element in foundElements) {
        if (!RegisterElement(element)) {
          Debug.LogWarning($"[GridManager] Failed to register element at {element.transform.position}");
        }
      }
      Debug.Log($"[GridManager] Registered {foundElements.Length} initial grid elements.");
    }

    private void OnDestroy() {
      if (_ghostPreview != null) Destroy(_ghostPreview);
    }


    private void OnEnable() => GridSnappableUIController.OnRemoveRequested += HandleRemoveRequest;

    private void OnDisable() => GridSnappableUIController.OnRemoveRequested -= HandleRemoveRequest;

    private void HandleRemoveRequest(GridSnappable snappable) => RemoveElement(snappable);

    #endregion

    #region Element management
    public bool RegisterElement(GridSnappable element) {
      Vector2Int coord = GetGridCoord(element.transform.position);

      if (_gridElements.ContainsKey(coord)) {
        Debug.LogWarning($"[GridManager] Grid cell {coord} already occupied.");
        return false;
      }
      _gridElements[coord] = element;
      element.transform.position = SnapToGrid(element.transform.position);
      Debug.Log($"Element snapped to grid at {coord}");
      element.OnRegistered(coord);
      OnElementRegistered?.Invoke(element, coord);

#if UNITY_EDITOR
      _editorGridView.Add(new GridElementView { Coord = coord, Element = element });
#endif
      return true;
    }

    public void RemoveElement(GridSnappable element) {
      Vector2Int coord = GetGridCoord(element.transform.position);
      if (_gridElements.ContainsKey(coord) && _gridElements[coord] == element) {
        _gridElements.Remove(coord);
        element.OnRemoved(coord);
        OnElementRemoved?.Invoke(element, coord);
      }
    }

    public GridSnappable GetElementAtScreenPosition(Vector2 screenPos) {
      Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
      RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
      //return gameObject on collider if it has a GridInteractive component
      if (hit.collider != null && (hit.collider.gameObject is var go)) {
        if (go.TryGetComponent(out GridSnappable gridElement)) {
          return gridElement;
        }
        else {
          Debug.Log($"[GridManager] GameObject found at {screenPos} (worldPos:{worldPos} does not have a GridInteractive compatible component");
        }
      }
      //else {
      //  Debug.Log($"[GridManager] No GameObject found at {screenPos} (worldPos:{worldPos}");
      //}
      return null;
    }
    public bool TryGetElementAtScreenPosition(Vector2 screenPos, out GridSnappable element) {
      element = GetElementAtScreenPosition(screenPos);
      return element != null;
    }
    public bool TryGetElementCoord(GameObject element, out Vector2Int coord) {
      foreach (var kvp in _gridElements) {
        if (kvp.Value == element) {
          coord = kvp.Key;
          return true;
        }
      }
      coord = default;
      return false;
    }

    public bool IsOccupied(Vector2Int coord) => _gridElements.ContainsKey(coord);

    public bool IsOccupied(Vector2 position) => IsOccupied(GetGridCoord(position));

    public void ShowSnappableUI(GridSnappable snappable) {
      if (_activeUI != null) Destroy(_activeUI);

      _activeUI = Instantiate(snappableUIPrefab);
      _activeUI.transform.position = snappable.transform.position + Vector3.up * 1.5f;

      var controller = _activeUI.GetComponent<GridSnappableUIController>();
      controller.Bind(snappable);
    }

    public void HideSnappableUI() {
      if (_activeUI != null) {
        _activeUI.GetComponent<GridSnappableUIController>()?.Unbind();
        Destroy(_activeUI);
      }
    }


    #endregion


    #region Ghot Preview

    public void ShowGhostPreview(GridSnappable sourceElement, Vector2 worldPosition) {
      if (_ghostPreview == null) {
        _ghostPreview = new GameObject("GhostPreview");
        var sr = _ghostPreview.AddComponent<SpriteRenderer>();
        sr.material = ghostMaterial;
        sr.sortingLayerName = "Foreground"; // Optional
        sr.sortingOrder = 100;              // Renders above normal elements
      }

      if (sourceElement.TryGetComponent(out SpriteRenderer sourceRenderer)
        && sourceRenderer.sprite is var sourceSprite) {
        var sr = _ghostPreview.GetComponent<SpriteRenderer>();
        sr.sprite = sourceSprite;
      }

      Vector2 snapPos = SnapToGrid(worldPosition);
      _ghostPreview.transform.position = snapPos;
      _ghostPreview.SetActive(true);
    }

    public void HideGhostPreview() {
      if (_ghostPreview != null)
        _ghostPreview.SetActive(false);
    }

    #endregion

    #region Grid utilities
    public Vector2 SnapToGrid(Vector2 position) {
      float x = Mathf.Round((position.x - Origin.x) / CellSize) * CellSize + Origin.x;
      float y = Mathf.Round((position.y - Origin.y) / CellSize) * CellSize + Origin.y;
      return new Vector2(x, y);
    }

    public Vector2Int GetGridCoord(Vector2 position) {
      int x = Mathf.RoundToInt((position.x - Origin.x) / CellSize);
      int y = Mathf.RoundToInt((position.y - Origin.y) / CellSize);
      return new Vector2Int(x, y);
    }
    #endregion

#if UNITY_EDITOR
    [ContextMenu("Scan Elements")]
    private void EditorScanAndRegisterElements() {
      var foundElements = FindObjectsByType<GridSnappable>(FindObjectsSortMode.None);

      int registeredCount = 0;
      foreach (var element in foundElements) {
        if (RegisterElement(element)) {
          registeredCount++;
        }
        else {
          Debug.LogWarning($"[GridManager] Failed to register element at {element.transform.position}");
        }
      }

      Debug.Log($"[GridManager] Editor scan registered {registeredCount} of {foundElements.Length} elements.");
    }
#endif
  }


}