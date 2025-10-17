using UnityEngine;

namespace Ameba.MoreMountains.Feedbacks {
  /// <summary>
  /// Interface that feedbacks can implement to receive value injection from ValueHolderFeedback
  /// </summary>
  public interface IValueReceiver {
    /// when in forced value mode, this will contain the forced value holder that will be used
    ValueHolderFeedback ForcedValueHolder { get; set; }

    /// <summary>
    /// Forces value injection from the assigned value holder
    /// </summary>
    void ForceAutomateValueInjection();

    /// <summary>
    /// Returns whether this feedback supports automated value injection
    /// </summary>
    bool HasAutomatedValueInjection { get; }
  }
}