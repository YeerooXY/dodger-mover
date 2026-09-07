using DodgerMover.Combat;

namespace DodgerMover.Simulation
{
    // Only the simulation mutates a fighter. Action phase is derived, never independently set.
    public sealed class Fighter
    {
        internal Fighter(int id, int health, float x)
        {
            Id = id;
            Vitality = new CombatantState(health);
            Reset(x);
        }

        private CombatantState Vitality { get; }
        public int Id { get; }
        public int Health => Vitality.Health;
        public int MaxHealth => Vitality.MaxHealth;
        public int StunTicks => Vitality.HitStunTicksRemaining;
        public float X { get; internal set; }
        public float Y { get; internal set; }
        public float VelocityX { get; internal set; }
        public float VelocityY { get; internal set; }
        public bool Grounded { get; internal set; }
        public int Facing { get; internal set; }
        public MoveDefinition Move { get; private set; }
        public int MoveAge { get; private set; }
        public int Activation { get; private set; }
        public bool HitConnected { get; internal set; }
        internal int LastGroundedTick { get; set; }
        public ActionPhase Phase => Health == 0 ? ActionPhase.Defeated
            : StunTicks > 0 ? ActionPhase.HitStun
            : Move == null ? ActionPhase.Ready
            : MoveAge < Move.Startup ? ActionPhase.Startup
            : MoveAge < Move.Startup + Move.Active ? ActionPhase.Active : ActionPhase.Recovery;

        internal void Reset(float x)
        {
            Vitality.Reset();
            X = x; Y = 0; VelocityX = 0; VelocityY = 0;
            Grounded = true; Facing = 1; LastGroundedTick = 0;
            Move = null; MoveAge = 0; Activation = 0; HitConnected = false;
        }

        internal void BeginTick()
        {
            Vitality.AdvanceTick();
            if (Move != null && ++MoveAge >= Move.Total) CancelMove();
        }

        internal void StartMove(MoveDefinition move)
        {
            Move = move; MoveAge = 0; Activation++; HitConnected = false;
        }

        internal void CancelMove() { Move = null; MoveAge = 0; HitConnected = false; }

        internal void Receive(HitResolution hit)
        {
            if (Health == 0) return;
            Vitality.Apply(hit);
            VelocityX = hit.HorizontalVelocity; VelocityY = hit.VerticalVelocity;
            if (VelocityY > 0) Grounded = false;
            CancelMove();
        }
    }

    public readonly struct FighterSnapshot
    {
        public FighterSnapshot(Fighter fighter)
        {
            X = fighter.X; Y = fighter.Y; Vx = fighter.VelocityX; Vy = fighter.VelocityY;
            Health = fighter.Health; Stun = fighter.StunTicks; Facing = fighter.Facing;
            Grounded = fighter.Grounded; Phase = fighter.Phase; Move = fighter.Move?.Id ?? MoveId.None;
            MoveAge = fighter.MoveAge; Activation = fighter.Activation; Hit = fighter.HitConnected;
        }
        public readonly float X, Y, Vx, Vy;
        public readonly int Health, Stun, Facing, MoveAge, Activation;
        public readonly bool Grounded, Hit;
        public readonly ActionPhase Phase;
        public readonly MoveId Move;
    }
}
