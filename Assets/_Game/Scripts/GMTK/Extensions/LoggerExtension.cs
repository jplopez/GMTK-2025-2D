using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMTK {

  //i'm too lazy to set a propper logger system for now so here is a quick extension for MonoBehaviour and ScriptableObject
  public static class LoggerExtension {

    #region Logger
    public enum LoggerLevels { Info, Warning, Error, Exception, Debug }
   
    public static bool DisableAllLogging = false;

    public static Dictionary<LoggerLevels, bool> EnabledLogLevels = new() {
      { LoggerLevels.Info, true },
      { LoggerLevels.Warning, true },
      { LoggerLevels.Error, true },
      { LoggerLevels.Exception, true },
      { LoggerLevels.Debug, false }
    };

    public static string DateFormat = "HH:mm:ss";

    public static string GetTimestamp() {
      return DateTime.Now.ToString(DateFormat);
    }
    public static bool CanLog(LoggerLevels level) {
      return !DisableAllLogging && EnabledLogLevels.TryGetValue(level, out var isEnabled) && isEnabled;
    }
    public static void SetLogLevel(LoggerLevels level, bool isEnabled) {
      if (EnabledLogLevels.ContainsKey(level)) {
        EnabledLogLevels[level] = isEnabled;
      } else {
        EnabledLogLevels.Add(level, isEnabled);
      }
    }

    public static void DoLog(LoggerLevels level, string message) {
      if (CanLog(level)) {
        switch (level) {
          case LoggerLevels.Info:
            Debug.Log($"[{GetTimestamp()}] {message}");
            break;
          case LoggerLevels.Warning:
            Debug.LogWarning($"[{GetTimestamp()}] {message}");
            break;
          case LoggerLevels.Error:
            Debug.LogError($"[{GetTimestamp()}] {message}");
            break;
          case LoggerLevels.Exception:
            Debug.LogError($"[{GetTimestamp()}] [EXCEPTION] {message}");
            break;
          case LoggerLevels.Debug:
            Debug.Log($"[{GetTimestamp()}] [DEBUG] {message}");
            break;
        }
      }
    }
    #endregion

    #region MonoBehaviour Logger Extensions

    public static void Log(this MonoBehaviour obj, string message) {
      LoggerExtension.DoLog(LoggerLevels.Info, $"[{obj.GetType().Name}] {message}");
    }
    public static void LogWarning(this MonoBehaviour obj, string message) {
      LoggerExtension.DoLog(LoggerLevels.Warning, $"[{obj.GetType().Name}] {message}");
    }
    public static void LogError(this MonoBehaviour obj, string message) {
      LoggerExtension.DoLog(LoggerLevels.Error, $"[{obj.GetType().Name}] {message}");
    }
    
    public static void LogException(this MonoBehaviour obj, Exception ex) {
      LoggerExtension.DoLog(LoggerLevels.Exception, $"[{obj.GetType().Name}] Exception: {ex.Message}\n{ex.StackTrace}");
    }

    public static void LogDebug(this MonoBehaviour obj, string message) {
      LoggerExtension.DoLog(LoggerLevels.Debug, $"[{obj.GetType().Name}] {message}");
    }

    #endregion

    #region ScriptableObject Logger Extensions

    public static void Log(this ScriptableObject obj, string message) {
      LoggerExtension.DoLog(LoggerLevels.Info, $"[{obj.GetType().Name}] {message}");
    }
    public static void LogWarning(this ScriptableObject obj, string message) {
      LoggerExtension.DoLog(LoggerLevels.Warning, $"[{obj.GetType().Name}] {message}");
    }

    public static void LogError(this ScriptableObject obj, string message) {
      LoggerExtension.DoLog(LoggerLevels.Error, $"[{obj.GetType().Name}] {message}");
    }

    public static void LogException(this ScriptableObject obj, Exception ex) {
      LoggerExtension.DoLog(LoggerLevels.Exception, $"[{obj.GetType().Name}] Exception: {ex.Message}\n{ex.StackTrace}");
    }

    public static void LogDebug(this ScriptableObject obj, string message) {
      LoggerExtension.DoLog(LoggerLevels.Debug, $"[{obj.GetType().Name}] {message}"); 
    }

    #endregion

  }
}