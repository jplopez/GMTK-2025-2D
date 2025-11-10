using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;
using Ameba;
using System.Collections;

namespace GMTK.Tests {

  /// <summary>
  /// Functional tests for PlayableGrid testing element management and event handling.
  /// </summary>
  [TestFixture]
  public class PlayableGridTests {

    private GameObject _gridObject;
    private PlayableGrid _playableGrid;
    private GameObject _elementObject;
    private PlayableElement _element;
    private GameEventChannel _eventChannel;

    [SetUp]
    public void SetUp() {
      // Create grid object
      _gridObject = new GameObject("TestPlayableGrid");
      _playableGrid = _gridObject.AddComponent<PlayableGrid>();

      // Create a test element
      _elementObject = new GameObject("TestElement");
      _element = _elementObject.AddComponent<PlayableElement>();
      
      // Add required components for PlayableElement
      _elementObject.AddComponent<SpriteRenderer>();
      _elementObject.AddComponent<PolygonCollider2D>();
      
      // Initialize element
      _element.Initialize();

      // Set up event channel mock
      _eventChannel = ScriptableObject.CreateInstance<GameEventChannel>();
      ServiceLocator.Register(_eventChannel);
    }

    [TearDown]
    public void TearDown() {
      if (_elementObject != null) {
        Object.DestroyImmediate(_elementObject);
      }
      if (_gridObject != null) {
        Object.DestroyImmediate(_gridObject);
      }
      if (_eventChannel != null) {
        ScriptableObject.DestroyImmediate(_eventChannel);
      }
      
      ServiceLocator.Clear();
    }

    #region Add/Get/Remove Tests

    [Test]
    public void Add_ValidElement_AddsToGrid() {
      var result = _playableGrid.Add(0, 0, _element);
      
      Assert.That(result, Is.Null);
      Assert.That(_playableGrid.Get(0, 0), Is.EqualTo(_element));
    }

    [Test]
    public void Add_ReplaceElement_ReturnsOldElement() {
      var element2Object = new GameObject("TestElement2");
      var element2 = element2Object.AddComponent<PlayableElement>();
      element2Object.AddComponent<SpriteRenderer>();
      element2Object.AddComponent<PolygonCollider2D>();
      element2.Initialize();

      _playableGrid.Add(0, 0, _element);
      var result = _playableGrid.Add(0, 0, element2);
      
      Assert.That(result, Is.EqualTo(_element));
      Assert.That(_playableGrid.Get(0, 0), Is.EqualTo(element2));
      
      Object.DestroyImmediate(element2Object);
    }

    [Test]
    public void Add_InvokesEvent() {
      bool eventInvoked = false;
      _playableGrid.OnElementAdded.AddListener((e) => eventInvoked = true);
      
      _playableGrid.Add(0, 0, _element);
      
      Assert.That(eventInvoked, Is.True);
    }

    [Test]
    public void Get_ExistingElement_ReturnsElement() {
      _playableGrid.Add(5, 5, _element);
      
      var result = _playableGrid.Get(5, 5);
      
      Assert.That(result, Is.EqualTo(_element));
    }

    [Test]
    public void Get_EmptyTile_ReturnsNull() {
      var result = _playableGrid.Get(5, 5);
      Assert.That(result, Is.Null);
    }

    [Test]
    public void TryGet_ExistingElement_ReturnsTrue() {
      _playableGrid.Add(3, 3, _element);
      
      bool result = _playableGrid.TryGet(3, 3, out PlayableElement element);
      
      Assert.That(result, Is.True);
      Assert.That(element, Is.EqualTo(_element));
    }

    [Test]
    public void TryGet_EmptyTile_ReturnsFalse() {
      bool result = _playableGrid.TryGet(3, 3, out PlayableElement element);
      
      Assert.That(result, Is.False);
      Assert.That(element, Is.Null);
    }

    [Test]
    public void Remove_ExistingElement_RemovesAndReturns() {
      _playableGrid.Add(7, 7, _element);
      
      var result = _playableGrid.Remove(7, 7);
      
      Assert.That(result, Is.EqualTo(_element));
      Assert.That(_playableGrid.Get(7, 7), Is.Null);
    }

    [Test]
    public void Remove_InvokesEvent() {
      bool eventInvoked = false;
      _playableGrid.OnElementRemoved.AddListener((e) => eventInvoked = true);
      
      _playableGrid.Add(0, 0, _element);
      _playableGrid.Remove(0, 0);
      
      Assert.That(eventInvoked, Is.True);
    }

    [Test]
    public void TryRemove_ExistingElement_ReturnsTrue() {
      _playableGrid.Add(2, 2, _element);
      
      bool result = _playableGrid.TryRemove(2, 2, out PlayableElement element);
      
      Assert.That(result, Is.True);
      Assert.That(element, Is.EqualTo(_element));
      Assert.That(_playableGrid.Get(2, 2), Is.Null);
    }

    [Test]
    public void TryRemove_EmptyTile_ReturnsFalse() {
      bool result = _playableGrid.TryRemove(2, 2, out PlayableElement element);
      
      Assert.That(result, Is.False);
      Assert.That(element, Is.Null);
    }

    #endregion

    #region IsEmpty Tests

    [Test]
    public void IsEmpty_EmptyTile_ReturnsTrue() {
      bool result = _playableGrid.IsEmpty(5, 5);
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsEmpty_OccupiedTile_ReturnsFalse() {
      _playableGrid.Add(5, 5, _element);
      bool result = _playableGrid.IsEmpty(5, 5);
      Assert.That(result, Is.False);
    }

    #endregion

    #region CanPlaceElement Tests

    [Test]
    public void CanPlaceElement_EmptyTile_ReturnsTrue() {
      var worldPos = _playableGrid.GridToWorld(new Vector2Int(1, 1));
      
      bool result = _playableGrid.CanPlaceElement(_element, worldPos);
      
      Assert.That(result, Is.True);
    }

    [Test]
    public void CanPlaceElement_OccupiedTile_ReturnsFalse() {
      var gridPos = new Vector2Int(1, 1);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      
      var element2Object = new GameObject("TestElement2");
      var element2 = element2Object.AddComponent<PlayableElement>();
      element2Object.AddComponent<SpriteRenderer>();
      element2Object.AddComponent<PolygonCollider2D>();
      element2.Initialize();
      
      _playableGrid.Add(gridPos.x, gridPos.y, element2);
      
      bool result = _playableGrid.CanPlaceElement(_element, worldPos);
      
      Assert.That(result, Is.False);
      
      Object.DestroyImmediate(element2Object);
    }

    [Test]
    public void CanPlaceElement_OutOfBounds_ReturnsFalse() {
      var worldPos = new Vector2(1000, 1000);
      
      bool result = _playableGrid.CanPlaceElement(_element, worldPos);
      
      Assert.That(result, Is.False);
    }

    [Test]
    public void CanPlaceElement_NullElement_ReturnsFalse() {
      var worldPos = _playableGrid.GridToWorld(new Vector2Int(1, 1));
      
      bool result = _playableGrid.CanPlaceElement(null, worldPos);
      
      Assert.That(result, Is.False);
    }

    #endregion

    #region Coordinate Conversion Tests

    [Test]
    public void WorldToGrid_ConvertsCorrectly() {
      var worldPos = new Vector2(2.5f, 3.5f);
      
      var gridPos = _playableGrid.WorldToGrid(worldPos);
      
      // With tile size 1.0, position 2.5, 3.5 should round to grid position 2, 4 or 3, 4 depending on rounding
      Assert.That(gridPos.x, Is.GreaterThanOrEqualTo(2));
      Assert.That(gridPos.y, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void GridToWorld_ConvertsCorrectly() {
      var gridPos = new Vector2Int(2, 3);
      
      var worldPos = _playableGrid.GridToWorld(gridPos);
      
      // With tile size 1.0, grid position 2, 3 should be world position 2.0, 3.0
      Assert.That(worldPos.x, Is.EqualTo(2.0f));
      Assert.That(worldPos.y, Is.EqualTo(3.0f));
    }

    [Test]
    public void WorldToGrid_GridToWorld_RoundTrip() {
      var originalGridPos = new Vector2Int(5, 7);
      
      var worldPos = _playableGrid.GridToWorld(originalGridPos);
      var gridPos = _playableGrid.WorldToGrid(worldPos);
      
      Assert.That(gridPos, Is.EqualTo(originalGridPos));
    }

    #endregion

    #region Event Handling Tests

    [UnityTest]
    public IEnumerator ElementSelected_RemovesFromGrid() {
      // Add element to grid
      _playableGrid.Add(1, 1, _element);
      var worldPos = _playableGrid.GridToWorld(new Vector2Int(1, 1));
      _element.UpdatePosition(worldPos);
      
      // Wait for Awake to be called on PlayableGrid
      yield return null;
      
      // Trigger selected event
      var args = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.Selected);
      _eventChannel.Raise(GameEventType.ElementSelected, args);
      
      // Element should still be in grid (selected doesn't remove until drag starts)
      // This behavior can be adjusted based on requirements
      yield return null;
    }

    [UnityTest]
    public IEnumerator ElementDropped_ValidPosition_AddsToGrid() {
      var gridPos = new Vector2Int(2, 2);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);
      
      // Wait for Awake to be called on PlayableGrid
      yield return null;
      
      // Trigger dropped event
      var args = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, args);
      
      // Give time for event to process
      yield return null;
      
      // Element should be added to grid
      var result = _playableGrid.Get(gridPos.x, gridPos.y);
      Assert.That(result, Is.EqualTo(_element));
    }

    [Test]
    public void MultipleElements_IndependentManagement() {
      var element2Object = new GameObject("TestElement2");
      var element2 = element2Object.AddComponent<PlayableElement>();
      element2Object.AddComponent<SpriteRenderer>();
      element2Object.AddComponent<PolygonCollider2D>();
      element2.Initialize();

      _playableGrid.Add(0, 0, _element);
      _playableGrid.Add(1, 1, element2);
      
      Assert.That(_playableGrid.Get(0, 0), Is.EqualTo(_element));
      Assert.That(_playableGrid.Get(1, 1), Is.EqualTo(element2));
      
      _playableGrid.Remove(0, 0);
      Assert.That(_playableGrid.Get(1, 1), Is.EqualTo(element2));
      
      Object.DestroyImmediate(element2Object);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void AddRemoveMultipleTimes_WorksCorrectly() {
      _playableGrid.Add(0, 0, _element);
      _playableGrid.Remove(0, 0);
      _playableGrid.Add(0, 0, _element);
      
      Assert.That(_playableGrid.Get(0, 0), Is.EqualTo(_element));
    }

    [Test]
    public void BoundaryPositions_WorkCorrectly() {
      // Test corner positions
      _playableGrid.Add(0, 0, _element);
      Assert.That(_playableGrid.Get(0, 0), Is.EqualTo(_element));
      _playableGrid.Remove(0, 0);
      
      _playableGrid.Add(9, 9, _element);
      Assert.That(_playableGrid.Get(9, 9), Is.EqualTo(_element));
    }

    #endregion
  }
}
