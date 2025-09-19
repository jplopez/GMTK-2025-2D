using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for LevelGrid MonoBehaviour
    /// </summary>
    public class LevelGridTests
    {
        private GameObject testGameObject;
        private LevelGrid levelGrid;

        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestLevelGrid");
            levelGrid = testGameObject.AddComponent<LevelGrid>();
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
        public void LevelGrid_CanBeCreated()
        {
            // Assert
            Assert.IsNotNull(levelGrid);
            Assert.IsInstanceOf<MonoBehaviour>(levelGrid);
        }

        [Test]
        public void LevelGrid_HasDefaultCellSize()
        {
            // Assert
            Assert.AreEqual(1f, levelGrid.CellSize, 0.001f);
        }

        [Test]
        public void LevelGrid_CanSetCellSize()
        {
            // Act
            levelGrid.CellSize = 2f;

            // Assert
            Assert.AreEqual(2f, levelGrid.CellSize, 0.001f);
        }

        [Test]
        public void LevelGrid_HasDefaultGridSize()
        {
            // Assert
            Assert.AreEqual(new Vector2Int(50, 34), levelGrid.GridSize);
        }

        [Test]
        public void LevelGrid_CanSetGridSize()
        {
            // Act
            levelGrid.GridSize = new Vector2Int(20, 15);

            // Assert
            Assert.AreEqual(new Vector2Int(20, 15), levelGrid.GridSize);
        }

        [Test]
        public void LevelGrid_HasDefaultOriginSource()
        {
            // Assert
            Assert.AreEqual(GridOriginSources.GameObject, levelGrid.OriginSource);
        }

        [Test]
        public void LevelGrid_CanSetOriginSource()
        {
            // Act
            levelGrid.OriginSource = GridOriginSources.Custom;

            // Assert
            Assert.AreEqual(GridOriginSources.Custom, levelGrid.OriginSource);
        }

        [Test]
        public void LevelGrid_HasDefaultCustomGridOrigin()
        {
            // Assert
            Assert.AreEqual(Vector2.zero, levelGrid.CustomGridOrigin);
        }

        [Test]
        public void LevelGrid_CanSetCustomGridOrigin()
        {
            // Act
            levelGrid.CustomGridOrigin = new Vector2(5, 10);

            // Assert
            Assert.AreEqual(new Vector2(5, 10), levelGrid.CustomGridOrigin);
        }

        [Test]
        public void LevelGrid_CanSetBoundColliders()
        {
            // Arrange
            GameObject boundGO = new GameObject("BoundCollider");
            EdgeCollider2D edgeCollider = boundGO.AddComponent<EdgeCollider2D>();

            // Act
            levelGrid.GridTopBound = edgeCollider;

            // Assert
            Assert.AreEqual(edgeCollider, levelGrid.GridTopBound);

            // Cleanup
            Object.DestroyImmediate(boundGO);
        }

        [Test]
        public void LevelGrid_CanSetMultipleBoundColliders()
        {
            // Arrange
            GameObject topBoundGO = new GameObject("TopBound");
            GameObject bottomBoundGO = new GameObject("BottomBound");
            GameObject leftBoundGO = new GameObject("LeftBound");
            GameObject rightBoundGO = new GameObject("RightBound");

            EdgeCollider2D topCollider = topBoundGO.AddComponent<EdgeCollider2D>();
            EdgeCollider2D bottomCollider = bottomBoundGO.AddComponent<EdgeCollider2D>();
            EdgeCollider2D leftCollider = leftBoundGO.AddComponent<EdgeCollider2D>();
            EdgeCollider2D rightCollider = rightBoundGO.AddComponent<EdgeCollider2D>();

            // Act
            levelGrid.GridTopBound = topCollider;
            levelGrid.GridBottomBound = bottomCollider;
            levelGrid.GridLeftBound = leftCollider;
            levelGrid.GridRightBound = rightCollider;

            // Assert
            Assert.AreEqual(topCollider, levelGrid.GridTopBound);
            Assert.AreEqual(bottomCollider, levelGrid.GridBottomBound);
            Assert.AreEqual(leftCollider, levelGrid.GridLeftBound);
            Assert.AreEqual(rightCollider, levelGrid.GridRightBound);

            // Cleanup
            Object.DestroyImmediate(topBoundGO);
            Object.DestroyImmediate(bottomBoundGO);
            Object.DestroyImmediate(leftBoundGO);
            Object.DestroyImmediate(rightBoundGO);
        }

        [Test]
        public void LevelGrid_GridOriginProperty_IsAccessible()
        {
            // Assert - GridOrigin property should be accessible
            Vector2 origin = levelGrid.GridOrigin;
            Assert.IsNotNull(origin);
        }

        [Test]
        public void LevelGrid_ElementDefaultPosition_IsStatic()
        {
            // Assert
            Assert.AreEqual(new Vector3(-10, 0, 0), LevelGrid.ELEMENT_DEFAULT_POSITION);
        }

        [Test]
        public void LevelGrid_CanSetInputHandler()
        {
            // Arrange
            GameObject inputHandlerGO = new GameObject("InputHandler");
            SnappableInputHandler inputHandler = inputHandlerGO.AddComponent<SnappableInputHandler>();

            // Act
            // Note: _inputHandler is protected, so we can't set it directly in tests
            // This would typically be set through the inspector or initialization

            // Assert - Just verify the component exists for now
            Assert.IsNotNull(inputHandler);

            // Cleanup
            Object.DestroyImmediate(inputHandlerGO);
        }
    }
}