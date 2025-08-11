using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ameba;

namespace GMTK {

  [CreateAssetMenu(menuName = "GMTK/Level Definition")]
  public class LevelDefinition : RuntimeRegistry {

    protected Dictionary<string, LevelElementEntry> _lookup = new();

    private void OnEnable() => InitializeLookup();

    private void OnDisable() => _lookup.Clear();

    public void InitializeLookup() {
      _lookup ??= new Dictionary<string, LevelElementEntry>();
      _lookup.Clear();
      foreach (var e in Entries) _lookup[e.id] = new LevelElementEntry() {
        id = e.id,
        Prefab = e.Prefab
      };
    }

    public LevelElementEntry GetLevelElementEntry(string id) => _lookup[id];

    public bool TryGetLevelElementEntry(string id, out LevelElementEntry entry) => (entry = GetLevelElementEntry(id)) != null;
    
    public Vector2Int? GetPosition(string id) => GetLevelElementEntry(id)?.gridPosition;

    public List<LevelElementEntry> GetEntriesInPosition(Vector2Int position) {
      return _lookup.Values.Where(e => e.gridPosition.Equals(position))?.ToList();
    }

    public bool IsPositionOccupied(Vector2Int position) => GetEntriesInPosition(position)?.Count > 0;

    public bool IsPlayable(string id) => GetLevelElementEntry(id)?.isPlayable ?? false;

    public bool LeftInDrawer(string id) => GetLevelElementEntry(id)?.countInDrawer > 0;

    public List<LevelElementEntry> GetPlayables() => _lookup.Values.Where(e => e.isPlayable)?.ToList();

    public List<LevelElementEntry> GetPlayablesAndLeftInDrawer() => _lookup.Values.Where(e => e.isPlayable && e.countInDrawer > 0)?.ToList();

  }

  [Serializable]
  public class LevelElementEntry : RegistryEntry {
    public Vector2Int gridPosition = Vector2Int.zero;
    public bool isPlayable = true;
    public int countInDrawer = 5; // Only used if isPlayable
  }

}