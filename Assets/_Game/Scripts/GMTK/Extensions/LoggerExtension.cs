
using System;
using UnityEngine;

namespace GMTK {
  public static class LoggerExtension {
    public static void Log(this MonoBehaviour obj, string message) {
      Debug.Log($"[{DateTime.Now:HH:mm:ss}] [{obj.GetType().Name}] {message}");
    }
    public static void LogWarning(this MonoBehaviour obj, string message) {
      Debug.LogWarning($"[{DateTime.Now:HH:mm:ss}] [{obj.GetType().Name}] {message}");
    }
    public static void LogError(this MonoBehaviour obj, string message) {
      Debug.LogError($"[{DateTime.Now:HH:mm:ss}] [{obj.GetType().Name}] {message}");
    }
    public static void LogException(this MonoBehaviour obj, Exception ex) {
      Debug.LogError($"[{DateTime.Now:HH:mm:ss}] [{obj.GetType().Name}] Exception: {ex.Message}\n{ex.StackTrace}");
    }
    public static void LogDebug(this MonoBehaviour obj, string message, bool enableLogging = true) {
      if (enableLogging) {
        Debug.Log($"[{DateTime.Now:HH:mm:ss}] [{obj.GetType().Name}] {message}");
      }
    }
  }
}