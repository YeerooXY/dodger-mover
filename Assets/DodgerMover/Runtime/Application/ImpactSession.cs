using System;
using DodgerMover.Simulation;

namespace DodgerMover.Application
{
    public sealed class ImpactSession
    {
        private double accumulator;
        private double inputEpoch, lastTime;
        private readonly TimedInput[] pending = new TimedInput[128];
        private int pendingCount;
        private float timelineMovement;

        private readonly struct TimedInput
        {
            public TimedInput(double time, float move, Buttons pressed, bool changesMovement)
            { Time = time; Move = move; Pressed = pressed; ChangesMovement = changesMovement; }
            public readonly double Time;
            public readonly float Move;
            public readonly Buttons Pressed;
            public readonly bool ChangesMovement;
        }

        public ImpactSession(double inputTime = 0) { inputEpoch = lastTime = inputTime; }
        public ImpactSimulation Simulation { get; } = new ImpactSimulation();
        public float Interpolation => (float)(accumulator * ImpactSimulation.TickRate);

        public void QueueMovement(double time, float move)
            => Queue(new TimedInput(time, new PlayerCommand(move).Move, Buttons.None, true));

        public void QueuePress(double time, Buttons buttons)
            => Queue(new TimedInput(time, 0, buttons, false));

        private void Queue(TimedInput input)
        {
            if (double.IsNaN(input.Time) || double.IsInfinity(input.Time))
                throw new ArgumentOutOfRangeException(nameof(input));
            if (pendingCount == pending.Length)
                throw new InvalidOperationException("Input event capacity exceeded before the next frame.");
            int index = pendingCount++;
            // Stable insertion supports out-of-order device delivery without changing equal-time order.
            while (index > 0 && pending[index - 1].Time > input.Time)
            { pending[index] = pending[index - 1]; index--; }
            pending[index] = input;
        }

        private void ApplyTimedInput()
        {
            int consumed = 0;
            Buttons presses = Buttons.None;
            double newestAttackTime = double.NegativeInfinity;
            const Buttons attacks = Buttons.Light | Buttons.Heavy | Buttons.Launcher;
            while (consumed < pendingCount)
            {
                TimedInput input = pending[consumed];
                int eventTick = (int)Math.Floor((input.Time - inputEpoch) * ImpactSimulation.TickRate + 1e-7);
                if (eventTick > Simulation.Tick) break;
                if (input.ChangesMovement) timelineMovement = input.Move;
                presses |= input.Pressed & Buttons.Jump;
                Buttons attack = input.Pressed & attacks;
                if (attack != Buttons.None)
                {
                    if (input.Time != newestAttackTime) presses &= ~attacks;
                    presses |= attack;
                    newestAttackTime = input.Time;
                }
                consumed++;
            }
            if (consumed == 0) return;
            Simulation.Submit(new PlayerCommand(timelineMovement, presses));
            pendingCount -= consumed;
            Array.Copy(pending, consumed, pending, 0, pendingCount);
        }

        public void AdvanceTo(double inputTime) => Advance(inputTime - lastTime);

        public void Advance(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            // A focus stall must not produce seconds of catch-up or queue stale held movement.
            lastTime += elapsedSeconds;
            if (elapsedSeconds > .1) inputEpoch += elapsedSeconds - .1;
            accumulator += Math.Min(elapsedSeconds, .1);
            const double step = 1.0 / ImpactSimulation.TickRate;
            while (accumulator + 1e-10 >= step)
            {
                ApplyTimedInput();
                Simulation.Step();
                accumulator = Math.Max(0, accumulator - step);
            }
        }

        public void ResetInputClock(double inputTime)
        {
            inputEpoch = inputTime - Simulation.Tick / (double)ImpactSimulation.TickRate - accumulator;
            lastTime = inputTime; pendingCount = 0; timelineMovement = 0;
            Simulation.Submit(new PlayerCommand(0)); Simulation.Buffer.Clear();
        }

        public void Restart()
        {
            accumulator = 0; Simulation.Restart(); ResetInputClock(lastTime);
        }
    }
}
