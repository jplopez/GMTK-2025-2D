using UnityEngine;
using MoreMountains.Feedbacks;
using Ameba.MoreMountains.Feedbacks;

namespace GMTK.Moremountains {

  [AddComponentMenu("")]
  [FeedbackHelp("This feedback allows you to trigger events using the GMTK.GameEventChannel")]
  [FeedbackPath("Events/GameEventChannel")]
  public class GameEventFeedback : EventChannelFeedback<GameEventType> {
    
  }
}