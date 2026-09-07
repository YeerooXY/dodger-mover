using System;

namespace DodgerMover.Simulation
{
    [Flags]
    public enum Buttons { None = 0, Jump = 1, Light = 2, Heavy = 4, Launcher = 8 }

    public readonly struct PlayerCommand
    {
        public PlayerCommand(float move, Buttons pressed = Buttons.None)
        {
            if (float.IsNaN(move) || float.IsInfinity(move))
                throw new ArgumentOutOfRangeException(nameof(move));
            Move = Math.Max(-1f, Math.Min(1f, move));
            Pressed = pressed;
        }

        public float Move { get; }
        public Buttons Pressed { get; }
    }

    // One pending jump and one pending attack; newest attack wins. Expiry is exclusive.
    public sealed class CommandBuffer
    {
        public const int JumpLifetime = 6;
        public const int AttackLifetime = 9;
        private int jumpUntil = -1;
        private int attackUntil = -1;
        private Buttons attack;

        public bool HasJump(int tick) => tick < jumpUntil;
        public Buttons AttackAt(int tick) => tick < attackUntil ? attack : Buttons.None;

        public void Push(Buttons pressed, int tick)
        {
            if ((pressed & Buttons.Jump) != 0) jumpUntil = tick + JumpLifetime;
            Buttons next = (pressed & Buttons.Launcher) != 0 ? Buttons.Launcher
                : (pressed & Buttons.Heavy) != 0 ? Buttons.Heavy
                : (pressed & Buttons.Light) != 0 ? Buttons.Light : Buttons.None;
            if (next == Buttons.None) return;
            attack = next;
            attackUntil = tick + AttackLifetime;
        }

        public void ConsumeJump() => jumpUntil = -1;
        public void ConsumeAttack() { attack = Buttons.None; attackUntil = -1; }
        public void Clear() { ConsumeJump(); ConsumeAttack(); }
    }
}
