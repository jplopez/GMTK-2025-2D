using NUnit.Framework;
using UnityEngine;
using Ameba;
using System;

namespace Ameba.Tests {

  /// <summary>
  /// Unit tests for AmebaGrid class testing all public methods.
  /// </summary>
  [TestFixture]
  public class AmebaGridTests {

    private AmebaGrid _grid;
    private const int TEST_ROWS = 10;
    private const int TEST_COLUMNS = 10;
    private const float TEST_TILE_SIZE = 1.0f;

    [SetUp]
    public void SetUp() {
      _grid = new AmebaGrid(TEST_ROWS, TEST_COLUMNS, TEST_TILE_SIZE);
    }

    [TearDown]
    public void TearDown() {
      _grid = null;
    }

    #region Constructor Tests

    [Test]
    public void Constructor_ValidParameters_CreatesGrid() {
      var grid = new AmebaGrid(5, 5, 1.0f);
      Assert.That(grid.Rows, Is.EqualTo(5));
      Assert.That(grid.Columns, Is.EqualTo(5));
      Assert.That(grid.TileSize, Is.EqualTo(1.0f));
    }

    [Test]
    public void Constructor_MaxSize_CreatesGrid() {
      var grid = new AmebaGrid(1000, 1000, 1.0f);
      Assert.That(grid.Rows, Is.EqualTo(1000));
      Assert.That(grid.Columns, Is.EqualTo(1000));
    }

    [Test]
    public void Constructor_ZeroRows_ThrowsException() {
      Assert.Throws<ArgumentException>(() => new AmebaGrid(0, 10, 1.0f));
    }

    [Test]
    public void Constructor_ZeroColumns_ThrowsException() {
      Assert.Throws<ArgumentException>(() => new AmebaGrid(10, 0, 1.0f));
    }

    [Test]
    public void Constructor_NegativeRows_ThrowsException() {
      Assert.Throws<ArgumentException>(() => new AmebaGrid(-1, 10, 1.0f));
    }

    [Test]
    public void Constructor_NegativeColumns_ThrowsException() {
      Assert.Throws<ArgumentException>(() => new AmebaGrid(10, -1, 1.0f));
    }

    [Test]
    public void Constructor_RowsExceedMax_ThrowsException() {
      Assert.Throws<ArgumentException>(() => new AmebaGrid(1001, 10, 1.0f));
    }

    [Test]
    public void Constructor_ColumnsExceedMax_ThrowsException() {
      Assert.Throws<ArgumentException>(() => new AmebaGrid(10, 1001, 1.0f));
    }

    [Test]
    public void Constructor_ZeroTileSize_ThrowsException() {
      Assert.Throws<ArgumentException>(() => new AmebaGrid(10, 10, 0f));
    }

    [Test]
    public void Constructor_NegativeTileSize_ThrowsException() {
      Assert.Throws<ArgumentException>(() => new AmebaGrid(10, 10, -1.0f));
    }

    #endregion

    #region Add Tests

    [Test]
    public void Add_ValidCoordinates_AddsObject() {
      var obj = new object();
      var result = _grid.Add(0, 0, obj);
      
      Assert.That(result, Is.Null);
      Assert.That(_grid.Get(0, 0), Is.EqualTo(obj));
    }

    [Test]
    public void Add_ReplaceExisting_ReturnsOldObject() {
      var obj1 = new object();
      var obj2 = new object();
      
      _grid.Add(0, 0, obj1);
      var result = _grid.Add(0, 0, obj2);
      
      Assert.That(result, Is.EqualTo(obj1));
      Assert.That(_grid.Get(0, 0), Is.EqualTo(obj2));
    }

    [Test]
    public void Add_OutOfBounds_ThrowsException() {
      var obj = new object();
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Add(-1, 0, obj));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Add(0, -1, obj));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Add(TEST_COLUMNS, 0, obj));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Add(0, TEST_ROWS, obj));
    }

    [Test]
    public void Add_NullObject_AllowsNull() {
      var result = _grid.Add(0, 0, null);
      Assert.That(result, Is.Null);
    }

    #endregion

    #region Get Tests

    [Test]
    public void Get_ExistingObject_ReturnsObject() {
      var obj = new object();
      _grid.Add(5, 5, obj);
      
      var result = _grid.Get(5, 5);
      Assert.That(result, Is.EqualTo(obj));
    }

    [Test]
    public void Get_EmptyTile_ReturnsNull() {
      var result = _grid.Get(5, 5);
      Assert.That(result, Is.Null);
    }

    [Test]
    public void Get_OutOfBounds_ThrowsException() {
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Get(-1, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Get(0, -1));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Get(TEST_COLUMNS, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Get(0, TEST_ROWS));
    }

    #endregion

    #region TryGet Tests

    [Test]
    public void TryGet_ExistingObject_ReturnsTrue() {
      var obj = new object();
      _grid.Add(3, 3, obj);
      
      bool result = _grid.TryGet(3, 3, out object retrievedObj);
      
      Assert.That(result, Is.True);
      Assert.That(retrievedObj, Is.EqualTo(obj));
    }

    [Test]
    public void TryGet_EmptyTile_ReturnsFalse() {
      bool result = _grid.TryGet(3, 3, out object obj);
      
      Assert.That(result, Is.False);
      Assert.That(obj, Is.Null);
    }

    [Test]
    public void TryGet_OutOfBounds_ReturnsFalse() {
      bool result1 = _grid.TryGet(-1, 0, out object obj1);
      bool result2 = _grid.TryGet(TEST_COLUMNS, 0, out object obj2);
      
      Assert.That(result1, Is.False);
      Assert.That(result2, Is.False);
      Assert.That(obj1, Is.Null);
      Assert.That(obj2, Is.Null);
    }

    [Test]
    public void TryGet_NullObject_ReturnsFalse() {
      _grid.Add(0, 0, null);
      bool result = _grid.TryGet(0, 0, out object obj);
      
      Assert.That(result, Is.False);
      Assert.That(obj, Is.Null);
    }

    #endregion

    #region GetAs Tests

    [Test]
    public void GetAs_CorrectType_ReturnsTypedObject() {
      var testString = "test";
      _grid.Add(2, 2, testString);
      
      var result = _grid.GetAs<string>(2, 2);
      
      Assert.That(result, Is.EqualTo(testString));
    }

    [Test]
    public void GetAs_WrongType_ReturnsNull() {
      var testString = "test";
      _grid.Add(2, 2, testString);
      
      var result = _grid.GetAs<GameObject>(2, 2);
      
      Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAs_EmptyTile_ReturnsNull() {
      var result = _grid.GetAs<string>(2, 2);
      Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAs_OutOfBounds_ThrowsException() {
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.GetAs<string>(-1, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.GetAs<string>(TEST_COLUMNS, 0));
    }

    #endregion

    #region TryGetAs Tests

    [Test]
    public void TryGetAs_CorrectType_ReturnsTrue() {
      var testString = "test";
      _grid.Add(4, 4, testString);
      
      bool result = _grid.TryGetAs<string>(4, 4, out string obj);
      
      Assert.That(result, Is.True);
      Assert.That(obj, Is.EqualTo(testString));
    }

    [Test]
    public void TryGetAs_WrongType_ReturnsFalse() {
      var testString = "test";
      _grid.Add(4, 4, testString);
      
      bool result = _grid.TryGetAs<GameObject>(4, 4, out GameObject obj);
      
      Assert.That(result, Is.False);
      Assert.That(obj, Is.Null);
    }

    [Test]
    public void TryGetAs_EmptyTile_ReturnsFalse() {
      bool result = _grid.TryGetAs<string>(4, 4, out string obj);
      
      Assert.That(result, Is.False);
      Assert.That(obj, Is.Null);
    }

    [Test]
    public void TryGetAs_OutOfBounds_ReturnsFalse() {
      bool result = _grid.TryGetAs<string>(-1, 0, out string obj);
      
      Assert.That(result, Is.False);
      Assert.That(obj, Is.Null);
    }

    #endregion

    #region Remove Tests

    [Test]
    public void Remove_ExistingObject_ReturnsObject() {
      var obj = new object();
      _grid.Add(6, 6, obj);
      
      var result = _grid.Remove(6, 6);
      
      Assert.That(result, Is.EqualTo(obj));
      Assert.That(_grid.Get(6, 6), Is.Null);
    }

    [Test]
    public void Remove_EmptyTile_ReturnsNull() {
      var result = _grid.Remove(6, 6);
      Assert.That(result, Is.Null);
    }

    [Test]
    public void Remove_OutOfBounds_ThrowsException() {
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Remove(-1, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Remove(0, -1));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Remove(TEST_COLUMNS, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.Remove(0, TEST_ROWS));
    }

    #endregion

    #region TryRemove Tests

    [Test]
    public void TryRemove_ExistingObject_ReturnsTrue() {
      var obj = new object();
      _grid.Add(7, 7, obj);
      
      bool result = _grid.TryRemove(7, 7, out object removedObj);
      
      Assert.That(result, Is.True);
      Assert.That(removedObj, Is.EqualTo(obj));
      Assert.That(_grid.Get(7, 7), Is.Null);
    }

    [Test]
    public void TryRemove_EmptyTile_ReturnsFalse() {
      bool result = _grid.TryRemove(7, 7, out object obj);
      
      Assert.That(result, Is.False);
      Assert.That(obj, Is.Null);
    }

    [Test]
    public void TryRemove_OutOfBounds_ReturnsFalse() {
      bool result1 = _grid.TryRemove(-1, 0, out object obj1);
      bool result2 = _grid.TryRemove(TEST_COLUMNS, 0, out object obj2);
      
      Assert.That(result1, Is.False);
      Assert.That(result2, Is.False);
      Assert.That(obj1, Is.Null);
      Assert.That(obj2, Is.Null);
    }

    [Test]
    public void TryRemove_NullObject_ReturnsFalse() {
      _grid.Add(0, 0, null);
      bool result = _grid.TryRemove(0, 0, out object obj);
      
      Assert.That(result, Is.False);
      Assert.That(obj, Is.Null);
    }

    #endregion

    #region IsEmpty Tests

    [Test]
    public void IsEmpty_Tile_EmptyTile_ReturnsTrue() {
      bool result = _grid.IsEmpty(8, 8);
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsEmpty_Tile_OccupiedTile_ReturnsFalse() {
      _grid.Add(8, 8, new object());
      bool result = _grid.IsEmpty(8, 8);
      Assert.That(result, Is.False);
    }

    [Test]
    public void IsEmpty_Tile_NullObject_ReturnsTrue() {
      _grid.Add(8, 8, null);
      bool result = _grid.IsEmpty(8, 8);
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsEmpty_Tile_OutOfBounds_ThrowsException() {
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.IsEmpty(-1, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.IsEmpty(TEST_COLUMNS, 0));
    }

    [Test]
    public void IsEmpty_Grid_EmptyGrid_ReturnsTrue() {
      bool result = _grid.IsEmpty();
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsEmpty_Grid_WithObjects_ReturnsFalse() {
      _grid.Add(0, 0, new object());
      bool result = _grid.IsEmpty();
      Assert.That(result, Is.False);
    }

    [Test]
    public void IsEmpty_Grid_AllNullObjects_ReturnsTrue() {
      _grid.Add(0, 0, null);
      _grid.Add(1, 1, null);
      bool result = _grid.IsEmpty();
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsEmpty_Grid_AfterRemoval_ReturnsTrue() {
      _grid.Add(0, 0, new object());
      _grid.Remove(0, 0);
      bool result = _grid.IsEmpty();
      Assert.That(result, Is.True);
    }

    #endregion

    #region GetTile Tests

    [Test]
    public void GetTile_ValidCoordinates_ReturnsTile() {
      var tile = _grid.GetTile(5, 5);
      Assert.That(tile.Coordinates, Is.EqualTo(new Vector2Int(5, 5)));
    }

    [Test]
    public void GetTile_OutOfBounds_ThrowsException() {
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.GetTile(-1, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => _grid.GetTile(TEST_COLUMNS, 0));
    }

    [Test]
    public void Tile_Coordinates_ReturnsCorrectValue() {
      var tile = _grid.GetTile(3, 4);
      Assert.That(tile.Coordinates, Is.EqualTo(new Vector2Int(3, 4)));
    }

    [Test]
    public void Tile_BottomLeft_ReturnsCorrectPosition() {
      var tile = _grid.GetTile(2, 3);
      Assert.That(tile.BottomLeft, Is.EqualTo(new Vector2(2.0f, 3.0f)));
    }

    [Test]
    public void Tile_BottomRight_ReturnsCorrectPosition() {
      var tile = _grid.GetTile(2, 3);
      Assert.That(tile.BottomRight, Is.EqualTo(new Vector2(3.0f, 3.0f)));
    }

    [Test]
    public void Tile_TopLeft_ReturnsCorrectPosition() {
      var tile = _grid.GetTile(2, 3);
      Assert.That(tile.TopLeft, Is.EqualTo(new Vector2(2.0f, 4.0f)));
    }

    [Test]
    public void Tile_TopRight_ReturnsCorrectPosition() {
      var tile = _grid.GetTile(2, 3);
      Assert.That(tile.TopRight, Is.EqualTo(new Vector2(3.0f, 4.0f)));
    }

    [Test]
    public void Tile_Center_ReturnsCorrectPosition() {
      var tile = _grid.GetTile(2, 3);
      Assert.That(tile.Center, Is.EqualTo(new Vector2(2.5f, 3.5f)));
    }

    [Test]
    public void Tile_GetObject_ReturnsCorrectObject() {
      var obj = new object();
      _grid.Add(1, 1, obj);
      
      var tile = _grid.GetTile(1, 1);
      Assert.That(tile.GetObject(), Is.EqualTo(obj));
    }

    [Test]
    public void Tile_GetObject_EmptyTile_ReturnsNull() {
      var tile = _grid.GetTile(1, 1);
      Assert.That(tile.GetObject(), Is.Null);
    }

    [Test]
    public void Tile_GetObjectAs_CorrectType_ReturnsTypedObject() {
      var testString = "test";
      _grid.Add(1, 1, testString);
      
      var tile = _grid.GetTile(1, 1);
      Assert.That(tile.GetObjectAs<string>(), Is.EqualTo(testString));
    }

    [Test]
    public void Tile_GetObjectAs_WrongType_ReturnsNull() {
      var testString = "test";
      _grid.Add(1, 1, testString);
      
      var tile = _grid.GetTile(1, 1);
      Assert.That(tile.GetObjectAs<GameObject>(), Is.Null);
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public void MultipleOperations_SameCell_WorksCorrectly() {
      var obj1 = new object();
      var obj2 = new object();
      
      _grid.Add(0, 0, obj1);
      Assert.That(_grid.Get(0, 0), Is.EqualTo(obj1));
      
      _grid.Add(0, 0, obj2);
      Assert.That(_grid.Get(0, 0), Is.EqualTo(obj2));
      
      var removed = _grid.Remove(0, 0);
      Assert.That(removed, Is.EqualTo(obj2));
      Assert.That(_grid.IsEmpty(0, 0), Is.True);
    }

    [Test]
    public void DifferentCells_IndependentStorage() {
      var obj1 = new object();
      var obj2 = new object();
      
      _grid.Add(0, 0, obj1);
      _grid.Add(1, 1, obj2);
      
      Assert.That(_grid.Get(0, 0), Is.EqualTo(obj1));
      Assert.That(_grid.Get(1, 1), Is.EqualTo(obj2));
      
      _grid.Remove(0, 0);
      Assert.That(_grid.Get(1, 1), Is.EqualTo(obj2));
    }

    [Test]
    public void BoundaryCoordinates_WorkCorrectly() {
      var obj = new object();
      
      // Test corners
      _grid.Add(0, 0, obj);
      Assert.That(_grid.Get(0, 0), Is.EqualTo(obj));
      
      _grid.Add(TEST_COLUMNS - 1, 0, obj);
      Assert.That(_grid.Get(TEST_COLUMNS - 1, 0), Is.EqualTo(obj));
      
      _grid.Add(0, TEST_ROWS - 1, obj);
      Assert.That(_grid.Get(0, TEST_ROWS - 1), Is.EqualTo(obj));
      
      _grid.Add(TEST_COLUMNS - 1, TEST_ROWS - 1, obj);
      Assert.That(_grid.Get(TEST_COLUMNS - 1, TEST_ROWS - 1), Is.EqualTo(obj));
    }

    #endregion
  }
}
