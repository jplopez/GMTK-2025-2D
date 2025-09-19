using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for additional MonoBehaviour classes
    /// </summary>
    public class AdditionalMonoBehaviourTests
    {
        #region Checkpoint Tests
        
        private GameObject checkpointGameObject;
        private Checkpoint checkpoint;
        private Collider2D checkpointCollider;

        [SetUp]
        public void SetUpCheckpoint()
        {
            checkpointGameObject = new GameObject("TestCheckpoint");
            checkpointCollider = checkpointGameObject.AddComponent<BoxCollider2D>();
            checkpointCollider.isTrigger = true;
            checkpoint = checkpointGameObject.AddComponent<Checkpoint>();
        }

        [TearDown]
        public void TearDownCheckpoint()
        {
            if (checkpointGameObject != null)
            {
                Object.DestroyImmediate(checkpointGameObject);
            }
        }

        [Test]
        public void Checkpoint_CanBeCreated()
        {
            Assert.IsNotNull(checkpoint);
            Assert.IsInstanceOf<MonoBehaviour>(checkpoint);
        }

        [Test]
        public void Checkpoint_HasDefaultCueMode()
        {
            Assert.AreEqual(Checkpoint.VisualCueMode.OnEnter, checkpoint.CueMode);
        }

        [Test]
        public void Checkpoint_CanSetCueMode()
        {
            checkpoint.CueMode = Checkpoint.VisualCueMode.Always;
            Assert.AreEqual(Checkpoint.VisualCueMode.Always, checkpoint.CueMode);
        }

        [Test]
        public void Checkpoint_CanSetVisualCuePrefab()
        {
            GameObject prefab = new GameObject("VisualCue");
            checkpoint.VisualCuePrefab = prefab;
            Assert.AreEqual(prefab, checkpoint.VisualCuePrefab);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Checkpoint_Position_ReturnsTransformPosition()
        {
            checkpointGameObject.transform.position = new Vector3(5, 10, 0);
            Vector2 position = checkpoint.Position;
            Assert.AreEqual(new Vector2(5, 10), position);
        }

        #endregion

        #region LevelCompleteController Tests

        private GameObject levelCompleteGameObject;
        private LevelCompleteController levelCompleteController;

        [SetUp]
        public void SetUpLevelComplete()
        {
            levelCompleteGameObject = new GameObject("TestLevelComplete");
            levelCompleteController = levelCompleteGameObject.AddComponent<LevelCompleteController>();
        }

        [TearDown]
        public void TearDownLevelComplete()
        {
            if (levelCompleteGameObject != null)
            {
                Object.DestroyImmediate(levelCompleteGameObject);
            }
        }

        [Test]
        public void LevelCompleteController_CanBeCreated()
        {
            Assert.IsNotNull(levelCompleteController);
            Assert.IsInstanceOf<MonoBehaviour>(levelCompleteController);
        }

        #endregion

        #region GUIController Tests

        private GameObject guiControllerGameObject;
        private GUIController guiController;

        [SetUp]
        public void SetUpGUIController()
        {
            guiControllerGameObject = new GameObject("TestGUIController");
            guiController = guiControllerGameObject.AddComponent<GUIController>();
        }

        [TearDown]
        public void TearDownGUIController()
        {
            if (guiControllerGameObject != null)
            {
                Object.DestroyImmediate(guiControllerGameObject);
            }
        }

        [Test]
        public void GUIController_CanBeCreated()
        {
            Assert.IsNotNull(guiController);
            Assert.IsInstanceOf<MonoBehaviour>(guiController);
        }

        #endregion

        #region SnappableInputHandler Tests

        private GameObject inputHandlerGameObject;
        private SnappableInputHandler inputHandler;

        [SetUp]
        public void SetUpInputHandler()
        {
            inputHandlerGameObject = new GameObject("TestInputHandler");
            inputHandler = inputHandlerGameObject.AddComponent<SnappableInputHandler>();
        }

        [TearDown]
        public void TearDownInputHandler()
        {
            if (inputHandlerGameObject != null)
            {
                Object.DestroyImmediate(inputHandlerGameObject);
            }
        }

        [Test]
        public void SnappableInputHandler_CanBeCreated()
        {
            Assert.IsNotNull(inputHandler);
            Assert.IsInstanceOf<MonoBehaviour>(inputHandler);
        }

        #endregion

        #region LevelInventory Tests

        private GameObject levelInventoryGameObject;
        private LevelInventory levelInventory;

        [SetUp]
        public void SetUpLevelInventory()
        {
            levelInventoryGameObject = new GameObject("TestLevelInventory");
            levelInventory = levelInventoryGameObject.AddComponent<LevelInventory>();
        }

        [TearDown]
        public void TearDownLevelInventory()
        {
            if (levelInventoryGameObject != null)
            {
                Object.DestroyImmediate(levelInventoryGameObject);
            }
        }

        [Test]
        public void LevelInventory_CanBeCreated()
        {
            Assert.IsNotNull(levelInventory);
            Assert.IsInstanceOf<MonoBehaviour>(levelInventory);
        }

        #endregion

        #region ScoreTextAnimator Tests

        private GameObject scoreAnimatorGameObject;
        private ScoreTextAnimator scoreAnimator;

        [SetUp]
        public void SetUpScoreAnimator()
        {
            scoreAnimatorGameObject = new GameObject("TestScoreAnimator");
            scoreAnimator = scoreAnimatorGameObject.AddComponent<ScoreTextAnimator>();
        }

        [TearDown]
        public void TearDownScoreAnimator()
        {
            if (scoreAnimatorGameObject != null)
            {
                Object.DestroyImmediate(scoreAnimatorGameObject);
            }
        }

        [Test]
        public void ScoreTextAnimator_CanBeCreated()
        {
            Assert.IsNotNull(scoreAnimator);
            Assert.IsInstanceOf<MonoBehaviour>(scoreAnimator);
        }

        #endregion

        #region LevelManager Tests

        private GameObject levelManagerGameObject;
        private LevelManager levelManager;

        [SetUp]
        public void SetUpLevelManager()
        {
            levelManagerGameObject = new GameObject("TestLevelManager");
            levelManager = levelManagerGameObject.AddComponent<LevelManager>();
        }

        [TearDown]
        public void TearDownLevelManager()
        {
            if (levelManagerGameObject != null)
            {
                Object.DestroyImmediate(levelManagerGameObject);
            }
        }

        [Test]
        public void LevelManager_CanBeCreated()
        {
            Assert.IsNotNull(levelManager);
            Assert.IsInstanceOf<MonoBehaviour>(levelManager);
        }

        #endregion
    }
}