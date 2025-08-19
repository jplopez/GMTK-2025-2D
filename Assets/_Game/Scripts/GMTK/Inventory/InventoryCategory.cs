using UnityEngine;

namespace GMTK {
  [System.Serializable]
  public class InventoryCategory {
    [Tooltip("Unique identifier for this category")]
    public int Id;

    [Tooltip("Display name for this category")]
    public string Name;

    [Tooltip("Optional description of what this category contains")]
    [TextArea(2, 4)]
    public string Description;

    [Tooltip("Optional icon for UI representation")]
    public Sprite Icon;

    public InventoryCategory() { }

    public InventoryCategory(int id, string name, string description = "") {
      Id = id;
      Name = name;
      Description = description;
    }

    public override string ToString() => $"Category {Id}: {Name}";

    public override bool Equals(object obj) => obj is InventoryCategory other && Id == other.Id;
    
    public override int GetHashCode() => Id.GetHashCode();
  }
}
