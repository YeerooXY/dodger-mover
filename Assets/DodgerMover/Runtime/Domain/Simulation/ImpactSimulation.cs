using System;
using DodgerMover.Combat;

namespace DodgerMover.Simulation
{
    public sealed class ImpactSimulation
    {
        public const int TickRate = 60;
        public const float Delta = 1f / TickRate;
        public const float LeftWall = -11.5f, RightWall = 11.5f;
        public const float PlatformLeft = 2.2f, PlatformRight = 6.8f, PlatformY = 2.25f;
        public const int CoyoteTicks = 6;
        private float movement;

        public ImpactSimulation()
        {
            Player = new Fighter(0, 100, -4f);
            Enemy = new Fighter(1, 160, 0f);
            Buffer = new CommandBuffer();
        }

        public Fighter Player { get; }
        public Fighter Enemy { get; }
        public CommandBuffer Buffer { get; }
        public int Tick { get; private set; }
        public int HitStopTicks { get; private set; }
        public int HitCount { get; private set; }
        public MoveId LastHit { get; private set; }
        public int LastDamage { get; private set; }
        public float ImpactX { get; private set; }
        public float ImpactY { get; private set; }
        public bool EnemyApproaches { get; set; } = true;

        public void Submit(PlayerCommand command)
        {
            movement = command.Move;
            Buffer.Push(command.Pressed, Tick);
        }

        public void Restart()
        {
            Player.Reset(-4f); Enemy.Reset(0f); Buffer.Clear();
            Tick = 0; HitStopTicks = 0; HitCount = 0; movement = 0;
            LastHit = MoveId.None; LastDamage = 0; ImpactX = 0; ImpactY = 0;
        }

        public void Step()
        {
            if (HitStopTicks > 0) { HitStopTicks--; Tick++; return; }
            Player.BeginTick(); Enemy.BeginTick();
            if (Player.Grounded) Player.LastGroundedTick = Tick;

            TryJump();
            TryAttack();
            MovePlayer();
            MoveEnemy();
            Integrate(Player); Integrate(Enemy);
            // Landing may consume a buffered jump on the very same tick.
            TryJump();
            ResolveContact();
            // A jump buffered on (or just before) contact may confirm now. Consume it
            // before hit-stop can age the command out of its six-tick lifetime.
            TryJump();
            Tick++;
        }

        private void TryJump()
        {
            if (!Buffer.HasJump(Tick)) return;
            bool launchCancel = Player.Move == MoveSet.Launcher && Player.HitConnected;
            if (Player.Phase != ActionPhase.Ready && !launchCancel) return;
            if (!Player.Grounded && Tick - Player.LastGroundedTick >= CoyoteTicks) return;
            if (launchCancel) Player.CancelMove();
            Player.VelocityY = 13.8f; Player.Grounded = false;
            Player.LastGroundedTick = Tick - CoyoteTicks;
            Buffer.ConsumeJump();
        }

        private void TryAttack()
        {
            Buttons attack = Buffer.AttackAt(Tick);
            if (attack == Buttons.None) return;
            MoveDefinition next = null;
            if (Player.Phase == ActionPhase.Ready)
            {
                if (!Player.Grounded)
                {
                    if (attack == Buttons.Light) next = MoveSet.AirLight;
                }
                else next = attack == Buttons.Heavy ? MoveSet.Heavy
                    : attack == Buttons.Launcher ? MoveSet.Launcher : MoveSet.Light1;
            }
            else if (attack == Buttons.Light && Player.Grounded && Player.HitConnected &&
                Player.Phase == ActionPhase.Recovery && Player.MoveAge < Player.Move.Total - 3)
            {
                if (Player.Move == MoveSet.Light1) next = MoveSet.Light2;
                else if (Player.Move == MoveSet.Light2) next = MoveSet.Light3;
            }
            if (next == null) return;
            if (Math.Abs(movement) > .1f) Player.Facing = movement > 0 ? 1 : -1;
            Player.StartMove(next);
            Buffer.ConsumeAttack();
        }

        private void MovePlayer()
        {
            float control = Player.Move == null ? 1f : .38f;
            float target = movement * 7.2f * control;
            Player.VelocityX = Approach(Player.VelocityX, target, Player.Grounded ? 1.25f : .65f);
            if (Player.Move == null && Math.Abs(movement) > .1f) Player.Facing = movement > 0 ? 1 : -1;
        }

        private void MoveEnemy()
        {
            if (Enemy.Phase == ActionPhase.HitStun || Enemy.Phase == ActionPhase.Defeated)
            {
                Enemy.VelocityX = Approach(Enemy.VelocityX, 0, Enemy.Grounded ? .24f : .015f);
                return;
            }
            float distance = Player.X - Enemy.X;
            Enemy.Facing = distance >= 0 ? 1 : -1;
            float target = EnemyApproaches && Math.Abs(distance) > 1.4f ? Enemy.Facing * 1.25f : 0;
            Enemy.VelocityX = Approach(Enemy.VelocityX, target, .15f);
        }

        private static void Integrate(Fighter body)
        {
            float previousY = body.Y;
            body.X = Math.Max(LeftWall, Math.Min(RightWall, body.X + body.VelocityX * Delta));
            if (body.X == LeftWall || body.X == RightWall) body.VelocityX = 0;
            body.VelocityY = Math.Max(-22f, body.VelocityY - 32f * Delta);
            body.Y += body.VelocityY * Delta;
            body.Grounded = false;
            // One-way platform: crossing from above only. Feet use a small support radius.
            bool onPlatform = body.X + .28f > PlatformLeft && body.X - .28f < PlatformRight;
            float surface = onPlatform && previousY >= PlatformY && body.Y <= PlatformY && body.VelocityY <= 0
                ? PlatformY : 0;
            if (body.Y <= surface && body.VelocityY <= 0)
            {
                body.Y = surface; body.VelocityY = 0; body.Grounded = true;
            }
        }

        private void ResolveContact()
        {
            if (Player.Phase != ActionPhase.Active || Player.HitConnected || Enemy.Health == 0) return;
            float forward = (Enemy.X - Player.X) * Player.Facing;
            float vertical = Enemy.Y - Player.Y;
            if (forward < -.15f || forward > Player.Move.Reach + .36f ||
                vertical > Player.Move.Top || vertical + 1.6f < -.15f) return;
            HitResolution hit = HitResolver.Resolve(Player.Move.Hit, Player.Facing > 0);
            Enemy.Receive(hit);
            Player.HitConnected = true;
            HitStopTicks = Player.Move.HitStop;
            HitCount++; LastHit = Player.Move.Id; LastDamage = hit.Damage;
            ImpactX = Enemy.X - Player.Facing * .25f; ImpactY = Enemy.Y + .85f;
        }

        private static float Approach(float value, float target, float amount)
            => value < target ? Math.Min(target, value + amount) : Math.Max(target, value - amount);
    }
}
