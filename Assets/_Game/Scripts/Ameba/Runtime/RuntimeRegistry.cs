using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ameba {

  [CreateAssetMenu(fileName = "RuntimeRegistry", menuName = "Ameba/Runtime/GameObject Registry")]
  public class RuntimeRegistry : ScriptableObject {

    [Tooltip("The regitered GameObjects. GameObjects are wrapped in the RegistryEntry class")]
    public List<RegistryEntry> Entries = new();

    public GameObject GetPrefab(string id) {
      if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("RuntimeRegistry: id cannot be null or empty");
      var entry = Entries.Find(x => id.Equals(x.id));
      if (entry != null) {
        return entry.Prefab;
      }
      return default;
    }

    public bool TryGetPrefab(string id, out GameObject prefab) {
      prefab = GetPrefab(id);
      return prefab != null;
    }

    public T GetPrefab<T>(string id) {
      GameObject prefab = GetPrefab(id);
      if (prefab != null) {
        if (prefab.TryGetComponent(out T component)) {
          return component;
        }
        else {
          Debug.LogWarning($"[RuntimeRegistry] The type {typeof(T).Name} was not found for id '{id}'");
        }
      }
      return default;
    }

    public bool TryGetPrefab<T>(string id, out T prefab) {
      prefab = GetPrefab<T>(id);
      return prefab != null;
    }

    public RegistryEntry GetEntry(string id) => Entries.Find(e => id.Equals(e.id));

    public bool Contains(string id) => Entries.Contains(GetEntry(id));

    public bool ContainsPrefab(GameObject prefab) => Entries.Any(e => e.Prefab.Equals(prefab));

    public List<string> GetIds() => Entries.Select(e => e.id).ToList();

    public List<GameObject> GetPrefabs() => Entries.Select(e => e.Prefab).ToList();
  }

  [Serializable]
  public class RegistryEntry : IPoolable {
    public string id;
    public GameObject Prefab;

    //IPoolable methods
    public int PrewarmCount { get; set; } = 5;
    public string PrefabId { get => id; set => id = value; }

    public virtual void OnSpawn() { }
    public virtual void OnReturn() { }
  }

}
