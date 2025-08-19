using UnityEngine;

namespace GMTK {
  [System.Serializable]
  public class LevelInventoryItem {
    [Tooltip("Reference to the element definition in GameInventory")]
    public int ElementId;

    [Tooltip("How many of this element are available in this level")]
    public int Quantity;

    [Tooltip("How many are currently being used")]
    public int InUse;

    [Tooltip("Is this element visible to the player in this level")]
    public bool IsVisible = true;

    public int Available => Quantity - InUse;
    public bool HasAvailable => Available > 0;
    public bool IsEmpty => Quantity <= 0;

    public LevelInventoryItem() { }

    public LevelInventoryItem(int elementId, int quantity, bool isVisible = true) {
      ElementId = elementId;
      Quantity = quantity;
      InUse = 0;
      IsVisible = isVisible;
    }

    public bool TryTake(int amount = 1) {
      if (Available >= amount) {
        InUse += amount;
        return true;
      }
      return false;
    }

    public void Return(int amount = 1) {
      InUse = Mathf.Max(0, InUse - amount);
    }

    public void AddQuantity(int amount) {
      Quantity = Mathf.Max(0, Quantity + amount);
    }

    public override string ToString() {
      return $"ElementId {ElementId}: {Available}/{Quantity} available (InUse: {InUse})";
    }
  }
}
