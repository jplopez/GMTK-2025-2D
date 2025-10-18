using System.Collections.Generic;
using System.Linq;
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
  /// Represents the current occupancy of cellsToRemove within a LevelGrid
  /// Supports having multiple elements per cell, and Layer Order <c>CellLayeringOrder</c>
  /// </summary>
  public class GridOccupancyMap {

    //
    protected readonly Dictionary<Vector2Int, OccupancyCell> _occupancy = new();

    protected readonly Dictionary<PlayableElement, List<Vector2Int>> _occupantFootprint = new();
    protected int _occupantsCount = 0;

    protected readonly float _cellSize;
    protected readonly Vector2 _gridOrigin;
    protected readonly int _maxOccupantsPerCell;
    protected readonly CellLayeringOrder _mode;

    /// <summary>
    /// Returns all cells in the map (occupied or not)
    /// </summary>
    /// <returns></returns>
    public Dictionary<Vector2Int, OccupancyCell> GetAllCells() => _occupancy;

    public GridOccupancyMap(float cellSize, Vector2 gridOrigin, int maxOccupantsPerCell = 1, CellLayeringOrder mode = CellLayeringOrder.LastToFirst) {
      _cellSize = cellSize;
      _gridOrigin = gridOrigin;
      _maxOccupantsPerCell = maxOccupantsPerCell;
      _mode = mode;
    }

    #region Register/Unregister

    /// <summary>
    /// Adds the specified occupant into the grid, using the occupant's world position to calculate the Grid cell.<br/>
    /// This methods takes into consideration the occupant footprint (how many cells needs to fit), and if the cells
    /// have room left.
    /// 
    /// </summary>
    /// <param name="occupant"></param>
    /// <param name="origin">The grid position of grid cell (0,0)</param>
    /// <returns>True if the occupant is placed into the grid. False otherwise</returns>
    public virtual bool Register(PlayableElement occupant, Vector2Int origin) {
      List<OccupancyCell> cells = new();
      foreach (Vector2Int occupantCellIndex in occupant.GetWorldOccupiedCells(origin)) {

        if (!_occupancy.TryGetValue(occupantCellIndex, out OccupancyCell occupancyCell)) {
          occupancyCell = new OccupancyCell(_maxOccupantsPerCell, _mode);
          _occupancy[occupantCellIndex] = occupancyCell;
        }
        if (occupancyCell.HasReachedMaxOccupancy) return false;
        cells.Add(occupancyCell);
      }
      //if foreach finishes, it means all cellsToRemove are available so we assign the element to them
      cells.ForEach(c => c.Add(occupant));
      _occupantFootprint.Add(occupant, occupant.GetFootprint());
      _occupantsCount++;
      return true;
    }

    public virtual void Unregister(PlayableElement occupant, Vector2Int origin) {
      List<OccupancyCell> cellsToRemove = new();
      var occupiedCells = //element.GetWorldOccupiedCells(origin);
                            _occupancy.Where(cell => cell.Value.Count > 0 && cell.Value.Contains(occupant)).Select(pair => pair.Key).ToList();
      foreach (Vector2Int snappableIndex in occupiedCells) {
       if(_occupancy.TryGetValue(snappableIndex, out OccupancyCell occupancyCell)) {
          cellsToRemove.Add(occupancyCell);
        }
      }
      if(cellsToRemove.Count != occupant.GetFootprint().Count) {
        Debug.LogWarning($"Unregister: number of cellsToRemove to remove doesn't match element footprint: {cellsToRemove.Count} != {occupant.GetFootprint().Count}");
      }
      cellsToRemove.ForEach(c => c.Remove(occupant));
      _occupantFootprint.Remove(occupant);
      _occupantsCount--;
      //Debug.Log($"Snappable {element.name} unregistered:");
      //cellsToRemove.ForEach(c => Debug.Log($"  {c}"));
    }

    #endregion

    #region Occupancy
    public bool ContainsElement(PlayableElement element) => _occupancy.Values.Any(cell => cell.Contains(element));

    /// <summary>
    /// How many occupants (elements) are registered in the grid
    /// </summary>
    public int OccupantsCount => _occupantsCount;

    /// <summary>
    /// Does it have any elements on that world position
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public virtual bool HasAnyOccupantsInWorldPosition(Vector2 worldPosition) {
      var index = WorldToGrid(worldPosition);
      return _occupancy.ContainsKey(index) && _occupancy[index].HasAnyOccupant;
    }

    /// <summary>
    /// Does it have the maximum number of elements as defined by readonly <c>_maxOccupantsPerCell</c>
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public virtual bool HasWorldPositionReachedMaxOccupancy(Vector2 worldPosition) {
      var index = WorldToGrid(worldPosition);
      return HasCellReachedMaxOccupancy(index);
    }

    public virtual IEnumerable<PlayableElement> GetOccupantsInWorldPosition(Vector2 worldPosition) {
      var index = WorldToGrid(worldPosition);
      return _occupancy.TryGetValue(index, out var cell) ? cell.GetOccupants() : new List<PlayableElement>();
    }

    public int OccupantsCountByWorldPosition(Vector2 worldPosition) {
      var index = WorldToGrid(worldPosition);
      return _occupancy.TryGetValue(index, out var cell) ? cell.Count : 0;
    }

    public virtual PlayableElement PeekTopByWorldPosition(Vector2 worldPosition) {
      var index = WorldToGrid(worldPosition);
      return _occupancy.TryGetValue(index, out var cell) ? cell.PeekTop() : null;
    }

    #endregion

    #region Visualization

    public IEnumerable<Vector2Int> GetOccupiedCells() => _occupancy.Keys.Where( k => _occupancy[k].Count >0);

    public int GetOccupancyCellCount(Vector2Int cell) => _occupancy.TryGetValue(cell, out var occupancyCell) ? occupancyCell.Count : 0;

    public bool HasCellReachedMaxOccupancy(Vector2Int cellIndex) =>
    _occupancy.TryGetValue(cellIndex, out OccupancyCell occupancyCell) && occupancyCell.HasReachedMaxOccupancy;


    #endregion

    private Vector2Int WorldToGrid(Vector2 position) {
      int x = Mathf.RoundToInt((position.x - _gridOrigin.x) / _cellSize);
      int y = Mathf.RoundToInt((position.y - _gridOrigin.y) / _cellSize);
      return new Vector2Int(x, y);
    }
  }

}