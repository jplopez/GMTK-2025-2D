using UnityEngine;
using UnityEngine.Events;

namespace GMTK {
  [CreateAssetMenu(menuName = "GMTK/Event Channel")]
  public class EventChannel : ScriptableObject {

    public enum EventChannelType {
      RaiseInt,
      SetInt
    }


    private UnityAction<int> OnIntRaised;
    private UnityAction<int> OnIntSet;

    public void NotifyRaiseInt(int amount) {
      OnIntRaised?.Invoke(amount);
    }

    public void NotifySetInt(int value) {
       OnIntSet?.Invoke(value);
    }

    public void AddChannelListener(EventChannelType type, UnityAction<int> listener) {
      switch (type) {
        case EventChannelType.RaiseInt:
          OnIntRaised += listener;
          break;
        case EventChannelType.SetInt:
          OnIntSet += listener;
          break;
        default:
          Debug.LogWarning($"[EventChannel] Unknown EventChannelType: {type}");
          break;
      }
    }

    public void RemoveChannelListener(EventChannelType type, UnityAction<int> listener) {
      switch (type) {
        case EventChannelType.RaiseInt:
          OnIntRaised += listener;
          break;
        case EventChannelType.SetInt:
          OnIntSet += listener;
          break;
        default:
          Debug.LogWarning($"[EventChannel] Unknown EventChannelType: {type}");
          break;
      }
    }
  }

}