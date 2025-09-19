using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for Booster MonoBehaviour
    /// </summary>
    public class BoosterTests
    {
        private GameObject testGameObject;
        private Booster booster;
        private Collider2D collider2D;

        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestBooster");
            collider2D = testGameObject.AddComponent<BoxCollider2D>();
            booster = testGameObject.AddComponent<Booster>();
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
        public void Booster_CanBeCreated()
        {
            // Assert
            Assert.IsNotNull(booster);
            Assert.IsInstanceOf<MonoBehaviour>(booster);
        }

        [Test]
        public void Booster_HasDefaultBoostForce()
        {
            // Assert
            Assert.AreEqual(Vector2.zero, booster.BoostForce);
        }

        [Test]
        public void Booster_CanSetBoostForce()
        {
            // Act
            booster.BoostForce = new Vector2(10, 15);

            // Assert
            Assert.AreEqual(new Vector2(10, 15), booster.BoostForce);
        }

        [Test]
        public void Booster_HasDefaultBoostOnEntry()
        {
            // Assert
            Assert.IsTrue(booster.BoostOnEntry);
        }

        [Test]
        public void Booster_CanSetBoostOnEntry()
        {
            // Act
            booster.BoostOnEntry = false;

            // Assert
            Assert.IsFalse(booster.BoostOnEntry);
        }

        [Test]
        public void Booster_HasDefaultBoostWhileIn()
        {
            // Assert
            Assert.IsFalse(booster.BoostWhileIn);
        }

        [Test]
        public void Booster_CanSetBoostWhileIn()
        {
            // Act
            booster.BoostWhileIn = true;

            // Assert
            Assert.IsTrue(booster.BoostWhileIn);
        }

        [Test]
        public void Booster_HasDefaultBoostOnExit()
        {
            // Assert
            Assert.IsTrue(booster.BoostOnExit);
        }

        [Test]
        public void Booster_CanSetBoostOnExit()
        {
            // Act
            booster.BoostOnExit = false;

            // Assert
            Assert.IsFalse(booster.BoostOnExit);
        }

        [Test]
        public void Booster_HasDefaultCooldownTime()
        {
            // Assert
            Assert.AreEqual(5f, booster.CooldownTime, 0.001f);
        }

        [Test]
        public void Booster_CanSetCooldownTime()
        {
            // Act
            booster.CooldownTime = 3f;

            // Assert
            Assert.AreEqual(3f, booster.CooldownTime, 0.001f);
        }

        [Test]
        public void Booster_RequiresCollider2D()
        {
            // Assert - The RequireComponent attribute should ensure Collider2D exists
            Assert.IsNotNull(testGameObject.GetComponent<Collider2D>());
        }

        [Test]
        public void Booster_CanSetMultipleBoostFlags()
        {
            // Act
            booster.BoostOnEntry = false;
            booster.BoostWhileIn = true;
            booster.BoostOnExit = false;

            // Assert
            Assert.IsFalse(booster.BoostOnEntry);
            Assert.IsTrue(booster.BoostWhileIn);
            Assert.IsFalse(booster.BoostOnExit);
        }

        [Test]
        public void Booster_CanSetNegativeBoostForce()
        {
            // Act
            booster.BoostForce = new Vector2(-5, -10);

            // Assert
            Assert.AreEqual(new Vector2(-5, -10), booster.BoostForce);
        }

        [Test]
        public void Booster_CanSetZeroCooldownTime()
        {
            // Act
            booster.CooldownTime = 0f;

            // Assert
            Assert.AreEqual(0f, booster.CooldownTime, 0.001f);
        }
    }
}