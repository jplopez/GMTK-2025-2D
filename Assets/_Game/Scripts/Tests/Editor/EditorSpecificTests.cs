using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using GMTK;
using Ameba;

namespace GMTK.Tests.Editor
{
    /// <summary>
    /// Editor-specific tests for ScriptableObjects that may have editor functionality
    /// </summary>
    public class EditorScriptableObjectTests
    {
        #region ServiceRegistry Editor Tests

        [Test]
        public void ServiceRegistry_CreateAssetMenu_IsConfigured()
        {
            // Test that the CreateAssetMenu attribute is properly configured
            var serviceRegistryType = typeof(ServiceRegistry);
            var createAssetMenuAttributes = serviceRegistryType.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);
            
            Assert.AreEqual(1, createAssetMenuAttributes.Length, "ServiceRegistry should have CreateAssetMenu attribute");
            
            var createAssetMenu = (CreateAssetMenuAttribute)createAssetMenuAttributes[0];
            Assert.AreEqual("Ameba/Service Registry", createAssetMenu.menuName);
            Assert.AreEqual("ServiceRegistry", createAssetMenu.fileName);
        }

        #endregion

        #region GameElement Editor Tests

        [Test]
        public void GameElement_CreateAssetMenu_IsConfigured()
        {
            var gameElementType = typeof(GameElement);
            var createAssetMenuAttributes = gameElementType.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);
            
            Assert.AreEqual(1, createAssetMenuAttributes.Length, "GameElement should have CreateAssetMenu attribute");
            
            var createAssetMenu = (CreateAssetMenuAttribute)createAssetMenuAttributes[0];
            Assert.AreEqual("GMTK/Game Element", createAssetMenu.menuName);
            Assert.AreEqual("GameElement", createAssetMenu.fileName);
        }

        #endregion

        #region AmebaStateMachine Editor Tests

        [Test]
        public void AmebaStateMachine_CreateAssetMenu_IsConfigured()
        {
            var stateMachineType = typeof(AmebaStateMachine);
            var createAssetMenuAttributes = stateMachineType.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);
            
            Assert.AreEqual(1, createAssetMenuAttributes.Length, "AmebaStateMachine should have CreateAssetMenu attribute");
            
            var createAssetMenu = (CreateAssetMenuAttribute)createAssetMenuAttributes[0];
            Assert.AreEqual("Ameba/State Machine", createAssetMenu.menuName);
            Assert.AreEqual("AmebaStateMachine", createAssetMenu.fileName);
        }

        #endregion

        #region RuntimeRegistry Editor Tests

        [Test]
        public void RuntimeRegistry_CreateAssetMenu_IsConfigured()
        {
            var runtimeRegistryType = typeof(RuntimeRegistry);
            var createAssetMenuAttributes = runtimeRegistryType.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);
            
            Assert.AreEqual(1, createAssetMenuAttributes.Length, "RuntimeRegistry should have CreateAssetMenu attribute");
            
            var createAssetMenu = (CreateAssetMenuAttribute)createAssetMenuAttributes[0];
            Assert.AreEqual("Ameba/Runtime/GameObject Registry", createAssetMenu.menuName);
            Assert.AreEqual("RuntimeRegistry", createAssetMenu.fileName);
        }

        #endregion

        #region Component Menu Tests

        [Test]
        public void SceneController_AddComponentMenu_IsConfigured()
        {
            var sceneControllerType = typeof(SceneController);
            var addComponentMenuAttributes = sceneControllerType.GetCustomAttributes(typeof(AddComponentMenu), false);
            
            Assert.AreEqual(1, addComponentMenuAttributes.Length, "SceneController should have AddComponentMenu attribute");
            
            var addComponentMenu = (AddComponentMenu)addComponentMenuAttributes[0];
            Assert.AreEqual("GMTK/Scenes/Scene Controller", addComponentMenu.componentMenu);
        }

        [Test]
        public void GMTKBootstrap_AddComponentMenu_IsConfigured()
        {
            var bootstrapType = typeof(GMTKBootstrap);
            var addComponentMenuAttributes = bootstrapType.GetCustomAttributes(typeof(AddComponentMenu), false);
            
            Assert.AreEqual(1, addComponentMenuAttributes.Length, "GMTKBootstrap should have AddComponentMenu attribute");
            
            var addComponentMenu = (AddComponentMenu)addComponentMenuAttributes[0];
            Assert.AreEqual("GMTK/Bootstrap Component", addComponentMenu.componentMenu);
        }

        #endregion

        #region RequireComponent Tests

        [Test]
        public void Booster_RequiresCollider2D()
        {
            var boosterType = typeof(Booster);
            var requireComponentAttributes = boosterType.GetCustomAttributes(typeof(RequireComponent), false);
            
            Assert.AreEqual(1, requireComponentAttributes.Length, "Booster should require Collider2D");
            
            var requireComponent = (RequireComponent)requireComponentAttributes[0];
            Assert.AreEqual(typeof(Collider2D), requireComponent.m_Type0);
        }

        [Test]
        public void Checkpoint_RequiresCollider2D()
        {
            var checkpointType = typeof(Checkpoint);
            var requireComponentAttributes = checkpointType.GetCustomAttributes(typeof(RequireComponent), false);
            
            Assert.AreEqual(1, requireComponentAttributes.Length, "Checkpoint should require Collider2D");
            
            var requireComponent = (RequireComponent)requireComponentAttributes[0];
            Assert.AreEqual(typeof(Collider2D), requireComponent.m_Type0);
        }

        #endregion

        #region DefaultExecutionOrder Tests

        [Test]
        public void GMTKBootstrap_HasHighPriorityExecutionOrder()
        {
            var bootstrapType = typeof(GMTKBootstrap);
            var executionOrderAttributes = bootstrapType.GetCustomAttributes(typeof(DefaultExecutionOrder), false);
            
            Assert.AreEqual(1, executionOrderAttributes.Length, "GMTKBootstrap should have DefaultExecutionOrder attribute");
            
            var executionOrder = (DefaultExecutionOrder)executionOrderAttributes[0];
            Assert.AreEqual(-100, executionOrder.order, "GMTKBootstrap should have high priority execution order");
        }

        #endregion
    }

    /// <summary>
    /// Editor tests for MonoBehaviour initialization and setup
    /// </summary>
    public class EditorMonoBehaviourTests
    {
        [Test]
        public void MonoBehaviour_Classes_CanBeInstantiatedInEditor()
        {
            // Test that we can create instances of key MonoBehaviour classes
            // This ensures they don't have invalid dependencies that would break in editor

            // Create temporary GameObjects to test component addition
            var sceneControllerGO = new GameObject("TestSceneController");
            var bootstrapGO = new GameObject("TestBootstrap");
            var levelGridGO = new GameObject("TestLevelGrid");

            try
            {
                // Test that components can be added without throwing exceptions
                Assert.DoesNotThrow(() => sceneControllerGO.AddComponent<SceneController>());
                Assert.DoesNotThrow(() => bootstrapGO.AddComponent<GMTKBootstrap>());
                Assert.DoesNotThrow(() => levelGridGO.AddComponent<LevelGrid>());
            }
            finally
            {
                // Cleanup
                Object.DestroyImmediate(sceneControllerGO);
                Object.DestroyImmediate(bootstrapGO);
                Object.DestroyImmediate(levelGridGO);
            }
        }

        [Test]
        public void ScriptableObject_Classes_CanBeCreatedInEditor()
        {
            // Test that ScriptableObject classes can be instantiated in editor
            ServiceRegistry serviceRegistry = null;
            GameElement gameElement = null;
            AmebaStateMachine stateMachine = null;
            RuntimeRegistry runtimeRegistry = null;

            try
            {
                Assert.DoesNotThrow(() => serviceRegistry = ScriptableObject.CreateInstance<ServiceRegistry>());
                Assert.DoesNotThrow(() => gameElement = ScriptableObject.CreateInstance<GameElement>());
                Assert.DoesNotThrow(() => stateMachine = ScriptableObject.CreateInstance<AmebaStateMachine>());
                Assert.DoesNotThrow(() => runtimeRegistry = ScriptableObject.CreateInstance<RuntimeRegistry>());

                Assert.IsNotNull(serviceRegistry);
                Assert.IsNotNull(gameElement);
                Assert.IsNotNull(stateMachine);
                Assert.IsNotNull(runtimeRegistry);
            }
            finally
            {
                // Cleanup
                if (serviceRegistry != null) Object.DestroyImmediate(serviceRegistry);
                if (gameElement != null) Object.DestroyImmediate(gameElement);
                if (stateMachine != null) Object.DestroyImmediate(stateMachine);
                if (runtimeRegistry != null) Object.DestroyImmediate(runtimeRegistry);
            }
        }
    }
}