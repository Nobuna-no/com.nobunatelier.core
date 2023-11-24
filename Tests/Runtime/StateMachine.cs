using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NobunAtelier;
using UnityEngine.SceneManagement;
using NUnit.Framework.Internal;
using UnityEditorInternal;
using NobunAtelier.Tests;

namespace NobunAtelier.Core.Tests
{
    public class StateMachine
    {
        //[Test]
        //public void SetState_ChangesActiveStateDefinition()
        //{
        //}

        [UnityTest]
        public IEnumerator GameState_FlowTest()
        {
            // Load the test scene (replace "TestScene" with the name of your scene)
            SceneManager.LoadScene("test-00-statemachine", LoadSceneMode.Additive);
            yield return new WaitForSeconds(1); // Wait for one second

            // Get the state machine from the scene
            // var stateMachine = GameObject.FindObjectOfType<StateMachineComponent<MyStateDefinition, MyDataCollection>>();

            // Store the initial state
            // var initialState = stateMachine.CurrentStateDefinition;

            // Act: Simulate conditions for a state change here, this will depend on your game
            // ...
            while (PlayModeTestLogger.Instance.IsTestRunning)
            {
                yield return new WaitForSeconds(1); // Wait for state transition
            }

            Assert.IsTrue(PlayModeTestLogger.IsTestSuccessful());

            // Assert: Check that the state has changed
            // Assert.AreNotEqual(initialState, stateMachine.CurrentStateDefinition);
        }
    }
}
