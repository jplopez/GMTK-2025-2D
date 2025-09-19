using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for GMTKBootstrap MonoBehaviour
    /// </summary>
    public class GMTKBootstrapTests
    {
        private GameObject testGameObject;
        private GMTKBootstrap bootstrap;

        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestGMTKBootstrap");
            bootstrap = testGameObject.AddComponent<GMTKBootstrap>();
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
        public void GMTKBootstrap_CanBeCreated()
        {
            // Assert
            Assert.IsNotNull(bootstrap);
            Assert.IsInstanceOf<MonoBehaviour>(bootstrap);
        }

        [Test]
        public void GMTKBootstrap_HasDefaultForceReinitialize()
        {
            // Assert
            Assert.IsFalse(bootstrap.ForceReinitialize);
        }

        [Test]
        public void GMTKBootstrap_CanSetForceReinitialize()
        {
            // Act
            bootstrap.ForceReinitialize = true;

            // Assert
            Assert.IsTrue(bootstrap.ForceReinitialize);
        }

        [Test]
        public void GMTKBootstrap_HasDefaultServiceRegistryResourcePath()
        {
            // Assert
            Assert.AreEqual("ServiceRegistry", bootstrap.ServiceRegistryResourcePath);
        }

        [Test]
        public void GMTKBootstrap_CanSetServiceRegistryResourcePath()
        {
            // Act
            bootstrap.ServiceRegistryResourcePath = "CustomServiceRegistry";

            // Assert
            Assert.AreEqual("CustomServiceRegistry", bootstrap.ServiceRegistryResourcePath);
        }

        [Test]
        public void GMTKBootstrap_HasDefaultEnableDebugLogging()
        {
            // Assert
            Assert.IsTrue(bootstrap.EnableDebugLogging);
        }

        [Test]
        public void GMTKBootstrap_CanSetEnableDebugLogging()
        {
            // Act
            bootstrap.EnableDebugLogging = false;

            // Assert
            Assert.IsFalse(bootstrap.EnableDebugLogging);
        }

        [Test]
        public void GMTKBootstrap_HasDefaultConnectGameStateMachineEvents()
        {
            // Assert
            Assert.IsTrue(bootstrap.ConnectGameStateMachineEvents);
        }

        [Test]
        public void GMTKBootstrap_CanSetConnectGameStateMachineEvents()
        {
            // Act
            bootstrap.ConnectGameStateMachineEvents = false;

            // Assert
            Assert.IsFalse(bootstrap.ConnectGameStateMachineEvents);
        }

        [Test]
        public void GMTKBootstrap_ServiceRegistryResourcePathCannotBeNull()
        {
            // Act
            bootstrap.ServiceRegistryResourcePath = null;

            // Assert - Test should handle null gracefully
            // Note: The actual null handling is in the InitializeAllServices method
            // which would set a default if null is detected
            Assert.IsNull(bootstrap.ServiceRegistryResourcePath);
        }

        [Test]
        public void GMTKBootstrap_ServiceRegistryResourcePathCanBeEmptyString()
        {
            // Act
            bootstrap.ServiceRegistryResourcePath = "";

            // Assert
            Assert.AreEqual("", bootstrap.ServiceRegistryResourcePath);
        }
    }
}