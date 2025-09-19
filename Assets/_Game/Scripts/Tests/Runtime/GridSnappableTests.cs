using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for GridSnappable MonoBehaviour
    /// </summary>
    public class GridSnappableTests
    {
        private GameObject testGameObject;
        private GridSnappable gridSnappable;

        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestGridSnappable");
            gridSnappable = testGameObject.AddComponent<GridSnappable>();
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
        public void GridSnappable_CanBeCreated()
        {
            // Assert
            Assert.IsNotNull(gridSnappable);
            Assert.IsInstanceOf<MonoBehaviour>(gridSnappable);
        }

        [Test]
        public void GridSnappable_HasDefaultDraggable()
        {
            // Assert
            Assert.IsTrue(gridSnappable.Draggable);
        }

        [Test]
        public void GridSnappable_CanSetDraggable()
        {
            // Act
            gridSnappable.Draggable = false;

            // Assert
            Assert.IsFalse(gridSnappable.Draggable);
        }

        [Test]
        public void GridSnappable_HasDefaultBehaviourDelegate()
        {
            // Assert
            Assert.AreEqual(GridSnappable.BehaviourDelegateType.None, gridSnappable.BehaviourDelegate);
        }

        [Test]
        public void GridSnappable_CanSetBehaviourDelegate()
        {
            // Act
            gridSnappable.BehaviourDelegate = GridSnappable.BehaviourDelegateType.Components;

            // Assert
            Assert.AreEqual(GridSnappable.BehaviourDelegateType.Components, gridSnappable.BehaviourDelegate);
        }

        [Test]
        public void GridSnappable_HasDefaultFlippable()
        {
            // Assert
            Assert.IsFalse(gridSnappable.Flippable);
        }

        [Test]
        public void GridSnappable_CanSetFlippable()
        {
            // Act
            gridSnappable.Flippable = true;

            // Assert
            Assert.IsTrue(gridSnappable.Flippable);
        }

        [Test]
        public void GridSnappable_HasDefaultCanRotate()
        {
            // Assert
            Assert.IsFalse(gridSnappable.CanRotate);
        }

        [Test]
        public void GridSnappable_CanSetCanRotate()
        {
            // Act
            gridSnappable.CanRotate = true;

            // Assert
            Assert.IsTrue(gridSnappable.CanRotate);
        }

        [Test]
        public void GridSnappable_CanSetSnapTransform()
        {
            // Arrange
            GameObject snapTransformGO = new GameObject("SnapTransform");
            Transform snapTransform = snapTransformGO.transform;

            // Act
            gridSnappable.SnapTransform = snapTransform;

            // Assert
            Assert.AreEqual(snapTransform, gridSnappable.SnapTransform);

            // Cleanup
            Object.DestroyImmediate(snapTransformGO);
        }

        [Test]
        public void GridSnappable_CanSetModel()
        {
            // Arrange
            GameObject modelGO = new GameObject("Model");
            Transform model = modelGO.transform;

            // Act
            gridSnappable.Model = model;

            // Assert
            Assert.AreEqual(model, gridSnappable.Model);

            // Cleanup
            Object.DestroyImmediate(modelGO);
        }

        [Test]
        public void GridSnappable_CanSetHighlightModel()
        {
            // Arrange
            GameObject highlightModel = new GameObject("HighlightModel");

            // Act
            gridSnappable.HighlightModel = highlightModel;

            // Assert
            Assert.AreEqual(highlightModel, gridSnappable.HighlightModel);

            // Cleanup
            Object.DestroyImmediate(highlightModel);
        }

        [Test]
        public void GridSnappable_CanSetPointerFeedbacks()
        {
            // Arrange
            GameObject pointerOnFeedback = new GameObject("PointerOnFeedback");
            GameObject pointerOutFeedback = new GameObject("PointerOutFeedback");

            // Act
            gridSnappable.PointerOnFeedback = pointerOnFeedback;
            gridSnappable.PointerOutFeedback = pointerOutFeedback;

            // Assert
            Assert.AreEqual(pointerOnFeedback, gridSnappable.PointerOnFeedback);
            Assert.AreEqual(pointerOutFeedback, gridSnappable.PointerOutFeedback);

            // Cleanup
            Object.DestroyImmediate(pointerOnFeedback);
            Object.DestroyImmediate(pointerOutFeedback);
        }

        [Test]
        public void GridSnappable_CanSetMultipleProperties()
        {
            // Act
            gridSnappable.Draggable = false;
            gridSnappable.Flippable = true;
            gridSnappable.CanRotate = true;
            gridSnappable.BehaviourDelegate = GridSnappable.BehaviourDelegateType.Components;

            // Assert
            Assert.IsFalse(gridSnappable.Draggable);
            Assert.IsTrue(gridSnappable.Flippable);
            Assert.IsTrue(gridSnappable.CanRotate);
            Assert.AreEqual(GridSnappable.BehaviourDelegateType.Components, gridSnappable.BehaviourDelegate);
        }
    }
}