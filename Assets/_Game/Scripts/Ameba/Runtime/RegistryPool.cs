using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ameba {

  public class RegistryPool : MonoBehaviour {

    [Tooltip("The GameObject registry for this pool")]
    public RuntimeRegistry Registry;
    [Tooltip("The initial number of instances of each item in the pool, unless the item specifies a number")]
    public int InitialPoolCount = 5;
    [Tooltip("If true, the pool will create all GameObject's instance on start")]
    public bool FillPoolAtStart = true;

    protected Dictionary<string, Queue<GameObject>> _pool = new();
    protected Dictionary<GameObject, string> _prefabToId = new();

    public virtual void Awake() {
      foreach (var entry in Registry.Entries) {
        _prefabToId[entry.Prefab] = entry.id;
      }
    }

    public virtual void Start() => PrewarmAll();

    public virtual void PrewarmAll() {
      if (!FillPoolAtStart) return;

      foreach (var entry in Registry.Entries) {
        if (entry is IPoolable poolable) {
          Prewarm(entry.id, poolable.PrewarmCount);
        }
        else {
          Prewarm(entry.id, InitialPoolCount);
        }
      }
    }


    public virtual void Prewarm(string id, int count) {
      if (!Registry.TryGetPrefab(id, out GameObject prefab)) return;

      if (!_pool.TryGetValue(id, out var queue)) {
        queue = new Queue<GameObject>();
        _pool[id] = queue;
      }

      for (int i = 0; i < count; i++) {
        var instance = Instantiate(prefab);
        instance.SetActive(false);
        if (instance.TryGetComponent<IPoolable>(out var poolable)) {
          poolable.PrefabId = id;
          poolable.OnSpawn();
        }
        queue.Enqueue(instance);
      }
    }

    public GameObject Get(string id) {
      // validate id
      if (string.IsNullOrEmpty(id)) {
        Debug.LogWarning("[RegistryPool] Requested ID is null or empty.");
        return null;
      }
      //confirm id exists in registry
      if (!Registry.TryGetPrefab(id, out GameObject prefab)) {
        Debug.LogWarning($"[RegistryPool] No prefab found for ID '{id}'.");
        return null;
      }
      //get queue for the id
      if (!_pool.TryGetValue(id, out var queue)) {
        queue = new Queue<GameObject>();
        _pool[id] = queue;
      }

      // Get prefab from pool or create a clone
      if (_pool[id].Count == 0)
        Debug.Log($"[RegistryPool] Pool for '{id}' was empty. Instantiating new object.");

      var instance = _pool[id].Count > 0
        ? _pool[id].Dequeue()
        : Instantiate(prefab);
      // Activate
      instance.SetActive(true);

      // Spawn (if implemenet IPoolable)
      if (instance.TryGetComponent<IPoolable>(out var poolable)) {
        poolable.OnSpawn();
      }
      //return instance
      return instance;
    }

    public T Get<T>(string id) where T : Component {
      var go = Get(id);
      return go != null ? go.GetComponent<T>() : null;
    }

    public bool TryGet<T>(string id, out T component) where T : Component {
      component = Get<T>(id);
      return component != null;
    }

    public void Return(GameObject instance) {
      //validate instance
      if (instance == null) return;

      // try get Ipoolable to call OnReturn and enqueue
      if (instance.TryGetComponent<IPoolable>(out var poolable)) {
        poolable.OnReturn();
        var poolableId = poolable.PrefabId;
        //ensure object isn't already in the pool
        if (_pool[poolableId].Contains(instance)) {
          Debug.LogWarning($"[RegistryPool] Instance of '{poolableId}' already in pool.");
          return;
        }
        _pool[poolableId].Enqueue(instance);
      }
      else {
        Debug.LogWarning("Returned object is not IPoolable");
        Destroy(instance);
      }
    }

    public int GetPoolSize(string id) => _pool.TryGetValue(id, out var q) ? q.Count : 0;

    public List<GameObject> GetInactive() {
      List<GameObject> list = new();
      foreach (var queue in _pool.Values) {
        list.AddRange(queue.Where(x => !x.activeSelf).ToList());
      }
      return list;
    }

#if UNITY_EDITOR
    [ContextMenu("Clear All Pools")]
    public void ClearPools() {
      foreach (var queue in _pool.Values) {
        while (queue.Count > 0) {
          Destroy(queue.Dequeue());
        }
      }
      _pool.Clear();
    }

#endif

  }
}