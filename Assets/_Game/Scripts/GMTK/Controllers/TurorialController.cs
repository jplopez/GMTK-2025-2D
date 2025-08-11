
using System.Collections.Generic;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// MonoBehaviour to control the toggle that shows/hides help info
  /// </summary>
  public class TurorialController : MonoBehaviour {

    public List<Transform> tutorialBoxes = new();
    public bool ShowOnStart = false;

    private bool _showingTutorialBoxes = false;

    public void Start() => ToggleTutorialBoxes(ShowOnStart);

    [ContextMenu("Toggle Tutorial Boxes")]
    public void Toggle() => ToggleTutorialBoxes(!_showingTutorialBoxes);

    private void ToggleTutorialBoxes(bool state) {
      foreach (var box in tutorialBoxes) {
        box.gameObject.SetActive(state);
      }
      _showingTutorialBoxes = state;
    }
  }
}