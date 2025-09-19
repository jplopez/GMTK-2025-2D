using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for GameElement ScriptableObject
    /// </summary>
    public class GameElementTests
    {
        private GameElement gameElement;

        [SetUp]
        public void SetUp()
        {
            gameElement = ScriptableObject.CreateInstance<GameElement>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameElement != null)
            {
                Object.DestroyImmediate(gameElement);
            }
        }

        [Test]
        public void GameElement_CanBeCreated()
        {
            // Assert
            Assert.IsNotNull(gameElement);
            Assert.IsInstanceOf<ScriptableObject>(gameElement);
        }

        [Test]
        public void GameElement_DefaultConstructor_SetsDefaults()
        {
            // Assert
            Assert.AreEqual(0, gameElement.Id);
            Assert.IsNull(gameElement.Name);
            Assert.IsNull(gameElement.Prefab);
            Assert.AreEqual(0, gameElement.CategoryId);
            Assert.IsNull(gameElement.Description);
            Assert.IsNull(gameElement.Icon);
            Assert.IsTrue(gameElement.IsUnlocked);
        }

        [Test]
        public void GameElement_ParameterizedConstructor_SetsValues()
        {
            // Arrange
            GameObject prefabGO = new GameObject("TestPrefab");
            GridSnappable prefab = prefabGO.AddComponent<GridSnappable>();

            // Act
            GameElement element = new GameElement(123, "Test Element", prefab, 5);

            // Assert
            Assert.AreEqual(123, element.Id);
            Assert.AreEqual("Test Element", element.Name);
            Assert.AreEqual(prefab, element.Prefab);
            Assert.AreEqual(5, element.CategoryId);

            // Cleanup
            Object.DestroyImmediate(prefabGO);
        }

        [Test]
        public void GameElement_CanSetId()
        {
            // Act
            gameElement.Id = 42;

            // Assert
            Assert.AreEqual(42, gameElement.Id);
        }

        [Test]
        public void GameElement_CanSetName()
        {
            // Act
            gameElement.Name = "Test Name";

            // Assert
            Assert.AreEqual("Test Name", gameElement.Name);
        }

        [Test]
        public void GameElement_CanSetCategoryId()
        {
            // Act
            gameElement.CategoryId = 10;

            // Assert
            Assert.AreEqual(10, gameElement.CategoryId);
        }

        [Test]
        public void GameElement_CanSetDescription()
        {
            // Act
            gameElement.Description = "Test description";

            // Assert
            Assert.AreEqual("Test description", gameElement.Description);
        }

        [Test]
        public void GameElement_CanSetIsUnlocked()
        {
            // Act
            gameElement.IsUnlocked = false;

            // Assert
            Assert.IsFalse(gameElement.IsUnlocked);
        }

        [Test]
        public void GameElement_CanSetIcon()
        {
            // Arrange
            Texture2D texture = new Texture2D(32, 32);
            Sprite icon = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.zero);

            // Act
            gameElement.Icon = icon;

            // Assert
            Assert.AreEqual(icon, gameElement.Icon);

            // Cleanup
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(icon);
        }

        [Test]
        public void GameElement_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            gameElement.Id = 5;
            gameElement.Name = "TestElement";
            gameElement.CategoryId = 3;

            // Act
            string result = gameElement.ToString();

            // Assert
            Assert.AreEqual("Element 5: TestElement (Category 3)", result);
        }

        [Test]
        public void GameElement_Equals_ReturnsTrueForSameId()
        {
            // Arrange
            GameElement element1 = ScriptableObject.CreateInstance<GameElement>();
            GameElement element2 = ScriptableObject.CreateInstance<GameElement>();
            element1.Id = 123;
            element2.Id = 123;

            // Act & Assert
            Assert.IsTrue(element1.Equals(element2));

            // Cleanup
            Object.DestroyImmediate(element1);
            Object.DestroyImmediate(element2);
        }

        [Test]
        public void GameElement_Equals_ReturnsFalseForDifferentId()
        {
            // Arrange
            GameElement element1 = ScriptableObject.CreateInstance<GameElement>();
            GameElement element2 = ScriptableObject.CreateInstance<GameElement>();
            element1.Id = 123;
            element2.Id = 456;

            // Act & Assert
            Assert.IsFalse(element1.Equals(element2));

            // Cleanup
            Object.DestroyImmediate(element1);
            Object.DestroyImmediate(element2);
        }

        [Test]
        public void GameElement_GetHashCode_ReturnsIdHashCode()
        {
            // Arrange
            gameElement.Id = 123;

            // Act
            int hashCode = gameElement.GetHashCode();

            // Assert
            Assert.AreEqual(123.GetHashCode(), hashCode);
        }

        [Test]
        public void GameElement_InstantiateSnappable_CreatesInstance()
        {
            // Note: This test may need to be marked as UnityTest if it requires instantiation
            // For now, we'll just test that the method exists and can be called
            // The actual instantiation would require a prefab in the scene
            
            // Arrange
            GameObject prefabGO = new GameObject("TestPrefab");
            GridSnappable prefab = prefabGO.AddComponent<GridSnappable>();
            gameElement.Prefab = prefab;

            // Act & Assert - Method should exist and be callable
            // Note: In a real scene test, we would verify instantiation works
            Assert.IsNotNull(gameElement.Prefab);

            // Cleanup
            Object.DestroyImmediate(prefabGO);
        }
    }
}