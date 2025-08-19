
using System;
using UnityEngine;

namespace GMTK {
  [System.Serializable]
  public class InventoryEventData : EventArgs {
    [Header("Element Information")]
    public GridSnappable Element;
    public int ElementId;
    public string ElementName;
    public int CategoryId;

    [Header("Quantity Information")]
    public int Quantity;
    public int RequestedQuantity;
    public int AvailableQuantity;
    public int TotalQuantity;

    [Header("Operation Context")]
    public InventoryOperation Operation;
    public bool Success;
    public string Message;
    public Vector3 WorldPosition;
    public string SourceSystem;

    [Header("Additional Context")]
    public bool WasInGrid;
    public bool WasInInventory;
    public bool IsVisible;
    public bool ForceOperation;

    public InventoryEventData(
        GridSnappable element,
        InventoryOperation operation,
        int elementId = -1,
        int quantity = 1,
        string sourceSystem = "Unknown") {

      Element = element;
      ElementId = elementId;
      if (element != null) {
        ElementName = (string.IsNullOrEmpty(element.name)) ? "Unknown" : element.name;
        WorldPosition = element.transform.position;
      } else {

      }
        Operation = operation;
      Quantity = quantity;
      RequestedQuantity = quantity;
      Success = true;
      Message = string.Empty;
      SourceSystem = sourceSystem;
      CategoryId = -1;
      AvailableQuantity = 0;
      TotalQuantity = 0;
      WasInGrid = false;
      WasInInventory = false;
      IsVisible = true;
      ForceOperation = false;
    }

    public static InventoryEventData CreateAddRequest(GridSnappable element, string sourceSystem = "LevelGrid") {
      return new InventoryEventData(element, InventoryOperation.Add, -1, 1, sourceSystem);
    }

    public static InventoryEventData CreateRetrieveRequest(int elementId, int quantity = 1, string sourceSystem = "UI") {
      return new InventoryEventData(null, InventoryOperation.Retrieve, elementId, quantity, sourceSystem) {
        ElementName = $"ElementID_{elementId}"
      };
    }

    public static InventoryEventData CreateQueryRequest(int elementId, string sourceSystem = "System") {
      return new InventoryEventData(null, InventoryOperation.Query, elementId, 0, sourceSystem);
    }

    public static InventoryEventData Failed(string message, InventoryOperation operation, int elementId = -1, string sourceSystem = "System") {
      return new InventoryEventData(null, operation, elementId, 0, sourceSystem) {
        Success = false,
        Message = message
      };
    }

    public InventoryEventData WithContext(bool wasInGrid, bool wasInInventory, int categoryId = -1) {
      WasInGrid = wasInGrid;
      WasInInventory = wasInInventory;
      CategoryId = categoryId;
      return this;
    }

    public InventoryEventData WithQuantityInfo(int available, int total) {
      AvailableQuantity = available;
      TotalQuantity = total;
      return this;
    }

    public InventoryEventData WithResult(bool success, string message) {
      Success = success;
      Message = message;
      return this;
    }
    public override string ToString() {
      return $"InventoryEvent: {Operation} | Element: {ElementName} (ID: {ElementId}) | " +
             $"Qty: {Quantity} | Success: {Success} | Source: {SourceSystem}";
    }
  }

  public enum InventoryOperation {
    Add,           // Adding element to inventory
    Retrieve,      // Getting element from inventory
    Query,         // Checking availability
    Update,        // Updating quantities
    SetVisibility, // Changing element visibility
    Clear,         // Clearing inventory
    Organize       // Reorganizing inventory
  }
}