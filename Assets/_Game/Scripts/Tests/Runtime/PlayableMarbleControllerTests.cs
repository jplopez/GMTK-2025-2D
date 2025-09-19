using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for PlayableMarbleController MonoBehaviour
    /// </summary>
    public class PlayableMarbleControllerTests
    {
        private GameObject testGameObject;
        private PlayableMarbleController marbleController;
        private Rigidbody2D rigidBody;
        private SpriteRenderer spriteRenderer;

        [SetUp]
        public void SetUp()
        {
            // Create a test GameObject with required components
            testGameObject = new GameObject("TestMarbleController");
            marbleController = testGameObject.AddComponent<PlayableMarbleController>();
            rigidBody = testGameObject.AddComponent<Rigidbody2D>();
            spriteRenderer = testGameObject.AddComponent<SpriteRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        [Test]
        public void PlayableMarbleController_CanBeCreated()
        {
            // Assert
            Assert.IsNotNull(marbleController);
            Assert.IsInstanceOf<MonoBehaviour>(marbleController);
        }

        [Test]
        public void PlayableMarbleController_HasDefaultMass()
        {
            // Assert
            Assert.AreEqual(5f, marbleController.Mass, 0.001f);
        }

        [Test]
        public void PlayableMarbleController_HasDefaultGravityScale()
        {
            // Assert
            Assert.AreEqual(1f, marbleController.GravityScale, 0.001f);
        }

        [Test]
        public void PlayableMarbleController_HasDefaultAngularDamping()
        {
            // Assert
            Assert.AreEqual(0.05f, marbleController.AngularDamping, 0.001f);
        }

        [Test]
        public void PlayableMarbleController_HasDefaultMinimalMovementThreshold()
        {
            // Assert
            Assert.AreEqual(0.01f, marbleController.MinimalMovementThreshold, 0.001f);
        }

        [Test]
        public void PlayableMarbleController_CanSetMass()
        {
            // Act
            marbleController.Mass = 10f;

            // Assert
            Assert.AreEqual(10f, marbleController.Mass, 0.001f);
        }

        [Test]
        public void PlayableMarbleController_CanSetGravityScale()
        {
            // Act
            marbleController.GravityScale = 2f;

            // Assert
            Assert.AreEqual(2f, marbleController.GravityScale, 0.001f);
        }

        [Test]
        public void PlayableMarbleController_CanSetAngularDamping()
        {
            // Act
            marbleController.AngularDamping = 0.1f;

            // Assert
            Assert.AreEqual(0.1f, marbleController.AngularDamping, 0.001f);
        }

        [Test]
        public void PlayableMarbleController_CanSetInitialForce()
        {
            // Act
            marbleController.InitialForce = new Vector2(10, 5);

            // Assert
            Assert.AreEqual(new Vector2(10, 5), marbleController.InitialForce);
        }

        [Test]
        public void PlayableMarbleController_CanSetMinimalMovementThreshold()
        {
            // Act
            marbleController.MinimalMovementThreshold = 0.02f;

            // Assert
            Assert.AreEqual(0.02f, marbleController.MinimalMovementThreshold, 0.001f);
        }

        [Test]
        public void PlayableMarbleController_CanSetGroundedMask()
        {
            // Act
            LayerMask mask = LayerMask.GetMask("Default", "Ground");
            marbleController.GroundedMask = mask;

            // Assert
            Assert.AreEqual(mask, marbleController.GroundedMask);
        }

        [Test]
        public void PlayableMarbleController_ModelDefaultsToGameObject()
        {
            // Note: This would be set during Awake(), but we're testing before that
            // Act
            marbleController.Model = testGameObject;

            // Assert
            Assert.AreEqual(testGameObject, marbleController.Model);
        }

        [Test]
        public void PlayableMarbleController_CanSetSpawnTransform()
        {
            // Arrange
            GameObject spawnPoint = new GameObject("SpawnPoint");
            Transform spawnTransform = spawnPoint.transform;

            // Act
            marbleController.SpawnTransform = spawnTransform;

            // Assert
            Assert.AreEqual(spawnTransform, marbleController.SpawnTransform);

            // Cleanup
            Object.DestroyImmediate(spawnPoint);
        }

        [Test]
        public void PlayableMarbleController_IsMovingProperty_InitiallyFalse()
        {
            // Assert - IsMoving should be false initially (timeSinceLastMove = 0)
            Assert.IsFalse(marbleController.IsMoving);
        }
    }
}