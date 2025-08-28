using System;
using UnityEngine;
using UnityEngine.Events;

namespace GMTK {

  /// <summary>
  /// Convenience class to extend LevelManager behaviours preventing them to bloat
  /// </summary>
  public abstract class LevelExtension : MonoBehaviour {

    protected LevelManager _host;

    public LevelManager Host => _host;

    protected virtual void Awake() {
      if (_host == null) {
        if (!TryGetComponent(out _host)) {
          Debug.LogWarning($"LevelExtension {name} can't find LevelManager _host. LevelExtension must be in the same GamObject as the LevelManager");
        }
      }
    }

    public abstract void Initialize();
    public abstract void OnStartLevel();

    public abstract void OnUpdateLevel();

    public abstract void OnResetLevel();

    public abstract void OnEnterCheckpoint();

    public abstract void OnExitCheckpoint();
    public abstract void OnCompleteLevel();

  }
}