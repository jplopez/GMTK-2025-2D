using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ameba;
using GMTK;
using System;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for additional ScriptableObject classes
    /// </summary>
    public class AdditionalScriptableObjectTests
    {
        #region RuntimeRegistry Tests

        private RuntimeRegistry runtimeRegistry;
        private GameObject testPrefab;
        private RegistryEntry testEntry;

        [SetUp]
        public void SetUpRuntimeRegistry()
        {
            runtimeRegistry = ScriptableObject.CreateInstance<RuntimeRegistry>();
            testPrefab = new GameObject("TestPrefab");
            testEntry = new RegistryEntry { id = "test_prefab", Prefab = testPrefab };
        }

        [TearDown]
        public void TearDownRuntimeRegistry()
        {
            if (runtimeRegistry != null)
            {
                Object.DestroyImmediate(runtimeRegistry);
            }
            if (testPrefab != null)
            {
                Object.DestroyImmediate(testPrefab);
            }
        }

        [Test]
        public void RuntimeRegistry_CanBeCreated()
        {
            Assert.IsNotNull(runtimeRegistry);
            Assert.IsInstanceOf<ScriptableObject>(runtimeRegistry);
        }

        [Test]
        public void RuntimeRegistry_EntriesInitialized()
        {
            Assert.IsNotNull(runtimeRegistry.Entries);
        }

        [Test]
        public void RuntimeRegistry_GetPrefab_WithValidId_ReturnsPrefab()
        {
            // Arrange
            runtimeRegistry.Entries.Add(testEntry);

            // Act
            GameObject result = runtimeRegistry.GetPrefab("test_prefab");

            // Assert
            Assert.AreEqual(testPrefab, result);
        }

        [Test]
        public void RuntimeRegistry_GetPrefab_WithInvalidId_ReturnsNull()
        {
            // Act
            GameObject result = runtimeRegistry.GetPrefab("invalid_id");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void RuntimeRegistry_GetPrefab_WithNullId_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => runtimeRegistry.GetPrefab(null));
        }

        [Test]
        public void RuntimeRegistry_GetPrefab_WithEmptyId_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => runtimeRegistry.GetPrefab(""));
        }

        [Test]
        public void RuntimeRegistry_TryGetPrefab_WithValidId_ReturnsTrue()
        {
            // Arrange
            runtimeRegistry.Entries.Add(testEntry);

            // Act
            bool result = runtimeRegistry.TryGetPrefab("test_prefab", out GameObject prefab);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(testPrefab, prefab);
        }

        [Test]
        public void RuntimeRegistry_TryGetPrefab_WithInvalidId_ReturnsFalse()
        {
            // Act
            bool result = runtimeRegistry.TryGetPrefab("invalid_id", out GameObject prefab);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(prefab);
        }

        [Test]
        public void RuntimeRegistry_Contains_WithValidId_ReturnsTrue()
        {
            // Arrange
            runtimeRegistry.Entries.Add(testEntry);

            // Act
            bool result = runtimeRegistry.Contains("test_prefab");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void RuntimeRegistry_Contains_WithInvalidId_ReturnsFalse()
        {
            // Act
            bool result = runtimeRegistry.Contains("invalid_id");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void RuntimeRegistry_ContainsPrefab_WithValidPrefab_ReturnsTrue()
        {
            // Arrange
            runtimeRegistry.Entries.Add(testEntry);

            // Act
            bool result = runtimeRegistry.ContainsPrefab(testPrefab);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region RuntimeVariable Tests

        private RuntimeVariable runtimeVariable;

        [SetUp]
        public void SetUpRuntimeVariable()
        {
            runtimeVariable = ScriptableObject.CreateInstance<RuntimeVariable>();
        }

        [TearDown]
        public void TearDownRuntimeVariable()
        {
            if (runtimeVariable != null)
            {
                Object.DestroyImmediate(runtimeVariable);
            }
        }

        [Test]
        public void RuntimeVariable_CanBeCreated()
        {
            Assert.IsNotNull(runtimeVariable);
            Assert.IsInstanceOf<ScriptableObject>(runtimeVariable);
        }

        #endregion

        #region GameInventory Tests

        private GameInventory gameInventory;

        [SetUp]
        public void SetUpGameInventory()
        {
            gameInventory = ScriptableObject.CreateInstance<GameInventory>();
        }

        [TearDown]
        public void TearDownGameInventory()
        {
            if (gameInventory != null)
            {
                Object.DestroyImmediate(gameInventory);
            }
        }

        [Test]
        public void GameInventory_CanBeCreated()
        {
            Assert.IsNotNull(gameInventory);
            Assert.IsInstanceOf<ScriptableObject>(gameInventory);
        }

        #endregion

        #region LevelService Tests

        private LevelService levelService;

        [SetUp]
        public void SetUpLevelService()
        {
            levelService = ScriptableObject.CreateInstance<LevelService>();
        }

        [TearDown]
        public void TearDownLevelService()
        {
            if (levelService != null)
            {
                Object.DestroyImmediate(levelService);
            }
        }

        [Test]
        public void LevelService_CanBeCreated()
        {
            Assert.IsNotNull(levelService);
            Assert.IsInstanceOf<ScriptableObject>(levelService);
        }

        #endregion

        #region RuntimeMap Tests

        private RuntimeMap runtimeMap;

        [SetUp]
        public void SetUpRuntimeMap()
        {
            runtimeMap = ScriptableObject.CreateInstance<RuntimeMap>();
        }

        [TearDown]
        public void TearDownRuntimeMap()
        {
            if (runtimeMap != null)
            {
                Object.DestroyImmediate(runtimeMap);
            }
        }

        [Test]
        public void RuntimeMap_CanBeCreated()
        {
            Assert.IsNotNull(runtimeMap);
            Assert.IsInstanceOf<ScriptableObject>(runtimeMap);
        }

        #endregion
    }

    #region RegistryEntry Tests

    /// <summary>
    /// Tests for RegistryEntry class which implements IPoolable
    /// </summary>
    public class RegistryEntryTests
    {
        private RegistryEntry registryEntry;
        private GameObject testPrefab;

        [SetUp]
        public void SetUp()
        {
            registryEntry = new RegistryEntry();
            testPrefab = new GameObject("TestPrefab");
        }

        [TearDown]
        public void TearDown()
        {
            if (testPrefab != null)
            {
                Object.DestroyImmediate(testPrefab);
            }
        }

        [Test]
        public void RegistryEntry_CanBeCreated()
        {
            Assert.IsNotNull(registryEntry);
        }

        [Test]
        public void RegistryEntry_CanSetId()
        {
            registryEntry.id = "test_id";
            Assert.AreEqual("test_id", registryEntry.id);
        }

        [Test]
        public void RegistryEntry_CanSetPrefab()
        {
            registryEntry.Prefab = testPrefab;
            Assert.AreEqual(testPrefab, registryEntry.Prefab);
        }

        [Test]
        public void RegistryEntry_PrefabId_ReflectsId()
        {
            registryEntry.id = "prefab_id_test";
            Assert.AreEqual("prefab_id_test", registryEntry.PrefabId);

            registryEntry.PrefabId = "new_prefab_id";
            Assert.AreEqual("new_prefab_id", registryEntry.id);
        }

        [Test]
        public void RegistryEntry_HasDefaultPrewarmCount()
        {
            Assert.AreEqual(5, registryEntry.PrewarmCount);
        }

        [Test]
        public void RegistryEntry_CanSetPrewarmCount()
        {
            registryEntry.PrewarmCount = 10;
            Assert.AreEqual(10, registryEntry.PrewarmCount);
        }

        [Test]
        public void RegistryEntry_OnSpawn_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => registryEntry.OnSpawn());
        }

        [Test]
        public void RegistryEntry_OnReturn_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => registryEntry.OnReturn());
        }
    }

    #endregion
}