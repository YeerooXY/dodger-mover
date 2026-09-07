using System.Collections;
using DodgerMover.Presentation;
using DodgerMover.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DodgerMover.Tests
{
    public sealed class ImpactIntegrationTests
    {
        private ImpactBootstrap root;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;

        [UnitySetUp]
        public IEnumerator BootScene()
        {
            // Batchmode has no focused Game view. Route virtual-device events into
            // the game explicitly, restoring the editor settings after every test.
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            yield return SceneManager.LoadSceneAsync("Assets/DodgerMover/Scenes/ImpactProof.unity", LoadSceneMode.Single);
            foreach (var candidate in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (candidate.TryGetComponent(out ImpactBootstrap bootstrap)) root = bootstrap;
            }
            Assert.That(root, Is.Not.Null);
            root.SampleDevices = false;
            root.Restart();
        }

        [TearDown]
        public void RestoreInputRouting()
        {
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        }

        [UnityTest]
        public IEnumerator SceneBootsWithCameraAndRunningSimulation()
        {
            Assert.That(root.Session.Simulation.Player.Health, Is.EqualTo(100));
            Assert.That(root.GetComponentInChildren<Camera>(), Is.Not.Null);
            yield return null;
            yield return null;
            Assert.That(root.Session.Simulation.Tick, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RestartIsImmediateAndDoesNotAccumulateSceneObjects()
        {
            int count = root.transform.childCount;
            root.Session.Simulation.Submit(new PlayerCommand(1, Buttons.Jump));
            for (int i = 0; i < 15; i++) yield return null;
            Assert.That(root.Session.Simulation.Player.X, Is.GreaterThan(-4));
            for (int i = 0; i < 50; i++) root.Restart();
            Assert.That(root.Session.Simulation.Player.X, Is.EqualTo(-4));
            Assert.That(root.Session.Simulation.Tick, Is.Zero);
            Assert.That(root.transform.childCount, Is.EqualTo(count));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void KeyboardBindingsProduceEdgesAndDoNotRepeatHeldAttacks()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                using (var input = new ImpactInput())
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space, Key.J));
                    InputSystem.Update();
                    PlayerCommand first = input.Sample();
                    Assert.That(first.Move, Is.EqualTo(1));
                    Assert.That(first.Pressed, Is.EqualTo(Buttons.Jump | Buttons.Light));
                    InputSystem.Update();
                    Assert.That(input.Sample().Pressed, Is.EqualTo(Buttons.None));
                }
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [Test]
        public void GamepadBindsMoveLauncherAndRestart()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                using (var input = new ImpactInput())
                {
                    var state = new GamepadState { leftStick = new Vector2(-1, 0) };
                    state = state.WithButton(GamepadButton.East).WithButton(GamepadButton.Start);
                    InputSystem.QueueStateEvent(gamepad, state);
                    InputSystem.Update();
                    Assert.That(input.Sample().Move, Is.EqualTo(-1));
                    Assert.That(input.Sample().Pressed, Is.EqualTo(Buttons.Launcher));
                    Assert.That(input.RestartPressed, Is.True);
                }
            }
            finally { InputSystem.RemoveDevice(gamepad); }
        }

        [UnityTest]
        public IEnumerator HeldMovementResumesAcrossRestartWithoutAnotherInputEdge()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            root.SampleDevices = true;
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                root.Restart();
                for (int i = 0; i < 5; i++) yield return null;
                Assert.That(root.Session.Simulation.Player.X, Is.GreaterThan(-4));
                Assert.That(root.Session.Simulation.Player.Move, Is.Null);
            }
            finally { root.SampleDevices = false; InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator OwnedLoopHasZeroSteadyAllocationsAfterWarmup()
        {
            for (int i = 0; i < 150; i++) yield return null;
            long peak = 0;
            double cpu = 0;
            for (int i = 0; i < 180; i++)
            {
                if (i % 60 == 0) root.Restart();
                root.Session.Simulation.Submit(new PlayerCommand(i % 60 < 30 ? 1 : 0, i % 60 == 27 ? Buttons.Light : Buttons.None));
                yield return null;
                peak = System.Math.Max(peak, root.LastOwnedBytes);
                cpu = System.Math.Max(cpu, root.LastOwnedMilliseconds);
            }
            Debug.Log("IMPACT_PROFILE: owned peak " + peak + " bytes/frame; peak CPU " + cpu.ToString("F3") + " ms (HUD formatting excluded).");
            Assert.That(peak, Is.Zero);
        }
    }
}
