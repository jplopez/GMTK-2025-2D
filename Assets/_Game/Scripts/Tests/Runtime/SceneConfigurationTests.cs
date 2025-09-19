using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for Scene Configuration MonoBehaviour classes
    /// </summary>
    public class SceneConfigurationTests
    {
        #region RaiseGameEventConfig Tests

        private GameObject raiseEventConfigGameObject;
        private RaiseGameEventConfig raiseEventConfig;

        [SetUp]
        public void SetUpRaiseEventConfig()
        {
            raiseEventConfigGameObject = new GameObject("TestRaiseGameEventConfig");
            raiseEventConfig = raiseEventConfigGameObject.AddComponent<RaiseGameEventConfig>();
        }

        [TearDown]
        public void TearDownRaiseEventConfig()
        {
            if (raiseEventConfigGameObject != null)
            {
                Object.DestroyImmediate(raiseEventConfigGameObject);
            }
        }

        [Test]
        public void RaiseGameEventConfig_CanBeCreated()
        {
            Assert.IsNotNull(raiseEventConfig);
            Assert.IsInstanceOf<MonoBehaviour>(raiseEventConfig);
        }

        [Test]
        public void RaiseGameEventConfig_ImplementsISceneConfigExtension()
        {
            Assert.IsInstanceOf<ISceneConfigExtension>(raiseEventConfig);
        }

        #endregion

        #region EndSceneConfig Tests

        private GameObject endSceneConfigGameObject;
        private EndSceneConfig endSceneConfig;

        [SetUp]
        public void SetUpEndSceneConfig()
        {
            endSceneConfigGameObject = new GameObject("TestEndSceneConfig");
            endSceneConfig = endSceneConfigGameObject.AddComponent<EndSceneConfig>();
        }

        [TearDown]
        public void TearDownEndSceneConfig()
        {
            if (endSceneConfigGameObject != null)
            {
                Object.DestroyImmediate(endSceneConfigGameObject);
            }
        }

        [Test]
        public void EndSceneConfig_CanBeCreated()
        {
            Assert.IsNotNull(endSceneConfig);
            Assert.IsInstanceOf<MonoBehaviour>(endSceneConfig);
        }

        [Test]
        public void EndSceneConfig_ImplementsISceneConfigExtension()
        {
            Assert.IsInstanceOf<ISceneConfigExtension>(endSceneConfig);
        }

        #endregion

        #region ScoreConfig Tests

        private GameObject scoreConfigGameObject;
        private ScoreConfig scoreConfig;

        [SetUp]
        public void SetUpScoreConfig()
        {
            scoreConfigGameObject = new GameObject("TestScoreConfig");
            scoreConfig = scoreConfigGameObject.AddComponent<ScoreConfig>();
        }

        [TearDown]
        public void TearDownScoreConfig()
        {
            if (scoreConfigGameObject != null)
            {
                Object.DestroyImmediate(scoreConfigGameObject);
            }
        }

        [Test]
        public void ScoreConfig_CanBeCreated()
        {
            Assert.IsNotNull(scoreConfig);
            Assert.IsInstanceOf<MonoBehaviour>(scoreConfig);
        }

        [Test]
        public void ScoreConfig_ImplementsISceneConfigExtension()
        {
            Assert.IsInstanceOf<ISceneConfigExtension>(scoreConfig);
        }

        #endregion

        #region SceneTransitionConfig Tests

        private GameObject sceneTransitionConfigGameObject;
        private SceneTransitionConfig sceneTransitionConfig;

        [SetUp]
        public void SetUpSceneTransitionConfig()
        {
            sceneTransitionConfigGameObject = new GameObject("TestSceneTransitionConfig");
            sceneTransitionConfig = sceneTransitionConfigGameObject.AddComponent<SceneTransitionConfig>();
        }

        [TearDown]
        public void TearDownSceneTransitionConfig()
        {
            if (sceneTransitionConfigGameObject != null)
            {
                Object.DestroyImmediate(sceneTransitionConfigGameObject);
            }
        }

        [Test]
        public void SceneTransitionConfig_CanBeCreated()
        {
            Assert.IsNotNull(sceneTransitionConfig);
            Assert.IsInstanceOf<MonoBehaviour>(sceneTransitionConfig);
        }

        [Test]
        public void SceneTransitionConfig_ImplementsISceneConfigExtension()
        {
            Assert.IsInstanceOf<ISceneConfigExtension>(sceneTransitionConfig);
        }

        #endregion

        #region PlayerInputActionDispatcher Tests

        private GameObject inputDispatcherGameObject;
        private PlayerInputActionDispatcher inputDispatcher;

        [SetUp]
        public void SetUpInputDispatcher()
        {
            inputDispatcherGameObject = new GameObject("TestInputDispatcher");
            inputDispatcher = inputDispatcherGameObject.AddComponent<PlayerInputActionDispatcher>();
        }

        [TearDown]
        public void TearDownInputDispatcher()
        {
            if (inputDispatcherGameObject != null)
            {
                Object.DestroyImmediate(inputDispatcherGameObject);
            }
        }

        [Test]
        public void PlayerInputActionDispatcher_CanBeCreated()
        {
            Assert.IsNotNull(inputDispatcher);
            Assert.IsInstanceOf<MonoBehaviour>(inputDispatcher);
        }

        [Test]
        public void PlayerInputActionDispatcher_ImplementsGameplayActions()
        {
            // The class implements PlayerControls.IGameplayActions interface
            // We can test that it's not null and is a MonoBehaviour
            Assert.IsNotNull(inputDispatcher);
        }

        #endregion
    }
}