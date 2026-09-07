using System;
using DodgerMover.Application;
using DodgerMover.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DodgerMover.Presentation
{
    public sealed class ImpactInput : IDisposable
    {
        private readonly InputActionMap map = new InputActionMap("Impact");
        private readonly InputAction move, jump, light, heavy, launcher, restart, debug, exit;
        private readonly ImpactSession session;
        public bool CaptureEvents { get; set; } = true;

        public ImpactInput(ImpactSession session = null)
        {
            this.session = session;
            move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick/x").WithProcessor("axisDeadzone(min=0.18,max=0.95)");
            move.AddBinding("<Gamepad>/dpad/x");
            jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            jump.AddBinding("<Keyboard>/w"); jump.AddBinding("<Keyboard>/upArrow");
            light = Button("Light", "<Keyboard>/j", "<Gamepad>/buttonWest"); light.AddBinding("<Keyboard>/x");
            heavy = Button("Heavy", "<Keyboard>/k", "<Gamepad>/buttonNorth"); heavy.AddBinding("<Keyboard>/c");
            launcher = Button("Launcher", "<Keyboard>/l", "<Gamepad>/buttonEast"); launcher.AddBinding("<Keyboard>/v");
            restart = Button("Restart", "<Keyboard>/r", "<Gamepad>/start");
            debug = Button("Diagnostics", "<Keyboard>/f1", "<Gamepad>/select");
            exit = map.AddAction("Exit", InputActionType.Button, "<Keyboard>/escape");
            move.performed += OnMove; move.canceled += OnMove;
            jump.performed += OnJump; light.performed += OnLight;
            heavy.performed += OnHeavy; launcher.performed += OnLauncher;
            map.Enable();
        }

        private void OnMove(InputAction.CallbackContext context)
        { if (CaptureEvents) session?.QueueMovement(context.time, context.ReadValue<float>()); }
        private void OnJump(InputAction.CallbackContext context)
        { if (CaptureEvents) session?.QueuePress(context.time, Buttons.Jump); }
        private void OnLight(InputAction.CallbackContext context)
        { if (CaptureEvents) session?.QueuePress(context.time, Buttons.Light); }
        private void OnHeavy(InputAction.CallbackContext context)
        { if (CaptureEvents) session?.QueuePress(context.time, Buttons.Heavy); }
        private void OnLauncher(InputAction.CallbackContext context)
        { if (CaptureEvents) session?.QueuePress(context.time, Buttons.Launcher); }

        private InputAction Button(string name, string keyboard, string gamepad)
        {
            var action = map.AddAction(name, InputActionType.Button, keyboard);
            action.AddBinding(gamepad);
            return action;
        }

        public bool RestartPressed => restart.WasPressedThisFrame();
        public bool DebugPressed => debug.WasPressedThisFrame();
        public bool ExitPressed => exit.WasPressedThisFrame();
        public bool UsingGamepad => move.activeControl?.device is Gamepad ||
            jump.activeControl?.device is Gamepad || light.activeControl?.device is Gamepad;

        public void ResynchronizeHeldMovement(double time)
        {
            if (CaptureEvents) session?.QueueMovement(time, move.ReadValue<float>());
        }

        public PlayerCommand Sample()
        {
            Buttons pressed = Buttons.None;
            if (jump.WasPressedThisFrame()) pressed |= Buttons.Jump;
            if (light.WasPressedThisFrame()) pressed |= Buttons.Light;
            if (heavy.WasPressedThisFrame()) pressed |= Buttons.Heavy;
            if (launcher.WasPressedThisFrame()) pressed |= Buttons.Launcher;
            return new PlayerCommand(move.ReadValue<float>(), pressed);
        }

        public void Dispose() { CaptureEvents = false; map.Disable(); map.Dispose(); }
    }
}
