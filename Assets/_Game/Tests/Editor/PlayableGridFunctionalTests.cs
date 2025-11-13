using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using Ameba;
using System.Collections;

namespace GMTK.Tests {

  /// <summary>
  /// Functional tests for PlayableGrid testing element management and event handling.
  /// All tests use UnityTest to properly initialize MonoBehaviour context and synthesize events.
  /// </summary>
  [TestFixture]
  public class PlayableGridFunctionalTests {

    private GameObject _gridObject;
    private PlayableGrid _playableGrid;
    private GameObject _elementObject;
    private PlayableElement _element;
    private GameEventChannel _eventChannel;

    private GMTKBootstrap _bootstrap;

    [SetUp]
    public void SetUp() {
      // Note: Setup runs before each test
    }

    [TearDown]
    public void TearDown() {
      if (_elementObject != null) {
        Object.DestroyImmediate(_elementObject);
      }
      if (_gridObject != null) {
        Object.DestroyImmediate(_gridObject);
      }

      ServiceLocator.Clear();
    }

    /// <summary>
    /// Helper coroutine to initialize test environment with proper MonoBehaviour context
    /// </summary>
    private IEnumerator InitializeTestEnvironment() {
      // Initialize bootstrap and services
      GameObject bootStrapObj = new("GMTKBootstrap", typeof(GMTKBootstrap));
      _bootstrap = bootStrapObj.GetComponent<GMTKBootstrap>();
      _bootstrap.ForceReinitialize = true;
      _bootstrap.Invoke("Awake", 0f);

      yield return null;

      _eventChannel = ServiceLocator.Get<GameEventChannel>();
      Assert.IsNotNull(_eventChannel, "GameEventChannel should be registered in ServiceLocator");

      // Create grid object
      _gridObject = new GameObject("TestPlayableGrid", typeof(PlayableGrid));
      _playableGrid = _gridObject.GetComponent<PlayableGrid>();
      _playableGrid.Invoke("Awake", 0f);

      _elementObject = TestPrefabInstance();
      _element = _elementObject.GetComponent<PlayableElement>();
      _element.Invoke("Awake", 0f);

      yield return null;
    }

    // Create a test playable element using prefab Assets/_Game/Prefabs/PlayableObjects/Platforms/Platform_YengaThin.prefab
    private GameObject TestPrefabInstance(string prefabPath = "Assets/_Game/Prefabs/PlayableObjects/Platforms/Platform_YengaThin.prefab", string name = "TestElement") {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      GameObject goInstance = Object.Instantiate(prefab);
      goInstance.name = name;
      return goInstance;
    }

    #region Add/Get/Remove Tests

    [UnityTest]
    public IEnumerator Add_ValidElement_AddsToGrid() {
      yield return InitializeTestEnvironment();

      var gridPos = new Vector2Int(0, 0);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);

      // Synthesize drop event to add element
      var dropArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      // Assert element was added to grid
      AssertElementAtPosition(0, 0, _element);
      AssertGridIsOccupied();
    }

    [UnityTest]
    public IEnumerator Add_ReplaceElement_ReturnsOldElement() {
      yield return InitializeTestEnvironment();
      // AllowReplaceItems = true is needed to expect the grid to replace elements
      _playableGrid.AllowReplaceItems = true;

      var element2Object = TestPrefabInstance(name: "TestElement2");
      var element2 = element2Object.GetComponent<PlayableElement>();
      element2.Invoke("Awake", 0f);

      yield return null;

      var gridPos = new Vector2Int(0, 0);
      var worldPos = _playableGrid.GridToWorld(gridPos);

      // Add first element
      _element.UpdatePosition(worldPos);
      var dropArgs1 = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs1);

      yield return null;

      AssertElementAtPosition(0, 0, _element);
      Debug.Log($"TEST Added first element");
      // Add second element to same position (should replace)
      element2.UpdatePosition(worldPos);
      var dropArgs2 = new PlayableElementEventArgs(element2, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs2);

      yield return null;

      AssertElementAtPosition(0, 0, element2, "Second element should replace first");
      Debug.Log($"TEST Added second element");

      Object.DestroyImmediate(element2Object);
    }

    [UnityTest]
    public IEnumerator Add_InvokesEvent() {
      yield return InitializeTestEnvironment();

      bool eventInvoked = false;
      _playableGrid.OnElementAdded.AddListener((e) => eventInvoked = true);

      var gridPos = new Vector2Int(0, 0);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);

      var dropArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      Assert.That(eventInvoked, Is.True, "OnElementAdded event should be invoked");
    }

    [UnityTest]
    public IEnumerator Get_ExistingElement_ReturnsElement() {
      yield return InitializeTestEnvironment();

      var gridPos = new Vector2Int(5, 5);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);

      var dropArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      AssertElementAtPosition(5, 5, _element);
    }

    [UnityTest]
    public IEnumerator Get_EmptyTile_ReturnsNull() {
      yield return InitializeTestEnvironment();

      AssertTileIsEmpty(5, 5);
      yield return null;
    }

    [UnityTest]
    public IEnumerator TryGet_ExistingElement_ReturnsTrue() {
      yield return InitializeTestEnvironment();

      var gridPos = new Vector2Int(3, 3);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);

      var dropArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      bool result = _playableGrid.Grid.TryGetAs<PlayableElement>(3, 3, out PlayableElement element);

      Assert.That(result, Is.True);
      Assert.That(element, Is.EqualTo(_element));
    }

    [UnityTest]
    public IEnumerator TryGet_EmptyTile_ReturnsFalse() {
      yield return InitializeTestEnvironment();

      bool result = _playableGrid.Grid.TryGetAs<PlayableElement>(3, 3, out PlayableElement element);

      Assert.That(result, Is.False);
      Assert.That(element, Is.Null);

      yield return null;
    }

    [UnityTest]
    public IEnumerator Remove_ExistingElement_RemovesAndReturns() {
      yield return InitializeTestEnvironment();

      var gridPos = new Vector2Int(7, 7);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);

      // Add element first
      var dropArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      AssertElementAtPosition(7, 7, _element);

      // Remove element by selecting and dragging away
      var selectArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.Selected);
      _eventChannel.Raise(GameEventType.ElementSelected, selectArgs);

      yield return null;

      AssertTileIsEmpty(7, 7, "Element should be removed when selected");
    }

    [UnityTest]
    public IEnumerator Remove_InvokesEvent() {
      yield return InitializeTestEnvironment();

      bool eventInvoked = false;
      _playableGrid.OnElementRemoved.AddListener((e) => eventInvoked = true);

      var gridPos = new Vector2Int(0, 0);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);

      // Add element
      var dropArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      // Remove element by selecting it
      var selectArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.Selected);
      _eventChannel.Raise(GameEventType.ElementSelected, selectArgs);

      yield return null;

      Assert.That(eventInvoked, Is.True, "OnElementRemoved should be invoked");
    }

    [UnityTest]
    public IEnumerator TryRemove_ExistingElement_ReturnsTrue() {
      yield return InitializeTestEnvironment();

      _playableGrid.Grid.Add(2, 2, _element);

      bool result = _playableGrid.Grid.TryRemove(2, 2, out var element);

      Assert.That(result, Is.True);
      Assert.That(element, Is.EqualTo(_element));
      AssertTileIsEmpty(2, 2);

      yield return null;
    }

    [UnityTest]
    public IEnumerator TryRemove_EmptyTile_ReturnsFalse() {
      yield return InitializeTestEnvironment();

      bool result = _playableGrid.Grid.TryRemove(2, 2, out var element);

      Assert.That(result, Is.False);
      Assert.That(element, Is.Null);

      yield return null;
    }

    #endregion

    #region IsEmpty Tests

    [UnityTest]
    public IEnumerator IsEmpty_OccupiedTile_ReturnsFalse() {
      yield return InitializeTestEnvironment();

      AssertGridIsEmpty();
      AssertTileIsEmpty(5, 5);

      var gridPos = new Vector2Int(5, 5);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);

      var dropArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      AssertGridIsOccupied();
      AssertTileIsOccupied(5, 5);
    }

    #endregion

    #region CanPlaceElement Tests

    [UnityTest]
    public IEnumerator CanPlaceElement_EmptyTile_ReturnsTrue() {
      yield return InitializeTestEnvironment();

      var worldPos = _playableGrid.GridToWorld(new Vector2Int(1, 1));

      bool result = _playableGrid.CanPlaceElement(_element, worldPos);

      Assert.That(result, Is.True);

      yield return null;
    }

    [UnityTest]
    public IEnumerator CanPlaceElement_OccupiedTile_ReturnsFalse() {
      yield return InitializeTestEnvironment();

      var gridPos = new Vector2Int(1, 1);
      var worldPos = _playableGrid.GridToWorld(gridPos);

      var element2Object = new GameObject("TestElement2", typeof(PlayableElement), typeof(SpriteRenderer), typeof(PolygonCollider2D));
      var element2 = element2Object.GetComponent<PlayableElement>();
      element2.Invoke("Awake", 0f);

      yield return null;

      // Add first element
      element2.UpdatePosition(worldPos);
      var dropArgs = new PlayableElementEventArgs(element2, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      // Try to check if second element can be placed in same spot
      bool result = _playableGrid.CanPlaceElement(_element, worldPos);

      Assert.That(result, Is.False, "Should not be able to place element on occupied tile");

      Object.DestroyImmediate(element2Object);
    }

    [UnityTest]
    public IEnumerator CanPlaceElement_OutOfBounds_ReturnsFalse() {
      yield return InitializeTestEnvironment();

      var worldPos = new Vector2(1000, 1000);

      bool result = _playableGrid.CanPlaceElement(_element, worldPos);

      Assert.That(result, Is.False);

      yield return null;
    }

    [UnityTest]
    public IEnumerator CanPlaceElement_NullElement_ReturnsFalse() {
      yield return InitializeTestEnvironment();

      var worldPos = _playableGrid.GridToWorld(new Vector2Int(1, 1));

      bool result = _playableGrid.CanPlaceElement(null, worldPos);

      Assert.That(result, Is.False);

      yield return null;
    }

    #endregion

    #region Event Handling Tests

    [UnityTest]
    public IEnumerator ElementSelected_RemovesFromGrid() {
      yield return InitializeTestEnvironment();

      // Add element to grid first
      var gridPos = new Vector2Int(1, 1);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);

      var dropArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      AssertElementAtPosition(1, 1, _element, "Element should be in grid after drop");

      // Select element (should remove from grid)
      var selectArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.Selected);
      _eventChannel.Raise(GameEventType.ElementSelected, selectArgs);

      yield return null;

      AssertTileIsEmpty(1, 1, "Element should be removed from grid when selected");
    }

    [UnityTest]
    public IEnumerator ElementDropped_ValidPosition_AddsToGrid() {
      yield return InitializeTestEnvironment();

      var gridPos = new Vector2Int(2, 2);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);

      yield return null;

      // Trigger dropped event
      var args = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, args);

      yield return null;

      // Element should be added to grid
      AssertElementAtPosition(2, 2, _element);
    }

    [UnityTest]
    public IEnumerator ElementDropped_InvalidPosition_ReturnsToOriginal() {
      yield return InitializeTestEnvironment();

      var originalGridPos = new Vector2Int(1, 1);
      var originalWorldPos = _playableGrid.GridToWorld(originalGridPos);
      _element.UpdatePosition(originalWorldPos);

      // Add element to grid first
      var dropArgs1 = new PlayableElementEventArgs(_element, originalWorldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs1);

      yield return null;

      AssertElementAtPosition(1, 1, _element);

      // Select element to pick it up
      var selectArgs = new PlayableElementEventArgs(_element, originalWorldPos, PlayableElementEventType.Selected);
      _eventChannel.Raise(GameEventType.ElementSelected, selectArgs);

      yield return null;

      // Try to drop at invalid position (out of bounds)
      var invalidWorldPos = new Vector2(1000, 1000);
      _element.UpdatePosition(invalidWorldPos);

      var dropArgs2 = new PlayableElementEventArgs(_element, invalidWorldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs2);

      yield return null;

      // Element should return to original position
      AssertElementAtPosition(1, 1, _element, "Element should return to original position on invalid drop");
    }

    [UnityTest]
    public IEnumerator MultipleElements_IndependentManagement() {
      yield return InitializeTestEnvironment();

      var element2Object = new GameObject("TestElement2", typeof(PlayableElement), typeof(SpriteRenderer), typeof(PolygonCollider2D));
      var element2 = element2Object.GetComponent<PlayableElement>();
      element2.Invoke("Awake", 0f);

      yield return null;

      // Add first element at (0,0)
      var gridPos1 = new Vector2Int(0, 0);
      var worldPos1 = _playableGrid.GridToWorld(gridPos1);
      _element.UpdatePosition(worldPos1);
      var dropArgs1 = new PlayableElementEventArgs(_element, worldPos1, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs1);

      yield return null;

      // Add second element at (1,1)
      var gridPos2 = new Vector2Int(1, 1);
      var worldPos2 = _playableGrid.GridToWorld(gridPos2);
      element2.UpdatePosition(worldPos2);
      var dropArgs2 = new PlayableElementEventArgs(element2, worldPos2, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs2);

      yield return null;

      AssertElementAtPosition(0, 0, _element);
      AssertElementAtPosition(1, 1, element2);

      // Remove first element by selecting it
      var selectArgs = new PlayableElementEventArgs(_element, worldPos1, PlayableElementEventType.Selected);
      _eventChannel.Raise(GameEventType.ElementSelected, selectArgs);

      yield return null;

      // Second element should still be there
      AssertTileIsEmpty(0, 0, "First element should be removed");
      AssertElementAtPosition(1, 1, element2, "Second element should remain");

      Object.DestroyImmediate(element2Object);
    }

    #endregion

    #region Edge Cases

    [UnityTest]
    public IEnumerator AddRemoveMultipleTimes_WorksCorrectly() {
      yield return InitializeTestEnvironment();

      var gridPos = new Vector2Int(0, 0);
      var worldPos = _playableGrid.GridToWorld(gridPos);
      _element.UpdatePosition(worldPos);

      // Add
      var dropArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      AssertElementAtPosition(0, 0, _element);

      // Remove
      var selectArgs = new PlayableElementEventArgs(_element, worldPos, PlayableElementEventType.Selected);
      _eventChannel.Raise(GameEventType.ElementSelected, selectArgs);

      yield return null;

      AssertTileIsEmpty(0, 0);

      // Add again
      _element.UpdatePosition(worldPos);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs);

      yield return null;

      AssertElementAtPosition(0, 0, _element, "Element should be added back successfully");
    }

    [UnityTest]
    public IEnumerator BoundaryPositions_WorkCorrectly() {
      yield return InitializeTestEnvironment();

      // Test corner positions (0,0)
      var gridPos1 = new Vector2Int(0, 0);
      var worldPos1 = _playableGrid.GridToWorld(gridPos1);
      _element.UpdatePosition(worldPos1);

      var dropArgs1 = new PlayableElementEventArgs(_element, worldPos1, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs1);

      yield return null;

      AssertElementAtPosition(0, 0, _element);

      // Remove
      var selectArgs1 = new PlayableElementEventArgs(_element, worldPos1, PlayableElementEventType.Selected);
      _eventChannel.Raise(GameEventType.ElementSelected, selectArgs1);

      yield return null;

      // Test opposite corner (9,9)
      var gridPos2 = new Vector2Int(9, 9);
      var worldPos2 = _playableGrid.GridToWorld(gridPos2);
      _element.UpdatePosition(worldPos2);

      var dropArgs2 = new PlayableElementEventArgs(_element, worldPos2, PlayableElementEventType.DropSuccess);
      _eventChannel.Raise(GameEventType.ElementDropped, dropArgs2);

      yield return null;

      AssertElementAtPosition(9, 9, _element);
    }

    #endregion

    #region Helper Assertion Methods

    /// <summary>
    /// Asserts that a specific element is at the given grid position
    /// </summary>
    private void AssertElementAtPosition(int x, int y, PlayableElement expectedElement, string message = "element mismatch") {
      var result = _playableGrid.Grid.Get(x, y);
      Assert.That(result, Is.EqualTo(expectedElement), $"[{_playableGrid.name}] Tile({x},{y}): {message}");
    }

    /// <summary>
    /// Asserts that the grid has at least one element
    /// </summary>
    private void AssertGridIsOccupied(string message = "grid should not be empty") {
      Assert.IsFalse(_playableGrid.Grid.IsEmpty(), $"[{_playableGrid.name}] {message}");
    }

    /// <summary>
    /// Asserts that the grid is completely empty
    /// </summary>
    private void AssertGridIsEmpty(string message = "grid should be empty") {
      Assert.IsTrue(_playableGrid.Grid.IsEmpty(), $"[{_playableGrid.name}] {message}");
    }

    /// <summary>
    /// Asserts that a specific tile is empty
    /// </summary>
    private void AssertTileIsEmpty(int x, int y, string message = "tile should be empty") {
      Assert.IsTrue(_playableGrid.Grid.IsEmpty(x, y), $"[{_playableGrid.name}] Tile({x},{y}): {message}");
    }

    /// <summary>
    /// Asserts that a specific tile is occupied
    /// </summary>
    private void AssertTileIsOccupied(int x, int y, string message = "tile should be occupied") {
      Assert.IsFalse(_playableGrid.Grid.IsEmpty(x, y), $"[{_playableGrid.name}] Tile({x},{y}): {message}");
    }

    #endregion
  }
}
