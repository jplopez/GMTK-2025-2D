  using UnityEngine;
  using Ameba;

  namespace GMTK {
    [CreateAssetMenu(menuName = "GMTK/Event Channel")]
    public class GameEventChannel : EventChannel<GameEventType> {

    /// <summary>
    /// Wrapper to raise event with PlayableElementEventArgs and log debug info
    /// </summary>
    /// <param name="gameEvent"></param>
    /// <param name="eventArgs"></param>
    public void Raise(GameEventType gameEvent, PlayableElementEventArgs eventArgs) {
      base.Raise(gameEvent, eventArgs);
      this.LogDebug($"Raised game event '{gameEvent}'\n\t Args:{eventArgs}");
    }
  }

}