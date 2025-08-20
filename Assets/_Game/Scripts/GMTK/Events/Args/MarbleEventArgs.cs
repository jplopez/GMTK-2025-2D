using System;
using UnityEngine;

namespace GMTK {

  public class MarbleEventArgs : EventArgs {
    public GameEventType EventType;
    public Vector2 Position;
    public PlayableMarbleController Marble;
    public Checkpoint HitCheckpoint;
    
  }
}