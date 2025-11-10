using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ameba {

  /// <summary>
  /// A generic 2D grid for storing and managing objects in a tile-based layout.
  /// Supports adding, removing, and retrieving objects from specific grid positions.
  /// </summary>
  public class AmebaGrid {

    private const int MAX_GRID_SIZE = 1000;

    private readonly int _rows;
    private readonly int _columns;
    private readonly float _tileSize;
    private readonly Dictionary<Vector2Int, object> _tiles;

    /// <summary>
    /// Gets the number of rows in the grid.
    /// </summary>
    public int Rows => _rows;

    /// <summary>
    /// Gets the number of columns in the grid.
    /// </summary>
    public int Columns => _columns;

    /// <summary>
    /// Gets the size of each tile in world units.
    /// </summary>
    public float TileSize => _tileSize;

    /// <summary>
    /// Initializes a new instance of the AmebaGrid class.
    /// </summary>
    /// <param name="rows">Number of rows in the grid (max 1000).</param>
    /// <param name="columns">Number of columns in the grid (max 1000).</param>
    /// <param name="tileSize">Size of each tile in world units.</param>
    /// <exception cref="ArgumentException">Thrown when rows or columns exceed maximum size.</exception>
    public AmebaGrid(int rows, int columns, float tileSize) {
      if (rows <= 0 || rows > MAX_GRID_SIZE) {
        throw new ArgumentException($"Rows must be between 1 and {MAX_GRID_SIZE}", nameof(rows));
      }
      if (columns <= 0 || columns > MAX_GRID_SIZE) {
        throw new ArgumentException($"Columns must be between 1 and {MAX_GRID_SIZE}", nameof(columns));
      }
      if (tileSize <= 0) {
        throw new ArgumentException("Tile size must be greater than 0", nameof(tileSize));
      }

      _rows = rows;
      _columns = columns;
      _tileSize = tileSize;
      _tiles = new Dictionary<Vector2Int, object>();
    }

    /// <summary>
    /// Adds an object to the specified tile position.
    /// </summary>
    /// <param name="x">The X coordinate of the tile.</param>
    /// <param name="y">The Y coordinate of the tile.</param>
    /// <param name="obj">The object to add.</param>
    /// <returns>The existing object in the tile, or null if the tile was empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are outside grid bounds.</exception>
    public object Add(int x, int y, object obj) {
      ValidateCoordinates(x, y);
      
      _tiles.TryGetValue(new Vector2Int(x, y), out object existingObject);
      _tiles[new Vector2Int(x, y)] = obj;
      
      return existingObject;
    }

    /// <summary>
    /// Gets the object at the specified tile position.
    /// </summary>
    /// <param name="x">The X coordinate of the tile.</param>
    /// <param name="y">The Y coordinate of the tile.</param>
    /// <returns>The object at the tile position, or null if the tile is empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are outside grid bounds.</exception>
    public object Get(int x, int y) {
      ValidateCoordinates(x, y);
      
      _tiles.TryGetValue(new Vector2Int(x, y), out object obj);
      return obj;
    }

    /// <summary>
    /// Attempts to get the object at the specified tile position without allocating memory.
    /// </summary>
    /// <param name="x">The X coordinate of the tile.</param>
    /// <param name="y">The Y coordinate of the tile.</param>
    /// <param name="obj">When this method returns, contains the object at the tile position if found; otherwise, null.</param>
    /// <returns>True if an object exists at the tile position; otherwise, false.</returns>
    public bool TryGet(int x, int y, out object obj) {
      obj = null;
      
      if (!IsValidCoordinate(x, y)) {
        return false;
      }
      
      return _tiles.TryGetValue(new Vector2Int(x, y), out obj) && obj != null;
    }

    /// <summary>
    /// Gets the object at the specified tile position as the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast the object to.</typeparam>
    /// <param name="x">The X coordinate of the tile.</param>
    /// <param name="y">The Y coordinate of the tile.</param>
    /// <returns>The object at the tile position cast to type T, or default(T) if the tile is empty or the object cannot be cast.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are outside grid bounds.</exception>
    public T GetAs<T>(int x, int y) where T : class {
      object obj = Get(x, y);
      return obj as T;
    }

    /// <summary>
    /// Attempts to get the object at the specified tile position as the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast the object to.</typeparam>
    /// <param name="x">The X coordinate of the tile.</param>
    /// <param name="y">The Y coordinate of the tile.</param>
    /// <param name="obj">When this method returns, contains the object at the tile position cast to type T if found and castable; otherwise, default(T).</param>
    /// <returns>True if an object exists at the tile position and can be cast to type T; otherwise, false.</returns>
    public bool TryGetAs<T>(int x, int y, out T obj) where T : class {
      obj = default(T);
      
      if (!TryGet(x, y, out object rawObj)) {
        return false;
      }
      
      obj = rawObj as T;
      return obj != null;
    }

    /// <summary>
    /// Removes the object at the specified tile position.
    /// </summary>
    /// <param name="x">The X coordinate of the tile.</param>
    /// <param name="y">The Y coordinate of the tile.</param>
    /// <returns>The object that was removed, or null if the tile was empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are outside grid bounds.</exception>
    public object Remove(int x, int y) {
      ValidateCoordinates(x, y);
      
      Vector2Int key = new Vector2Int(x, y);
      _tiles.TryGetValue(key, out object obj);
      _tiles.Remove(key);
      
      return obj;
    }

    /// <summary>
    /// Attempts to remove the object at the specified tile position without scanning the grid.
    /// </summary>
    /// <param name="x">The X coordinate of the tile.</param>
    /// <param name="y">The Y coordinate of the tile.</param>
    /// <param name="obj">When this method returns, contains the removed object if found; otherwise, null.</param>
    /// <returns>True if an object was removed; otherwise, false.</returns>
    public bool TryRemove(int x, int y, out object obj) {
      obj = null;
      
      if (!IsValidCoordinate(x, y)) {
        return false;
      }
      
      Vector2Int key = new Vector2Int(x, y);
      if (!_tiles.TryGetValue(key, out obj) || obj == null) {
        return false;
      }
      
      _tiles.Remove(key);
      return true;
    }

    /// <summary>
    /// Checks if a specific tile is empty.
    /// </summary>
    /// <param name="x">The X coordinate of the tile.</param>
    /// <param name="y">The Y coordinate of the tile.</param>
    /// <returns>True if the tile is empty; otherwise, false.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are outside grid bounds.</exception>
    public bool IsEmpty(int x, int y) {
      ValidateCoordinates(x, y);
      
      Vector2Int key = new Vector2Int(x, y);
      return !_tiles.TryGetValue(key, out object obj) || obj == null;
    }

    /// <summary>
    /// Checks if the entire grid is empty.
    /// </summary>
    /// <returns>True if the grid has no objects; otherwise, false.</returns>
    public bool IsEmpty() {
      foreach (var kvp in _tiles) {
        if (kvp.Value != null) {
          return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Gets a Tile object representing the tile at the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate of the tile.</param>
    /// <param name="y">The Y coordinate of the tile.</param>
    /// <returns>A read-only Tile object containing tile information.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are outside grid bounds.</exception>
    public Tile GetTile(int x, int y) {
      ValidateCoordinates(x, y);
      return new Tile(this, x, y);
    }

    /// <summary>
    /// Validates that the coordinates are within grid bounds.
    /// </summary>
    private void ValidateCoordinates(int x, int y) {
      if (!IsValidCoordinate(x, y)) {
        throw new ArgumentOutOfRangeException(
          $"Coordinates ({x}, {y}) are outside grid bounds (0-{_columns - 1}, 0-{_rows - 1})");
      }
    }

    /// <summary>
    /// Checks if the coordinates are within grid bounds.
    /// </summary>
    private bool IsValidCoordinate(int x, int y) {
      return x >= 0 && x < _columns && y >= 0 && y < _rows;
    }

    /// <summary>
    /// Represents a read-only tile in the grid with position and object information.
    /// </summary>
    public readonly struct Tile {
      private readonly AmebaGrid _grid;
      private readonly int _x;
      private readonly int _y;

      internal Tile(AmebaGrid grid, int x, int y) {
        _grid = grid;
        _x = x;
        _y = y;
      }

      /// <summary>
      /// Gets the grid coordinates of this tile.
      /// </summary>
      public Vector2Int Coordinates => new Vector2Int(_x, _y);

      /// <summary>
      /// Gets the world position of the tile's bottom-left corner.
      /// </summary>
      public Vector2 BottomLeft => new Vector2(_x * _grid._tileSize, _y * _grid._tileSize);

      /// <summary>
      /// Gets the world position of the tile's bottom-right corner.
      /// </summary>
      public Vector2 BottomRight => new Vector2((_x + 1) * _grid._tileSize, _y * _grid._tileSize);

      /// <summary>
      /// Gets the world position of the tile's top-left corner.
      /// </summary>
      public Vector2 TopLeft => new Vector2(_x * _grid._tileSize, (_y + 1) * _grid._tileSize);

      /// <summary>
      /// Gets the world position of the tile's top-right corner.
      /// </summary>
      public Vector2 TopRight => new Vector2((_x + 1) * _grid._tileSize, (_y + 1) * _grid._tileSize);

      /// <summary>
      /// Gets the world position of the tile's center.
      /// </summary>
      public Vector2 Center => new Vector2((_x + 0.5f) * _grid._tileSize, (_y + 0.5f) * _grid._tileSize);

      /// <summary>
      /// Gets the object stored in this tile.
      /// </summary>
      /// <returns>The object in the tile, or null if empty.</returns>
      public object GetObject() {
        return _grid.Get(_x, _y);
      }

      /// <summary>
      /// Gets the object stored in this tile as the specified type.
      /// </summary>
      /// <typeparam name="T">The type to cast the object to.</typeparam>
      /// <returns>The object in the tile cast to type T, or default(T) if empty or not castable.</returns>
      public T GetObjectAs<T>() where T : class {
        return _grid.GetAs<T>(_x, _y);
      }
    }
  }
}
