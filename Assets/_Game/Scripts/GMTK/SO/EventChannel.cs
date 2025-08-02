using UnityEngine;
using UnityEngine.Events;

namespace GMTK {
  [CreateAssetMenu(menuName = "GMTK/Event Channel")]
  public class EventChannel : ScriptableObject {

    public UnityAction<int> OnIntRaised;
    public void RaiseInt(int amount) {
      OnIntRaised?.Invoke(amount);
    }
  }

}