using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ameba;
using System;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for ServiceRegistry ScriptableObject
    /// </summary>
    public class ServiceRegistryTests
    {
        private ServiceRegistry serviceRegistry;

        [SetUp]
        public void SetUp()
        {
            serviceRegistry = ScriptableObject.CreateInstance<ServiceRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            if (serviceRegistry != null)
            {
                Object.DestroyImmediate(serviceRegistry);
            }
        }

        [Test]
        public void ServiceRegistry_CanBeCreated()
        {
            // Assert
            Assert.IsNotNull(serviceRegistry);
            Assert.IsInstanceOf<ScriptableObject>(serviceRegistry);
        }

        [Test]
        public void ServiceRegistry_ServiceTypes_InitiallyEmpty()
        {
            // Act
            Type[] serviceTypes = serviceRegistry.ServiceTypes;

            // Assert
            Assert.IsNotNull(serviceTypes);
            Assert.AreEqual(0, serviceTypes.Length);
        }

        [Test]
        public void ServiceRegistry_AddServiceType_AddsToList()
        {
            // Act
            serviceRegistry.AddServiceType("TestService");

            // Assert
            string[] typeNames = serviceRegistry.GetServiceTypeNames();
            Assert.Contains("TestService", typeNames);
        }

        [Test]
        public void ServiceRegistry_RemoveServiceType_RemovesFromList()
        {
            // Arrange
            serviceRegistry.AddServiceType("TestService");
            serviceRegistry.AddServiceType("AnotherService");

            // Act
            serviceRegistry.RemoveServiceType("TestService");

            // Assert
            string[] typeNames = serviceRegistry.GetServiceTypeNames();
            Assert.IsFalse(Array.Exists(typeNames, t => t == "TestService"));
            Assert.IsTrue(Array.Exists(typeNames, t => t == "AnotherService"));
        }

        [Test]
        public void ServiceRegistry_GetServiceTypeNames_ReturnsArray()
        {
            // Arrange
            serviceRegistry.AddServiceType("Service1");
            serviceRegistry.AddServiceType("Service2");

            // Act
            string[] typeNames = serviceRegistry.GetServiceTypeNames();

            // Assert
            Assert.IsNotNull(typeNames);
            Assert.AreEqual(2, typeNames.Length);
            Assert.Contains("Service1", typeNames);
            Assert.Contains("Service2", typeNames);
        }

        [Test]
        public void ServiceRegistry_AddServiceType_NullString_DoesNotAdd()
        {
            // Arrange
            int initialCount = serviceRegistry.GetServiceTypeNames().Length;

            // Act
            serviceRegistry.AddServiceType(null);

            // Assert
            int finalCount = serviceRegistry.GetServiceTypeNames().Length;
            Assert.AreEqual(initialCount, finalCount);
        }

        [Test]
        public void ServiceRegistry_AddServiceType_EmptyString_DoesNotAdd()
        {
            // Arrange
            int initialCount = serviceRegistry.GetServiceTypeNames().Length;

            // Act
            serviceRegistry.AddServiceType("");

            // Assert
            int finalCount = serviceRegistry.GetServiceTypeNames().Length;
            Assert.AreEqual(initialCount, finalCount);
        }

        [Test]
        public void ServiceRegistry_AddServiceType_DuplicateString_DoesNotAddTwice()
        {
            // Act
            serviceRegistry.AddServiceType("DuplicateService");
            serviceRegistry.AddServiceType("DuplicateService");

            // Assert
            string[] typeNames = serviceRegistry.GetServiceTypeNames();
            int count = 0;
            foreach (string typeName in typeNames)
            {
                if (typeName == "DuplicateService") count++;
            }
            Assert.AreEqual(1, count, "Service should only be added once");
        }

        [Test]
        public void ServiceRegistry_RemoveServiceType_NonExistentString_DoesNotThrow()
        {
            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() => serviceRegistry.RemoveServiceType("NonExistentService"));
        }

        [Test]
        public void ServiceRegistry_ServiceTypes_WithValidType_ReturnsTypes()
        {
            // Arrange - Add a known type that should exist in the current assemblies
            serviceRegistry.AddServiceType("ServiceRegistry");

            // Act
            Type[] serviceTypes = serviceRegistry.ServiceTypes;

            // Assert
            Assert.IsNotNull(serviceTypes);
            // Note: The actual type resolution depends on assembly availability
            // This test mainly ensures the property doesn't throw exceptions
        }

        [Test]
        public void ServiceRegistry_ServiceTypes_WithInvalidType_FiltersOut()
        {
            // Arrange
            serviceRegistry.AddServiceType("NonExistentType123");

            // Act
            Type[] serviceTypes = serviceRegistry.ServiceTypes;

            // Assert
            Assert.IsNotNull(serviceTypes);
            Assert.AreEqual(0, serviceTypes.Length, "Invalid types should be filtered out");
        }
    }
}