using Ameba;
using UnityEngine;

namespace GMTK {
  [AddComponentMenu("GMTK/Scene Configurations/Score Config")]
  public class ScoreConfig : MonoBehaviour, ISceneConfigExtension {

    [Tooltip("the reference to the ScoreGateKeeper to configure. If empty, this Config will look for one through 'Services'")]
    public ScoreGateKeeper scoreKeeper;

    [Tooltip("the transform of the ScoreGateKeeper. If empty, the config will assume the current GameObject's transform")]
    public Transform scoreTransform;

    public void ApplyConfig(SceneController controller) {

      if(scoreKeeper == null) {
        scoreKeeper = Services.Get<ScoreGateKeeper>();
      }
      if(scoreKeeper == null) {
        Debug.LogError($"{name} : can't find an instance of ScoreGateKeeper. Make sure the Services system has definition for this type");
        return;
      }
      scoreTransform = (scoreTransform == null) ? transform : scoreTransform;
      var scoreStrategy = gameObject.AddComponent<TimeBasedScoreCalculator>();
      scoreKeeper.SetStrategy(scoreStrategy, scoreTransform);

    }

    public bool CanApplyOnType(SceneType type) => true;
  }
}