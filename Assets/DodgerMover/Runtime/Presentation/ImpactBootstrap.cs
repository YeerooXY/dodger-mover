using System;
using System.Diagnostics;
using DodgerMover.Application;
using DodgerMover.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DodgerMover.Presentation
{
    public sealed class ImpactBootstrap : MonoBehaviour
    {
        [SerializeField] private Material spriteMaterial;
        [SerializeField] private Font interfaceFont;
        private ArenaArt art;
        private ImpactInput input;
        private FighterView playerView, enemyView;
        private ImpactFeedback feedback;
        private ImpactHud hud;
        private Camera viewCamera;
        private float viewTime, cameraX, smoothFrameMs;
        private int warmup;
        public ImpactSession Session { get; private set; }
        private bool sampleDevices = true;
        public bool SampleDevices
        {
            get => sampleDevices;
            set { sampleDevices = value; if (input != null) input.CaptureEvents = value; }
        }
        public double LastOwnedMilliseconds { get; private set; }
        public long LastOwnedBytes { get; private set; }
        public long PeakOwnedBytes { get; private set; }

        private void Awake()
        {
            QualitySettings.vSyncCount = 1;
            UnityEngine.Application.targetFrameRate = 60;
            Session = new ImpactSession(Time.realtimeSinceStartupAsDouble);
            input = new ImpactInput(Session);
            art = new ArenaArt(transform, spriteMaterial);
            art.BuildRoom();
            playerView = new FighterView(art, transform, "Player", ArenaArt.Teal, 10);
            enemyView = new FighterView(art, transform, "Training unit", ArenaArt.Orange, 5);
            feedback = new ImpactFeedback(art, transform);
            var cameraObject = new GameObject("Arena camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform, false);
            viewCamera = cameraObject.GetComponent<Camera>();
            viewCamera.orthographic = true; viewCamera.orthographicSize = 7.5f;
            viewCamera.clearFlags = CameraClearFlags.SolidColor; viewCamera.backgroundColor = ArenaArt.Ink;
            viewCamera.transform.position = new Vector3(0, 2.8f, -20);
            viewCamera.nearClipPlane = .1f; viewCamera.farClipPlane = 100;
            hud = new ImpactHud(transform, interfaceFont, Restart);
            var events = new GameObject("Input events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            RenderOwned(0);
        }

        private void Update()
        {
            if (SampleDevices && input.RestartPressed) Restart();
            if (SampleDevices && input.DebugPressed) hud.ToggleDiagnostics();
            if (SampleDevices && input.ExitPressed) UnityEngine.Application.Quit();
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            long started = Stopwatch.GetTimestamp();
            Session.AdvanceTo(Time.realtimeSinceStartupAsDouble);
            RenderOwned(Time.unscaledDeltaTime);
            LastOwnedMilliseconds = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
            LastOwnedBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
            if (++warmup > 120) PeakOwnedBytes = Math.Max(PeakOwnedBytes, LastOwnedBytes);
            smoothFrameMs = Mathf.Lerp(smoothFrameMs, Time.unscaledDeltaTime * 1000, .04f);
            hud.Render(Session.Simulation, input.UsingGamepad, Time.unscaledTime, smoothFrameMs, LastOwnedMilliseconds, LastOwnedBytes);
        }

        private void RenderOwned(float delta)
        {
            ImpactSimulation sim = Session.Simulation;
            if (sim.HitStopTicks == 0) viewTime += delta;
            playerView.Render(sim.Player, viewTime); enemyView.Render(sim.Enemy, viewTime);
            feedback.Render(sim, delta);
            float targetX = Mathf.Clamp(sim.Player.X * .16f, -1.5f, 1.5f);
            cameraX = Mathf.Lerp(cameraX, targetX, 1 - Mathf.Exp(-delta * 4));
            float impulse = Mathf.Sin(viewTime * 91) * feedback.CameraKick;
            viewCamera.transform.position = new Vector3(cameraX + impulse, 2.8f + Mathf.Abs(impulse) * .35f, -20);
            // Keep the whole room readable at narrower window aspect ratios.
            viewCamera.orthographicSize = Mathf.Max(7.5f, 13.1f / viewCamera.aspect);
        }

        public void Restart()
        {
            Session.Restart(); Session.ResetInputClock(Time.realtimeSinceStartupAsDouble);
            if (SampleDevices) input.ResynchronizeHeldMovement(Time.realtimeSinceStartupAsDouble);
            feedback.Reset(); cameraX = 0; viewTime = 0;
            RenderOwned(0);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (Session == null) return;
            Session.ResetInputClock(Time.realtimeSinceStartupAsDouble);
            if (focused && SampleDevices) input.ResynchronizeHeldMovement(Time.realtimeSinceStartupAsDouble);
        }

        private void OnDestroy() { input?.Dispose(); feedback?.Dispose(); art?.Dispose(); }
    }
}
