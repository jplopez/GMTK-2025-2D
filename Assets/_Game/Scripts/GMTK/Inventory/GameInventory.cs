using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GMTK {
  [CreateAssetMenu(menuName = "GMTK/Game Inventory", fileName = "GameInventory")]
  public class GameInventory : ScriptableObject {

    [Header("Categories")]
    [Tooltip("All available element categories in the game")]
    public List<InventoryCategory> Categories = new();

    [Header("Elements")]
    [Tooltip("All available elements in the game")]
    public List<GameElement> Elements = new();

    [Header("Debug")]
    [Tooltip("Enable debug logging for inventory operations")]
    public bool EnableDebugLogging = false;

    // Cached lookups for performance
    private Dictionary<int, InventoryCategory> _categoryLookup;
    private Dictionary<int, GameElement> _elementLookup;
    private Dictionary<int, List<GameElement>> _elementsByCategory;

    private void OnEnable() {
      BuildLookupCaches();
    }

    private void OnValidate() {
      BuildLookupCaches();
    }

    private void BuildLookupCaches() {
      // Build category lookup
      _categoryLookup = Categories?.ToDictionary(c => c.Id, c => c) ?? new Dictionary<int, InventoryCategory>();

      // Build element lookup
      _elementLookup = Elements?.ToDictionary(e => e.Id, e => e) ?? new Dictionary<int, GameElement>();

      // Build elements by category lookup
      _elementsByCategory = new Dictionary<int, List<GameElement>>();
      if (Elements != null) {
        foreach (var element in Elements) {
          if (!_elementsByCategory.ContainsKey(element.CategoryId)) {
            _elementsByCategory[element.CategoryId] = new List<GameElement>();
          }
          _elementsByCategory[element.CategoryId].Add(element);
        }
      }
    }

    #region Category Operations

    public InventoryCategory GetCategory(int categoryId) {
      InventoryCategory category = null;
      _categoryLookup?.TryGetValue(categoryId, out category);
      return category;
    }

    public List<InventoryCategory> GetAllCategories() {
      return Categories?.ToList() ?? new List<InventoryCategory>();
    }

    public bool HasCategory(int categoryId) {
      return _categoryLookup?.ContainsKey(categoryId) ?? false;
    }

    #endregion

    #region Element Operations

    public GameElement GetElement(int elementId) {
      GameElement element = null;
      _elementLookup?.TryGetValue(elementId, out element);
      return element;
    }

    public GameElement GetElementByName(string name) {
      return Elements?.FirstOrDefault(e => e.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    public List<GameElement> GetElementsByCategory(int categoryId) {
      List<GameElement> elements = new();
      _elementsByCategory?.TryGetValue(categoryId, out elements);
      return elements?.ToList() ?? new List<GameElement>();
    }

    public List<GameElement> GetUnlockedElements() {
      return Elements?.Where(e => e.IsUnlocked).ToList() ?? new List<GameElement>();
    }

    public List<GameElement> GetUnlockedElementsByCategory(int categoryId) {
      return GetElementsByCategory(categoryId).Where(e => e.IsUnlocked).ToList();
    }

    public bool HasElement(int elementId) {
      return _elementLookup?.ContainsKey(elementId) ?? false;
    }

    public bool IsElementUnlocked(int elementId) {
      var element = GetElement(elementId);
      return element?.IsUnlocked ?? false;
    }

    #endregion

    #region Progression Management

    public void UnlockElement(int elementId) {
      var element = GetElement(elementId);
      if (element != null) {
        element.IsUnlocked = true;
        LogDebug($"Unlocked element: {element.Name}");
      }
    }

    public void LockElement(int elementId) {
      var element = GetElement(elementId);
      if (element != null) {
        element.IsUnlocked = false;
        LogDebug($"Locked element: {element.Name}");
      }
    }

    public void UnlockCategory(int categoryId) {
      var elements = GetElementsByCategory(categoryId);
      foreach (var element in elements) {
        element.IsUnlocked = true;
      }
      LogDebug($"Unlocked category: {GetCategory(categoryId)?.Name}");
    }

    #endregion

    #region Utility Methods

    private void LogDebug(string message) {
      if (EnableDebugLogging) {
        Debug.Log($"[GameInventory] {message}");
      }
    }

    [ContextMenu("Rebuild Caches")]
    private void ForceBuildCaches() => BuildLookupCaches();

    [ContextMenu("Log Inventory Contents")]
    private void LogInventoryContents() {
      Debug.Log($"=== GameInventory Contents ===");
      Debug.Log($"Categories: {Categories?.Count ?? 0}");
      foreach (var category in Categories ?? new List<InventoryCategory>()) {
        var elementCount = GetElementsByCategory(category.Id).Count;
        Debug.Log($"  - {category.Name} (ID: {category.Id}): {elementCount} elements");
      }
      Debug.Log($"Total Elements: {Elements?.Count ?? 0}");
    }

    #endregion
  }
}
