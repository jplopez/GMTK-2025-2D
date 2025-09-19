using UnityEngine;
using Ameba;

namespace GMTK {
    /// <summary>
    /// Component that enables PlayableElement to snap to a LevelGrid.
    /// Provides pure grid-based positioning and snapping logic without feedback - feedback should be handled by separate components.
    /// Relies on LevelGrid's event system for occupancy management.
    /// Requires manual assignment of LevelGrid reference in the inspector.
    /// </summary>
    [AddComponentMenu("GMTK/Playable Element Components/Snappable Component")]
    public class SnappableElementComponent : PlayableElementComponent {

        [Header("Grid Settings")]
        [Tooltip("Reference to the LevelGrid this element should snap to. Must be assigned in inspector.")]
        public LevelGrid LevelGrid;

        [Header("Snapping Behavior")]
        [Tooltip("If true, element will automatically snap to grid when moved")]
        public bool AutoSnapToGrid = true;

        [Tooltip("If true, element will check for valid placement before snapping")]
        public bool ValidateBeforeSnap = true;

        [Header("Grid Preview")]
        [Tooltip("Visual indicator for grid preview (optional)")]
        public GameObject GridPreviewIndicator;

        // Private fields
        private Vector2Int _currentGridPosition;
        private Vector2Int _previousGridPosition;
        private bool _hasValidGridReference = false;

        #region PlayableElementComponent Implementation

        protected override void Initialize() {
            // Check for required LevelGrid reference
            if (LevelGrid == null) {
                this.LogWarning($"LevelGrid is not assigned on {gameObject.name}. SnappableElementComponent will not function.");
                _hasValidGridReference = false;
                return;
            }

            _hasValidGridReference = true;

            // Initialize grid preview if provided
            if (GridPreviewIndicator != null) {
                GridPreviewIndicator.SetActive(false);
            }

            // Don't check initial position here - let LevelGrid handle registration through events
            // Just update our current grid position for tracking
            if (_hasValidGridReference) {
                UpdateCurrentGridPosition();
            }
        }

        protected override bool Validate() {
            return _hasValidGridReference && _playableElement != null;
        }

        protected override void OnUpdate() {
            if (!Validate()) return;

            // Update grid position tracking
            UpdateGridPositionTracking();
        }

        protected override void ResetComponent() {
            // Re-initialize
            Initialize();
        }

        #endregion

        #region Grid Position Management

        private void UpdateCurrentGridPosition() {
            if (!Validate()) return;
            
            Vector3 currentWorldPosition = _playableElement.GetPosition();
            _currentGridPosition = LevelGrid.WorldToGrid(currentWorldPosition);
        }

        private void UpdateGridPositionTracking() {
            Vector3 currentWorldPosition = _playableElement.GetPosition();
            Vector2Int currentGridPos = LevelGrid.WorldToGrid(currentWorldPosition);

            // Check if grid position has changed
            if (currentGridPos != _currentGridPosition) {
                _previousGridPosition = _currentGridPosition;
                _currentGridPosition = currentGridPos;
                
                // Just track the change - don't handle occupancy registration here
                OnGridPositionChanged();
            }
        }

        private void OnGridPositionChanged() {
            // Only handle visual updates and snapping - let LevelGrid handle occupancy
            this.LogDebug($"Grid position changed from {_previousGridPosition} to {_currentGridPosition}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually snap the element to the nearest grid position
        /// </summary>
        public void SnapToNearestGridPosition() {
            if (!Validate()) return;

            Vector3 currentPosition = _playableElement.GetPosition();
            Vector2 snapPosition = LevelGrid.SnapToGrid(currentPosition);

            _playableElement.UpdatePosition(snapPosition);
            this.LogDebug($"Snapped {gameObject.name} to position {snapPosition}");
        }

        /// <summary>
        /// Get the current grid position of the element
        /// </summary>
        public Vector2Int GetCurrentGridPosition() {
            if (!Validate()) return Vector2Int.zero;

            return LevelGrid.WorldToGrid(_playableElement.GetPosition());
        }

        /// <summary>
        /// Check if the element is currently inside the playable grid area
        /// </summary>
        public bool IsInPlayableArea() {
            if (!Validate()) return false;

            return LevelGrid.IsInsidePlayableArea(_playableElement.GetPosition());
        }

        /// <summary>
        /// Check if the element can be placed at a specific world position
        /// </summary>
        public bool CanPlaceAt(Vector3 worldPosition) {
            if (!Validate()) return false;

            Vector2Int gridPos = LevelGrid.WorldToGrid(worldPosition);
            return LevelGrid.CanPlace(_playableElement, gridPos);
        }

        /// <summary>
        /// Convert grid coordinates to world coordinates
        /// </summary>
        public Vector2 GridToWorldPosition(Vector2Int gridPosition) {
            if (!Validate()) return Vector2.zero;

            return LevelGrid.GridToWorld(gridPosition);
        }

        /// <summary>
        /// Get grid size information
        /// </summary>
        public Vector2Int GetGridSize() {
            if (!Validate()) return Vector2Int.zero;
            
            return LevelGrid.GridSize;
        }

        /// <summary>
        /// Get grid cell size
        /// </summary>
        public float GetCellSize() {
            if (!Validate()) return 0f;
            
            return LevelGrid.CellSize;
        }

        /// <summary>
        /// Enable or disable automatic grid snapping
        /// </summary>
        public void SetAutoSnap(bool enabled) {
            AutoSnapToGrid = enabled;
        }

        #endregion

        #region Grid Preview Management

        private void UpdateGridPreview() {
            if (GridPreviewIndicator == null || !Validate()) return;

            if (_playableElement.IsBeingDragged) {
                GridPreviewIndicator.SetActive(true);
                Vector2Int targetGridPos = LevelGrid.WorldToGrid(_playableElement.GetPosition());
                Vector2 previewPosition = LevelGrid.GridToWorld(targetGridPos);
                GridPreviewIndicator.transform.position = previewPosition;
            }
            else {
                GridPreviewIndicator.SetActive(false);
            }
        }

        #endregion

        #region PlayableElement Event Handlers

        protected override void HandleDragStart(PlayableElementEventArgs evt) {
            if (!Validate()) return;

            // Show grid preview
            UpdateGridPreview();
            
            this.LogDebug($"Started dragging {gameObject.name}");
        }

        protected override void HandleDragUpdate(PlayableElementEventArgs evt) {
            if (!Validate()) return;

            // Update grid preview
            UpdateGridPreview();
        }

        protected override void HandleDragEnd(PlayableElementEventArgs evt) {
            if (!Validate()) return;

            // Update grid preview
            UpdateGridPreview();

            // Handle final positioning only if no other component handled the event
            if (!evt.Handled) {
                Vector3 finalPosition = _playableElement.GetPosition();

                if (LevelGrid.IsInsidePlayableArea(finalPosition)) {
                    // Snap to grid if enabled
                    if (AutoSnapToGrid) {
                        Vector2 snapPosition = LevelGrid.SnapToGrid(finalPosition);
                        _playableElement.UpdatePosition(snapPosition);
                        this.LogDebug($"Auto-snapped {gameObject.name} to {snapPosition}");
                    }
                    
                    // LevelGrid will handle the occupancy registration through its event system
                } else {
                    this.LogDebug($"{gameObject.name} moved outside playable area");
                }
            }
        }

        #endregion

        #region Legacy GridSnappable Event Handlers (Required by base class)

        protected override void HandleElementSelected(GridSnappableEventArgs evt) {
            // Handle legacy GridSnappable events if needed for compatibility
            if (evt.Element != null && evt.Element.gameObject == gameObject) {
                this.LogDebug($"Legacy element selected: {gameObject.name}");
            }
        }

        protected override void HandleElementDropped(GridSnappableEventArgs evt) {
            // Handle legacy GridSnappable events if needed for compatibility
            if (evt.Element != null && evt.Element.gameObject == gameObject) {
                this.LogDebug($"Legacy element dropped: {gameObject.name}");
            }
        }

        protected override void HandleElementHovered(GridSnappableEventArgs evt) {
            // Handle legacy GridSnappable events if needed
        }

        protected override void HandleElementUnhovered(GridSnappableEventArgs evt) {
            // Handle legacy GridSnappable events if needed
        }

        #endregion

        #region Debug Utilities

        private void OnDrawGizmosSelected() {
            if (!Validate()) return;

            // Draw current grid position
            Vector2Int currentGridPos = GetCurrentGridPosition();
            Vector2 worldPos = LevelGrid.GridToWorld(currentGridPos);

            Gizmos.color = IsInPlayableArea() ? Color.green : Color.red;
            Gizmos.DrawWireCube(worldPos, Vector3.one * LevelGrid.CellSize * 0.9f);

            // Draw footprint if element has multiple cells
            var footprint = _playableElement.GetFootprint();
            if (footprint.Count > 1) {
                Gizmos.color = Color.yellow;
                foreach (var cellOffset in footprint) {
                    Vector2Int cellPos = currentGridPos + cellOffset;
                    Vector2 cellWorldPos = LevelGrid.GridToWorld(cellPos);
                    Gizmos.DrawWireCube(cellWorldPos, Vector3.one * LevelGrid.CellSize * 0.8f);
                }
            }
            
            // Draw grid bounds
            if (LevelGrid.GridTopBound != null) {
                Gizmos.color = Color.cyan;
                var bounds = LevelGrid.GridTopBound.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

        #endregion
    }
}