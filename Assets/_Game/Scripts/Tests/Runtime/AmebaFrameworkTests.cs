using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ameba;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for Ameba framework MonoBehaviour classes
    /// </summary>
    public class AmebaMonoBehaviourTests
    {
        #region RegistryPool Tests

        private GameObject registryPoolGameObject;
        private RegistryPool registryPool;

        [SetUp]
        public void SetUpRegistryPool()
        {
            registryPoolGameObject = new GameObject("TestRegistryPool");
            registryPool = registryPoolGameObject.AddComponent<RegistryPool>();
        }

        [TearDown]
        public void TearDownRegistryPool()
        {
            if (registryPoolGameObject != null)
            {
                Object.DestroyImmediate(registryPoolGameObject);
            }
        }

        [Test]
        public void RegistryPool_CanBeCreated()
        {
            Assert.IsNotNull(registryPool);
            Assert.IsInstanceOf<MonoBehaviour>(registryPool);
        }

        #endregion

        #region RuntimeVariableBinder Tests

        private GameObject variableBinderGameObject;
        private RuntimeVariableBinder variableBinder;

        [SetUp]
        public void SetUpVariableBinder()
        {
            variableBinderGameObject = new GameObject("TestVariableBinder");
            variableBinder = variableBinderGameObject.AddComponent<RuntimeVariableBinder>();
        }

        [TearDown]
        public void TearDownVariableBinder()
        {
            if (variableBinderGameObject != null)
            {
                Object.DestroyImmediate(variableBinderGameObject);
            }
        }

        [Test]
        public void RuntimeVariableBinder_CanBeCreated()
        {
            Assert.IsNotNull(variableBinder);
            Assert.IsInstanceOf<MonoBehaviour>(variableBinder);
        }

        #endregion

        #region RuntimeVariablePoller Tests

        private GameObject variablePollerGameObject;
        private RuntimeVariablePoller variablePoller;

        [SetUp]
        public void SetUpVariablePoller()
        {
            variablePollerGameObject = new GameObject("TestVariablePoller");
            variablePoller = variablePollerGameObject.AddComponent<RuntimeVariablePoller>();
        }

        [TearDown]
        public void TearDownVariablePoller()
        {
            if (variablePollerGameObject != null)
            {
                Object.DestroyImmediate(variablePollerGameObject);
            }
        }

        [Test]
        public void RuntimeVariablePoller_CanBeCreated()
        {
            Assert.IsNotNull(variablePoller);
            Assert.IsInstanceOf<MonoBehaviour>(variablePoller);
        }

        #endregion

        #region AudioSourcePool Tests

        private GameObject audioPoolGameObject;
        private AudioSourcePool audioPool;

        [SetUp]
        public void SetUpAudioPool()
        {
            audioPoolGameObject = new GameObject("TestAudioPool");
            audioPool = audioPoolGameObject.AddComponent<AudioSourcePool>();
        }

        [TearDown]
        public void TearDownAudioPool()
        {
            if (audioPoolGameObject != null)
            {
                Object.DestroyImmediate(audioPoolGameObject);
            }
        }

        [Test]
        public void AudioSourcePool_CanBeCreated()
        {
            Assert.IsNotNull(audioPool);
            Assert.IsInstanceOf<MonoBehaviour>(audioPool);
        }

        #endregion

        #region ScoreKeeperController Tests

        private GameObject scoreKeeperGameObject;
        private ScoreKeeperController scoreKeeper;

        [SetUp]
        public void SetUpScoreKeeper()
        {
            scoreKeeperGameObject = new GameObject("TestScoreKeeper");
            scoreKeeper = scoreKeeperGameObject.AddComponent<ScoreKeeperController>();
        }

        [TearDown]
        public void TearDownScoreKeeper()
        {
            if (scoreKeeperGameObject != null)
            {
                Object.DestroyImmediate(scoreKeeperGameObject);
            }
        }

        [Test]
        public void ScoreKeeperController_CanBeCreated()
        {
            Assert.IsNotNull(scoreKeeper);
            Assert.IsInstanceOf<MonoBehaviour>(scoreKeeper);
        }

        #endregion
    }

    /// <summary>
    /// Unit tests for additional Ameba ScriptableObject classes
    /// </summary>
    public class AmebaScriptableObjectTests
    {
        #region ScoreGateKeeper Tests

        private ScoreGateKeeper scoreGateKeeper;

        [SetUp]
        public void SetUpScoreGateKeeper()
        {
            scoreGateKeeper = ScriptableObject.CreateInstance<ScoreGateKeeper>();
        }

        [TearDown]
        public void TearDownScoreGateKeeper()
        {
            if (scoreGateKeeper != null)
            {
                Object.DestroyImmediate(scoreGateKeeper);
            }
        }

        [Test]
        public void ScoreGateKeeper_CanBeCreated()
        {
            Assert.IsNotNull(scoreGateKeeper);
            Assert.IsInstanceOf<ScriptableObject>(scoreGateKeeper);
        }

        #endregion
    }
}