using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GMTK {
  public class LevelInventory : MonoBehaviour {

    [Header("Inventory Configuration")]
    [Tooltip("Reference to the master game inventory")]
    public GameInventory GameInventory;

    [Tooltip("Elements available in this level")]
    public List<LevelInventoryItem> LevelItems = new();

    [Header("Events")]
    [Tooltip("Reference to the GameEventChannel for inventory communications")]
    public GameEventChannel EventChannel;

    [Header("Debug")]
    [Tooltip("Enable debug logging for level inventory operations")]
    public bool EnableDebugLogging = false;

    // Runtime caches
    private Dictionary<int, LevelInventoryItem> _itemLookup;

    private void Awake() {
      InitializeInventory();
      AddEventListeners();
    }

    private void OnDestroy() {
      RemoveEventListeners();
    }

    #region Initialization and EventChannel setup
    private void InitializeInventory() {
      // Load GameInventory if not assigned
      if (GameInventory == null) {
        GameInventory = Resources.Load<GameInventory>("GameInventory");
        if (GameInventory == null) {
          Debug.LogError("[LevelInventory] GameInventory not found in Resources!");
          return;
        }
      }

      // Load EventChannel if not assigned
      if (EventChannel == null) {
        EventChannel = Game.Context.EventsChannel;
      }

      BuildItemLookup();
      LogDebug("LevelInventory initialized");
    }

    private void AddEventListeners() {
      if (EventChannel == null) return;

      // Use EventArgs as the parameter type, then cast inside the methods
      EventChannel.AddListener(GameEventType.InventoryAddRequest, HandleAddElementRequestWrapper);
      EventChannel.AddListener(GameEventType.InventoryRetrieveRequest, HandleRetrieveElementRequestWrapper);
      EventChannel.AddListener(GameEventType.InventoryQueryRequest, HandleQueryElementRequestWrapper);
    }

    private void RemoveEventListeners() {
      if (EventChannel == null) return;

      EventChannel.RemoveListener(GameEventType.InventoryAddRequest, HandleAddElementRequestWrapper);
      EventChannel.RemoveListener(GameEventType.InventoryRetrieveRequest, HandleRetrieveElementRequestWrapper);
      EventChannel.RemoveListener(GameEventType.InventoryQueryRequest, HandleQueryElementRequestWrapper);
    }

    private void BuildItemLookup() {
      _itemLookup = LevelItems?.ToDictionary(item => item.ElementId, item => item) ?? new Dictionary<int, LevelInventoryItem>();
    }

    #endregion

    #region Event Handler Wrappers (EventArgs -> InventoryEventData)

    private void HandleAddElementRequestWrapper(EventArgs args) {
      if (args is InventoryEventData inventoryData) {
        HandleAddElementRequest(inventoryData);
      }
      else {
        LogDebug($"Received invalid event data type for AddElementRequest: {args?.GetType()}");
      }
    }

    private void HandleRetrieveElementRequestWrapper(EventArgs args) {
      if (args is InventoryEventData inventoryData) {
        HandleRetrieveElementRequest(inventoryData);
      }
      else {
        LogDebug($"Received invalid event data type for RetrieveElementRequest: {args?.GetType()}");
      }
    }

    private void HandleQueryElementRequestWrapper(EventArgs args) {
      if (args is InventoryEventData inventoryData) {
        HandleQueryElementRequest(inventoryData);
      }
      else {
        LogDebug($"Received invalid event data type for QueryElementRequest: {args?.GetType()}");
      }
    }

    #endregion

    #region Actual Event Handlers

    private void HandleAddElementRequest(InventoryEventData eventData) {
      LogDebug($"Received add request: {eventData}");

      if (eventData.Element == null) {
        var failedData = InventoryEventData.Failed("Element is null", InventoryOperation.Add, eventData.ElementId, eventData.SourceSystem);
        RaiseInventoryEvent(GameEventType.InventoryOperationFailed, failedData);
        return;
      }

      // Enrich the event data with current inventory context
      var enrichedData = EnrichEventData(eventData);

      bool success = TryPushElementInternal(enrichedData);

      var responseData = new InventoryEventData(eventData.Element, InventoryOperation.Add, enrichedData.ElementId, 1, eventData.SourceSystem)
          .WithContext(enrichedData.WasInGrid, enrichedData.WasInInventory, enrichedData.CategoryId)
          .WithResult(success, success ? "Element added to inventory" : "Failed to add element to inventory");

      if (success) {
        // Update quantity info in response
        var item = GetItem(enrichedData.ElementId);
        if (item != null) {
          responseData.WithQuantityInfo(item.Available, item.Quantity);
        }

        RaiseInventoryEvent(GameEventType.InventoryElementAdded, responseData);

        // Destroy the element since it's now back in inventory
        Destroy(eventData.Element.gameObject);
        LogDebug($"Element {eventData.Element.name} added to inventory and destroyed");
      }
      else {
        RaiseInventoryEvent(GameEventType.InventoryOperationFailed, responseData);
      }
    }

    private void HandleRetrieveElementRequest(InventoryEventData eventData) {
      LogDebug($"Received retrieve request: {eventData}");

      var item = GetItem(eventData.ElementId);
      var retrievedElement = TryRetrieveElementInternal(eventData.ElementId);

      var responseData = new InventoryEventData(retrievedElement, InventoryOperation.Retrieve, eventData.ElementId, eventData.Quantity, eventData.SourceSystem);

      if (retrievedElement != null && item != null) {
        // Enrich with inventory context
        var gameElement = GameInventory?.GetElement(eventData.ElementId);
        responseData.CategoryId = gameElement?.CategoryId ?? -1;
        responseData.ElementName = gameElement?.Name ?? retrievedElement.name;
        responseData.WithQuantityInfo(item.Available, item.Quantity);
        responseData.WithResult(true, "Element retrieved from inventory");

        RaiseInventoryEvent(GameEventType.InventoryElementRetrieved, responseData);
      }
      else {
        responseData.WithResult(false, item == null ? "Element not found" : "No available quantity");
        RaiseInventoryEvent(GameEventType.InventoryOperationFailed, responseData);
      }
    }

    private void HandleQueryElementRequest(InventoryEventData eventData) {
      LogDebug($"Received query request: {eventData}");

      var item = GetItem(eventData.ElementId);
      var gameElement = GameInventory?.GetElement(eventData.ElementId);

      var responseData = new InventoryEventData(null, InventoryOperation.Query, eventData.ElementId, 0, eventData.SourceSystem);

      if (item != null) {
        responseData.ElementName = gameElement?.Name ?? $"Element_{eventData.ElementId}";
        responseData.CategoryId = gameElement?.CategoryId ?? -1;
        responseData.IsVisible = item.IsVisible;
        responseData.WithQuantityInfo(item.Available, item.Quantity);
        responseData.WithResult(true, $"Element found: {item.Available}/{item.Quantity} available");
      }
      else {
        responseData.WithResult(false, "Element not found in level inventory");
      }

      RaiseInventoryEvent(GameEventType.InventoryElementQueried, responseData);
    }

    #endregion

    #region Event Data Enrichment

    private InventoryEventData EnrichEventData(InventoryEventData eventData) {
      if (eventData.Element == null) return eventData;

      // Find game element info
      var gameElement = FindGameElementForSnappable(eventData.Element);
      if (gameElement != null) {
        eventData.ElementId = gameElement.Id;
        eventData.ElementName = gameElement.Name;
        eventData.CategoryId = gameElement.CategoryId;
      }

      // Check current inventory status
      var item = GetItem(eventData.ElementId);
      if (item != null) {
        eventData.AvailableQuantity = item.Available;
        eventData.TotalQuantity = item.Quantity;
        eventData.IsVisible = item.IsVisible;
        eventData.WasInInventory = true;
      }

      // Additional context can be added here based on the element's current state
      // For example, checking if it was in grid by looking at LevelGrid's occupancy map

      return eventData;
    }

    #endregion

    #region Internal Operations (Private)

    private bool TryPushElementInternal(InventoryEventData eventData) {
      if (eventData.Element == null) return false;

      var item = GetItem(eventData.ElementId);

      if (item != null) {
        // Check if inventory has space
        if (item.Quantity >= 100) { // Example max limit per item
          RaiseInventoryEvent(GameEventType.InventoryFull,
              eventData.WithResult(false, "Item has reached maximum quantity"));
          return false;
        }

        // Return one unit to existing item
        item.Return(1);
        LogDebug($"Returned element {eventData.ElementId} to inventory (Available: {item.Available})");
        RaiseInventoryUpdated(eventData);
        return true;
      }
      else {
        // Item doesn't exist in level inventory - check GameInventory for info
        var gameElement = GameInventory?.GetElement(eventData.ElementId);
        if (gameElement != null) {
          // Add new item to level inventory
          var newItem = new LevelInventoryItem(eventData.ElementId, 1, true);
          LevelItems.Add(newItem);
          BuildItemLookup();
          LogDebug($"Added new element {gameElement.Name} to level inventory");
          RaiseInventoryUpdated(eventData);
          return true;
        }
      }

      LogDebug($"Cannot push element {eventData.ElementId}: not found in GameInventory");
      return false;
    }

    private GridSnappable TryRetrieveElementInternal(int elementId) {
      var item = GetItem(elementId);
      if (item == null || !item.HasAvailable) {
        LogDebug($"Cannot retrieve element {elementId}: {(item == null ? "not found" : "no available quantity")}");
        return null;
      }

      var gameElement = GameInventory?.GetElement(elementId);
      if (gameElement?.Prefab == null) {
        LogDebug($"Cannot retrieve element {elementId}: prefab not found in GameInventory");
        return null;
      }

      if (item.TryTake(1)) {
        var instance = Instantiate(gameElement.Prefab);
        LogDebug($"Retrieved element: {gameElement.Name} (Remaining: {item.Available})");

        var updateData = new InventoryEventData(instance, InventoryOperation.Update, elementId, 1, "LevelInventory")
            .WithQuantityInfo(item.Available, item.Quantity);
        RaiseInventoryUpdated(updateData);

        return instance;
      }

      return null;
    }

    #endregion

    #region Public API Methods

    public LevelInventoryItem GetItem(int elementId) {
      LevelInventoryItem item = null;
      _itemLookup?.TryGetValue(elementId, out item);
      return item;
    }

    public List<LevelInventoryItem> GetVisibleItems() {
      return LevelItems?.Where(item => item.IsVisible).ToList() ?? new List<LevelInventoryItem>();
    }

    public List<LevelInventoryItem> GetAvailableItems() {
      return LevelItems?.Where(item => item.IsVisible && item.HasAvailable).ToList() ?? new List<LevelInventoryItem>();
    }

    public void SetElementVisibility(int elementId, bool isVisible) {
      var item = GetItem(elementId);
      if (item != null) {
        item.IsVisible = isVisible;
        LogDebug($"Set element {elementId} visibility to {isVisible}");

        var eventData = new InventoryEventData(null, InventoryOperation.SetVisibility, elementId, 0, "LevelInventory")
            .WithResult(true, $"Visibility set to {isVisible}");
        RaiseInventoryUpdated(eventData);
      }
    }

    #endregion

    #region Helper Methods

    private GameElement FindGameElementForSnappable(GridSnappable snappable) {
      if (GameInventory?.Elements == null) return null;

      string snappableName = snappable.name.Replace("(Clone)", "").Trim();

      return GameInventory.Elements.FirstOrDefault(e =>
          e.Prefab?.name == snappableName ||
          e.Name.Equals(snappableName, System.StringComparison.OrdinalIgnoreCase));
    }

    private void RaiseInventoryEvent(GameEventType eventType, InventoryEventData data) {
      if (EventChannel != null) {
        EventChannel.Raise(eventType, data);
      }
    }

    private void RaiseInventoryUpdated(InventoryEventData sourceData) {
      var updateData = new InventoryEventData(null, InventoryOperation.Update, -1, 0, "LevelInventory")
          .WithResult(true, "Inventory state updated");

      if (EventChannel != null) {
        EventChannel.Raise(GameEventType.InventoryUpdated, updateData);
      }
    }

    private void LogDebug(string message) {
      if (EnableDebugLogging) {
        Debug.Log($"[LevelInventory] {message}");
      }
    }

    #endregion

    #region Context Menu Helpers

    [ContextMenu("Log Inventory Status")]
    private void LogInventoryStatus() {
      Debug.Log("=== Level Inventory Status ===");
      foreach (var item in LevelItems) {
        var gameElement = GameInventory?.GetElement(item.ElementId);
        string elementName = gameElement?.Name ?? $"Unknown ({item.ElementId})";
        Debug.Log($"  {elementName}: {item.Available}/{item.Quantity} available (Visible: {item.IsVisible})");
      }
    }

    #endregion
  }
}
