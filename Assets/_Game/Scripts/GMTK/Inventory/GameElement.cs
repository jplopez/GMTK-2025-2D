using UnityEngine;

namespace GMTK {
  [System.Serializable]
  [CreateAssetMenu(fileName = "GameElement", menuName = "GMTK/Game Element")]
  public class GameElement : ScriptableObject {
    [Tooltip("Unique identifier for this element")]
    public int Id;

    [Tooltip("Display name of the element")]
    public string Name;

    [Tooltip("GridSnappable prefab reference for instantiation")]
    public GridSnappable Prefab;

    [Tooltip("Category this element belongs to")]
    public int CategoryId;

    [Tooltip("Optional description of the element")]
    [TextArea(2, 4)]
    public string Description;

    [Tooltip("Optional icon for UI representation")]
    public Sprite Icon;

    [Tooltip("Is this element unlocked and available to use")]
    public bool IsUnlocked = true;

    public GameElement() { }

    public GameElement(int id, string name, GridSnappable prefab, int categoryId) {
      Id = id;
      Name = name;
      Prefab = prefab;
      CategoryId = categoryId;
    }

    public GridSnappable InstantiateSnappable() => Instantiate(Prefab);

    public GridSnappable InstantiateSnappable(Transform parent) => Instantiate(Prefab, parent);

    public override string ToString() => $"Element {Id}: {Name} (Category {CategoryId})";

    public override bool Equals(object obj) {
      return obj is GameElement other && Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
  }
}
