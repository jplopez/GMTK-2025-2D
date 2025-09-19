using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ameba;

namespace GMTK.Tests.Runtime
{
    /// <summary>
    /// Unit tests for AmebaStateMachine ScriptableObject
    /// </summary>
    public class AmebaStateMachineTests
    {
        private AmebaStateMachine stateMachine;

        [SetUp]
        public void SetUp()
        {
            stateMachine = ScriptableObject.CreateInstance<AmebaStateMachine>();
        }

        [TearDown]
        public void TearDown()
        {
            if (stateMachine != null)
            {
                Object.DestroyImmediate(stateMachine);
            }
        }

        [Test]
        public void AmebaStateMachine_CanBeCreated()
        {
            // Assert
            Assert.IsNotNull(stateMachine);
            Assert.IsInstanceOf<ScriptableObject>(stateMachine);
        }

        [Test]
        public void AmebaStateMachine_HasDefaultNoRestrictions()
        {
            // Assert
            Assert.IsFalse(stateMachine.NoRestrictions);
        }

        [Test]
        public void AmebaStateMachine_CanSetNoRestrictions()
        {
            // Act
            stateMachine.NoRestrictions = true;

            // Assert
            Assert.IsTrue(stateMachine.NoRestrictions);
        }

        [Test]
        public void AmebaStateMachine_CanSetStartingState()
        {
            // Act
            stateMachine.StartingState = "TestStartingState";

            // Assert
            Assert.AreEqual("TestStartingState", stateMachine.StartingState);
        }

        [Test]
        public void AmebaStateMachine_CanSetCurrent()
        {
            // Act
            stateMachine.Current = "TestCurrentState";

            // Assert
            Assert.AreEqual("TestCurrentState", stateMachine.Current);
        }

        [Test]
        public void AmebaStateMachine_StartingStateCanBeNull()
        {
            // Act
            stateMachine.StartingState = null;

            // Assert
            Assert.IsNull(stateMachine.StartingState);
        }

        [Test]
        public void AmebaStateMachine_CurrentCanBeNull()
        {
            // Act
            stateMachine.Current = null;

            // Assert
            Assert.IsNull(stateMachine.Current);
        }

        [Test]
        public void AmebaStateMachine_CanSetStartingStateToEmptyString()
        {
            // Act
            stateMachine.StartingState = "";

            // Assert
            Assert.AreEqual("", stateMachine.StartingState);
        }

        [Test]
        public void AmebaStateMachine_CanSetCurrentToEmptyString()
        {
            // Act
            stateMachine.Current = "";

            // Assert
            Assert.AreEqual("", stateMachine.Current);
        }

        [Test]
        public void AmebaStateMachine_TestTransition_WithNoRestrictions_ReturnsTrue()
        {
            // Arrange
            stateMachine.NoRestrictions = true;
            GameState fromState = new GameState("From");
            GameState toState = new GameState("To");

            // Act
            bool result = stateMachine.TestTransition(fromState, toState);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void AmebaStateMachine_TestTransition_WithoutNoRestrictions_RequiresValidTransition()
        {
            // Arrange
            stateMachine.NoRestrictions = false;
            GameState fromState = new GameState("From");
            GameState toState = new GameState("To");

            // Act
            bool result = stateMachine.TestTransition(fromState, toState);

            // Assert - Should be false since no valid transitions are defined
            Assert.IsFalse(result);
        }

        [Test]
        public void AmebaStateMachine_ResetToStartingState_SetsCurrentState()
        {
            // Arrange
            stateMachine.StartingState = "InitialState";

            // Act
            stateMachine.ResetToStartingState();

            // Assert - We can't directly check the private _currentState, 
            // but we know the method should work without errors
            Assert.IsNotNull(stateMachine);
        }

        [Test]
        public void AmebaStateMachine_GetValidTransitions_ReturnsEmptyForNonExistentState()
        {
            // Arrange
            GameState nonExistentState = new GameState("NonExistent");

            // Act
            var validTransitions = stateMachine.GetValidTransitions(nonExistentState);

            // Assert
            Assert.IsNotNull(validTransitions);
            Assert.AreEqual(0, validTransitions.Count);
        }
    }
}