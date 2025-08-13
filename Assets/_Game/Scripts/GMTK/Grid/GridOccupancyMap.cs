using System.Collections.Generic;
using UnityEngine;

namespace GMTK {

  //The order to render the elements in a cell. 
  // FistToLast: the first to enter the cell is shown first in the screen
  // LastToFirst: the last to enter the cell is shown first in the screen
  public enum CellLayeringOrder {

    FirstToLast, // ie: FIFO: first in, first out
    LastToFirst  // ie: LIFO - last in, first out
  }

  /// <summary>
  /// Represents the current _occupancy of cells within a LevelGrid
  /// Supports having multiple elements per cell, and a Layer grouping _mode using <c>CellLayeringOrder</c>
  /// </summary>
  public class GridOccupancyMap {
    protected readonly Dictionary<Vector2Int, OccupancyCell> _occupancy = new();
    protected readonly float _cellSize;
    protected readonly Vector2 _gridOrigin;

    protected readonly int _maxOccupantsPerCell;
    protected readonly CellLayeringOrder _mode;

    public Dictionary<Vector2Int, OccupancyCell> GetAllCells() => _occupancy;

    public GridOccupancyMap(float cellSize, Vector2 gridOrigin, int maxOccupantsPerCell = 1, CellLayeringOrder mode = CellLayeringOrder.LastToFirst) {
      _cellSize = cellSize;
      _gridOrigin = gridOrigin;
      _maxOccupantsPerCell = maxOccupantsPerCell;
      _mode = mode;
    }

    public virtual bool Register(GridSnappable snappable) {
      var index = WorldToGrid(snappable.transform.position);
      if (!_occupancy.TryGetValue(index, out var cell)) {
        cell = new OccupancyCell(_maxOccupantsPerCell, _mode);
        _occupancy[index] = cell;
      }
      if (cell.IsFull) return false;

      cell.Add(snappable);
      return true;
    }

    public virtual bool Register(GridSnappable snappable, Vector2Int origin) {
      List<OccupancyCell> cells = new();
      foreach (Vector2Int snappableIndex in snappable.GetWorldOccupiedCells(origin)) {

        if (!_occupancy.TryGetValue(snappableIndex, out OccupancyCell occupancyCell)) {
          occupancyCell = new OccupancyCell(_maxOccupantsPerCell, _mode);
          _occupancy[snappableIndex] = occupancyCell;
        }
        if (occupancyCell.IsFull) return false;
        cells.Add(occupancyCell);
      }
      //if foreach finishes, it means all cells are available so we assign the snappable to them
      cells.ForEach(c => c.Add(snappable));
      return true;
    }

    private bool IsCellFull(Vector2Int cellIndex) {
      if (!_occupancy.TryGetValue(cellIndex, out OccupancyCell occupancyCell)) {
        occupancyCell = new OccupancyCell(_maxOccupantsPerCell, _mode);
      }
      return (occupancyCell.IsFull);
    }

    public virtual void Unregister(GridSnappable snappable) {
      var index = WorldToGrid(snappable.transform.position);
      if (_occupancy.TryGetValue(index, out var cell)) {
        cell.Remove(snappable);
        if (cell.IsEmpty) _occupancy.Remove(index);
      }
    }

    /// <summary>
    /// Does it have any elements on that position
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public virtual bool IsOccupied(Vector2 worldPosition) {
      var index = WorldToGrid(worldPosition);
      return _occupancy.ContainsKey(index);
    }

    /// <summary>
    /// Does it have the maximum number of elements as defined by readonly <c>_maxOccupantsPerCell</c>
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public virtual bool IsFull(Vector2 worldPosition) {
      var index = WorldToGrid(worldPosition);
      return IsCellFull(index);
    }

    public virtual IEnumerable<GridSnappable> GetOccupants(Vector2 worldPosition) {
      var index = WorldToGrid(worldPosition);
      return _occupancy.TryGetValue(index, out var cell) ? cell.GetOccupants() : new List<GridSnappable>();
    }

    public virtual GridSnappable PeekTop(Vector2 worldPosition) {
      var index = WorldToGrid(worldPosition);
      return _occupancy.TryGetValue(index, out var cell) ? cell.PeekTop() : null;
    }

    private Vector2Int WorldToGrid(Vector2 position) {
      int x = Mathf.RoundToInt((position.x - _gridOrigin.x) / _cellSize);
      int y = Mathf.RoundToInt((position.y - _gridOrigin.y) / _cellSize);
      return new Vector2Int(x, y);
    }
  }


  /// <summary>
  /// Represents the occupants of one cell in a LevelGrid
  /// Uses <c>CellLayeringMode</c> to determine how to order the elements for rendering
  /// </summary>
  public class OccupancyCell {

    private readonly int maxOccupants;
    private readonly CellLayeringOrder mode;
    private readonly LinkedList<GridSnappable> occupants = new();
    public int Count => occupants.Count;

    public OccupancyCell(int maxOccupants, CellLayeringOrder mode) {
      this.maxOccupants = maxOccupants;
      this.mode = mode;
    }

    public bool IsFull => occupants.Count >= maxOccupants;
    public bool IsEmpty => occupants.Count == 0;

    public void Add(GridSnappable snappable) {
      if (IsFull) return;

      switch (mode) {
        case CellLayeringOrder.FirstToLast:
          occupants.AddLast(snappable);
          break;
        case CellLayeringOrder.LastToFirst:
          occupants.AddLast(snappable);
          break;
      }
    }

    public void Remove(GridSnappable snappable) {
      occupants.Remove(snappable);
    }

    public IEnumerable<GridSnappable> GetOccupants() => occupants;
    public GridSnappable PeekTop() => occupants.First?.Value;
  }

}