using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;
using System.Collections;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for SceneController MonoBehaviour
    /// </summary>
    public class SceneControllerTests
    {
        private GameObject testGameObject;
        private SceneController sceneController;

        [SetUp]
        public void SetUp()
        {
            // Create a test GameObject with SceneController
            testGameObject = new GameObject("TestSceneController");
            sceneController = testGameObject.AddComponent<SceneController>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        [Test]
        public void SceneController_CanBeCreated()
        {
            // Arrange & Act already done in SetUp

            // Assert
            Assert.IsNotNull(sceneController);
            Assert.IsInstanceOf<MonoBehaviour>(sceneController);
        }

        [Test]
        public void SceneController_HasDefaultConfigurationSource()
        {
            // Act & Assert
            Assert.AreEqual(SceneController.ConfigSource.Preset, sceneController.ConfigurationSource);
        }

        [Test]
        public void SceneController_CanSetConfigurationSource()
        {
            // Act
            sceneController.ConfigurationSource = SceneController.ConfigSource.Manual;

            // Assert
            Assert.AreEqual(SceneController.ConfigSource.Manual, sceneController.ConfigurationSource);
        }

        [Test]
        public void SceneController_EnableDebugLoggingDefaultsFalse()
        {
            // Act & Assert
            Assert.IsFalse(sceneController.EnableDebugLogging);
        }

        [Test]
        public void SceneController_CanSetDebugLogging()
        {
            // Act
            sceneController.EnableDebugLogging = true;

            // Assert
            Assert.IsTrue(sceneController.EnableDebugLogging);
        }

        [Test]
        public void SceneController_LoadingHideDelayHasDefaultValue()
        {
            // Act & Assert
            Assert.AreEqual(0.5f, sceneController.LoadingHideDelay, 0.001f);
        }

        [Test]
        public void SceneController_CanSetLoadingHideDelay()
        {
            // Act
            sceneController.LoadingHideDelay = 1.0f;

            // Assert
            Assert.AreEqual(1.0f, sceneController.LoadingHideDelay, 0.001f);
        }

        [Test]
        public void SceneController_OnSceneLoadEventsCanBeSet()
        {
            // Arrange
            GameEventType[] events = new GameEventType[] { GameEventType.GameStarted, GameEventType.GamePaused };

            // Act
            sceneController.OnSceneLoadEvents = events;

            // Assert
            Assert.IsNotNull(sceneController.OnSceneLoadEvents);
            Assert.AreEqual(2, sceneController.OnSceneLoadEvents.Length);
            Assert.AreEqual(GameEventType.GameStarted, sceneController.OnSceneLoadEvents[0]);
            Assert.AreEqual(GameEventType.GamePaused, sceneController.OnSceneLoadEvents[1]);
        }

        [Test]
        public void SceneController_SelectedConfigNameCanBeSet()
        {
            // Act
            sceneController.SelectedConfigName = "TestConfig";

            // Assert
            Assert.AreEqual("TestConfig", sceneController.SelectedConfigName);
        }
    }
}