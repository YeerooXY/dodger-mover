using System;
using DodgerMover.Application;
using DodgerMover.Simulation;
using NUnit.Framework;

namespace DodgerMover.Tests
{
    public sealed class ImpactSimulationTests
    {
        private static void Step(ImpactSimulation sim, int count, float move = 0, Buttons press = Buttons.None)
        {
            for (int i = 0; i < count; i++)
            {
                sim.Submit(new PlayerCommand(move, i == 0 ? press : Buttons.None));
                sim.Step();
            }
        }

        private static ImpactSimulation InRange()
        {
            var sim = new ImpactSimulation { EnemyApproaches = false };
            Step(sim, 24, 1);
            Step(sim, 6);
            return sim;
        }

        [Test]
        public void JumpBufferExpiresOnExclusiveSixthTick()
        {
            var buffer = new CommandBuffer();
            buffer.Push(Buttons.Jump, 10);
            Assert.That(buffer.HasJump(15), Is.True);
            Assert.That(buffer.HasJump(16), Is.False);
        }

        [Test]
        public void AttackBufferExpiresOnExclusiveNinthTickAndNewestWins()
        {
            var buffer = new CommandBuffer();
            buffer.Push(Buttons.Light, 10); buffer.Push(Buttons.Heavy, 11);
            Assert.That(buffer.AttackAt(19), Is.EqualTo(Buttons.Heavy));
            Assert.That(buffer.AttackAt(20), Is.EqualTo(Buttons.None));
        }

        [Test]
        public void SimultaneousAttacksUseDocumentedPriority()
        {
            var sim = InRange();
            Step(sim, 1, 0, Buttons.Light | Buttons.Heavy | Buttons.Launcher);
            Assert.That(sim.Player.Move.Id, Is.EqualTo(MoveId.Launcher));
        }

        [Test]
        public void RunTurnAndWallRemainBounded()
        {
            var sim = new ImpactSimulation();
            Step(sim, 300, 1);
            Assert.That(sim.Player.X, Is.EqualTo(ImpactSimulation.RightWall));
            Step(sim, 600, -1);
            Assert.That(sim.Player.X, Is.EqualTo(ImpactSimulation.LeftWall));
            Assert.That(sim.Player.Facing, Is.EqualTo(-1));
        }

        [Test]
        public void JumpDoesNotDoubleJumpAndReturnsToFloor()
        {
            var sim = new ImpactSimulation();
            Step(sim, 8, 0, Buttons.Jump);
            float before = sim.Player.VelocityY;
            Step(sim, 1, 0, Buttons.Jump);
            Assert.That(sim.Player.VelocityY, Is.LessThan(before));
            Step(sim, 90);
            Assert.That(sim.Player.Grounded, Is.True);
            Assert.That(sim.Player.Y, Is.Zero);
        }

        [Test]
        public void JumpBufferedImmediatelyBeforeLandingFiresOnLanding()
        {
            var sim = new ImpactSimulation();
            Step(sim, 1, 0, Buttons.Jump);
            for (int i = 0; i < 100 && !(sim.Player.VelocityY < 0 && sim.Player.Y < .4f); i++) Step(sim, 1);
            Step(sim, 4, 0, Buttons.Jump);
            Assert.That(sim.Player.VelocityY, Is.GreaterThan(0));
            Assert.That(sim.Player.Grounded, Is.False);
        }

        private static ImpactSimulation OnPlatform()
        {
            var sim = new ImpactSimulation();
            Step(sim, 47, 1);
            Step(sim, 31, 1, Buttons.Jump);
            Step(sim, 26);
            Assert.That(sim.Player.Y, Is.EqualTo(ImpactSimulation.PlatformY), "setup must land on the raised platform");
            return sim;
        }

        [Test]
        public void OneWayPlatformAcceptsDescendingFeet()
        {
            var sim = OnPlatform();
            Assert.That(sim.Player.Grounded, Is.True);
            Step(sim, 1, 0, Buttons.Jump);
            Assert.That(sim.Player.Y, Is.GreaterThan(ImpactSimulation.PlatformY));
        }

        [TestCase(2, true)]
        [TestCase(7, false)]
        public void CoyoteTimeWorksOnlyInsideGraceWindow(int delay, bool shouldJump)
        {
            var sim = OnPlatform();
            for (int i = 0; i < 100 && sim.Player.Grounded; i++) Step(sim, 1, 1);
            Step(sim, delay - 1, 1);
            Step(sim, 1, 1, Buttons.Jump);
            Assert.That(sim.Player.VelocityY > 0, Is.EqualTo(shouldJump));
        }

        [TestCase(Buttons.Light, 8)]
        [TestCase(Buttons.Heavy, 26)]
        [TestCase(Buttons.Launcher, 12)]
        public void EveryActivationHitsOnlyOnce(Buttons button, int expectedDamage)
        {
            var sim = InRange();
            Step(sim, 70, 0, button);
            Assert.That(sim.Enemy.Health, Is.EqualTo(160 - expectedDamage));
            Assert.That(sim.HitCount, Is.EqualTo(1));
            Assert.That(sim.Player.Phase, Is.EqualTo(ActionPhase.Ready));
        }

        [Test]
        public void LightRecoveryChainsThreeDistinctMoves()
        {
            var sim = InRange();
            Step(sim, 1, 1, Buttons.Light);
            for (int chain = 0; chain < 2; chain++)
            {
                for (int i = 0; i < 50 && !(sim.Player.Phase == ActionPhase.Recovery && sim.Player.HitConnected); i++) Step(sim, 1, 1);
                Step(sim, 1, 1, Buttons.Light);
                Assert.That(sim.Player.Move.Id, Is.EqualTo(chain == 0 ? MoveId.Light2 : MoveId.Light3));
            }
            Step(sim, 35, 1);
            Assert.That(sim.HitCount, Is.EqualTo(3));
            Assert.That(sim.Enemy.Health, Is.EqualTo(126));
        }

        [Test]
        public void EarlyHeavyBufferExpiresWithoutSkippingRecovery()
        {
            var sim = InRange();
            Step(sim, 1, 0, Buttons.Heavy);
            Step(sim, 10, 0, Buttons.Light | Buttons.Jump);
            Assert.That(sim.Player.Move.Id, Is.EqualTo(MoveId.Heavy));
            Assert.That(sim.Player.Grounded, Is.True);
            Step(sim, 70);
            Assert.That(sim.Player.Activation, Is.EqualTo(1));
        }

        [Test]
        public void ConfirmedLauncherCanJumpCancelIntoAirFollowUp()
        {
            var sim = InRange();
            Step(sim, 1, 1, Buttons.Launcher);
            for (int i = 0; i < 40 && sim.HitCount == 0; i++) Step(sim, 1, 1);
            Assert.That(sim.LastHit, Is.EqualTo(MoveId.Launcher));
            Step(sim, 7, 1, Buttons.Jump);
            Assert.That(sim.Player.Grounded, Is.False);
            Step(sim, 16, 1, Buttons.Light);
            Assert.That(sim.LastHit, Is.EqualTo(MoveId.AirLight));
            Assert.That(sim.HitCount, Is.EqualTo(2));
        }

        [Test]
        public void WhiffedLauncherCannotJumpCancel()
        {
            var sim = new ImpactSimulation { EnemyApproaches = false };
            Step(sim, 10, 0, Buttons.Launcher);
            Step(sim, 5, 0, Buttons.Jump);
            Assert.That(sim.Player.Grounded, Is.True);
            Assert.That(sim.Player.Move.Id, Is.EqualTo(MoveId.Launcher));
        }

        [TestCase(7)]
        [TestCase(8)]
        public void LauncherJumpBufferedBeforeOrOnContactSurvivesHitStop(int pressAge)
        {
            var sim = InRange();
            Step(sim, 1, 0, Buttons.Launcher);
            Step(sim, pressAge - 1);
            Step(sim, 2, 0, Buttons.Jump);
            Assert.That(sim.HitCount, Is.EqualTo(1));
            Assert.That(sim.Player.Grounded, Is.False);
            Assert.That(sim.Player.Move, Is.Null);
            Assert.That(sim.Player.VelocityY, Is.GreaterThan(0));
        }

        [Test]
        public void HitStopFreezesBodiesAndAttackAgeWhileInputClockAdvances()
        {
            var sim = InRange();
            Step(sim, 1, 0, Buttons.Light);
            while (sim.HitCount == 0) Step(sim, 1);
            var player = new FighterSnapshot(sim.Player); var enemy = new FighterSnapshot(sim.Enemy);
            int tick = sim.Tick;
            Step(sim, sim.HitStopTicks, 1);
            Assert.That(new FighterSnapshot(sim.Player), Is.EqualTo(player));
            Assert.That(new FighterSnapshot(sim.Enemy), Is.EqualTo(enemy));
            Assert.That(sim.Tick, Is.GreaterThan(tick));
        }

        [Test]
        public void RestartRestoresEntireEncounterAndClearsPendingInput()
        {
            var sim = InRange();
            Step(sim, 20, 1, Buttons.Launcher); sim.Submit(new PlayerCommand(1, Buttons.Jump));
            sim.Restart();
            var fresh = new ImpactSimulation();
            Assert.That(new FighterSnapshot(sim.Player), Is.EqualTo(new FighterSnapshot(fresh.Player)));
            Assert.That(new FighterSnapshot(sim.Enemy), Is.EqualTo(new FighterSnapshot(fresh.Enemy)));
            Assert.That(sim.Tick, Is.Zero); Assert.That(sim.HitCount, Is.Zero); Assert.That(sim.HitStopTicks, Is.Zero);
            Assert.That(sim.Buffer.HasJump(0), Is.False);
        }

        // A short timestamped acceptance stream retained as a regression fixture.
        private static PlayerCommand ReplayCommand(int tick)
        {
            Buttons button = tick == 24 || tick == 38 || tick == 50 ? Buttons.Light
                : tick == 150 ? Buttons.Launcher : tick == 165 ? Buttons.Jump
                : tick == 174 ? Buttons.Light : tick % 100 == 70 ? Buttons.Heavy : Buttons.None;
            return new PlayerCommand(tick < 60 ? 1 : tick > 260 && tick < 310 ? -1 : 0, button);
        }

        [Test]
        public void RecordedCommandsProduceIdenticalSnapshotsOnEveryTick()
        {
            var first = new ImpactSimulation(); var second = new ImpactSimulation();
            for (int tick = 0; tick < 600; tick++)
            {
                first.Submit(ReplayCommand(tick)); second.Submit(ReplayCommand(tick));
                first.Step(); second.Step();
                Assert.That(new FighterSnapshot(first.Player), Is.EqualTo(new FighterSnapshot(second.Player)), "player tick " + tick);
                Assert.That(new FighterSnapshot(first.Enemy), Is.EqualTo(new FighterSnapshot(second.Enemy)), "enemy tick " + tick);
                Assert.That(first.HitStopTicks, Is.EqualTo(second.HitStopTicks));
                Assert.That(first.HitCount, Is.EqualTo(second.HitCount));
            }
            Assert.That(first.HitCount, Is.GreaterThan(0), "replay must exercise combat");
        }

        [Test]
        public void RenderFramePartitionDoesNotChangeSimulationResults()
        {
            var thirty = new ImpactSession(); var high = new ImpactSession();
            thirty.Simulation.Submit(new PlayerCommand(1, Buttons.Jump));
            high.Simulation.Submit(new PlayerCommand(1, Buttons.Jump));
            for (int frame = 0; frame < 60; frame++) thirty.Advance(1.0 / 30);
            for (int frame = 0; frame < 288; frame++) high.Advance(1.0 / 144);
            Assert.That(thirty.Simulation.Tick, Is.EqualTo(120));
            Assert.That(high.Simulation.Tick, Is.EqualTo(120));
            Assert.That(new FighterSnapshot(high.Simulation.Player), Is.EqualTo(new FighterSnapshot(thirty.Simulation.Player)));
        }

        [Test]
        public void InputSurvivesFramesWithoutSimulationTicks()
        {
            var session = new ImpactSession();
            session.Simulation.Submit(new PlayerCommand(0, Buttons.Jump));
            session.Advance(.001);
            session.Simulation.Submit(new PlayerCommand(0));
            session.Advance(.017);
            Assert.That(session.Simulation.Player.VelocityY, Is.GreaterThan(0));
        }

        private static ImpactSimulation TimedReplay(double frameSeconds, bool hitch)
        {
            var session = new ImpactSession();
            double[] times = { .025, .063, .421, .435, .711, .899, .917, 1.21, 1.51, 1.8, 1.96 };
            Buttons[] buttons = { Buttons.Jump, Buttons.None, Buttons.None, Buttons.Light, Buttons.Launcher,
                Buttons.Jump, Buttons.Light, Buttons.None, Buttons.Heavy, Buttons.None, Buttons.Jump };
            float[] moves = { 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0 };
            int index = 0, frame = 0;
            double time = 0;
            while (time < 2 - 1e-10)
            {
                time = Math.Min(2, time + (hitch && frame++ == 12 ? 1.0 / 12 : frameSeconds));
                // Deliver only events the rendered frame has actually received, retaining device times.
                while (index < times.Length && times[index] <= time)
                {
                    if (buttons[index] == Buttons.None) session.QueueMovement(times[index], moves[index]);
                    else session.QueuePress(times[index], buttons[index]);
                    index++;
                }
                session.AdvanceTo(time);
            }
            return session.Simulation;
        }

        [TestCase(30, false)]
        [TestCase(60, false)]
        [TestCase(144, false)]
        [TestCase(60, true)]
        public void MidFrameDeviceEventsRemainIdenticalAcrossRenderRatesAndHitch(int fps, bool hitch)
        {
            ImpactSimulation expected = TimedReplay(1.0 / 120, false);
            ImpactSimulation actual = TimedReplay(1.0 / fps, hitch);
            Assert.That(actual.Tick, Is.EqualTo(120));
            Assert.That(new FighterSnapshot(actual.Player), Is.EqualTo(new FighterSnapshot(expected.Player)));
            Assert.That(new FighterSnapshot(actual.Enemy), Is.EqualTo(new FighterSnapshot(expected.Enemy)));
            Assert.That(actual.HitCount, Is.EqualTo(expected.HitCount));
        }

        [Test]
        public void TimestampQueueIsStableAndAllocationFreeAfterWarmup()
        {
            var session = new ImpactSession();
            session.QueueMovement(.009, 1); session.QueuePress(.008, Buttons.Jump); session.Advance(.1);
            session.Restart();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 600; i++)
            {
                double time = .1 + i / 60.0;
                session.QueueMovement(time, i % 100 < 50 ? 1 : -1);
                if (i % 30 == 0) session.QueuePress(time + .001, Buttons.Light);
                session.Advance(1.0 / 60);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LatestDistinctTimeAttackWinsRegardlessOfDeliveryOrder(bool reverse)
        {
            var session = new ImpactSession();
            if (reverse) session.QueuePress(.010, Buttons.Light);
            session.QueuePress(.001, Buttons.Launcher | Buttons.Jump);
            if (!reverse) session.QueuePress(.010, Buttons.Light);
            session.Advance(1.0 / 60);
            Assert.That(session.Simulation.Player.Grounded, Is.False, "jump is independent of the attack slot");
            Assert.That(session.Simulation.Player.Move.Id, Is.EqualTo(MoveId.AirLight));
        }

        [Test]
        public void EqualTimeAttacksRetainSimultaneousPriority()
        {
            var session = new ImpactSession();
            session.QueuePress(.001, Buttons.Launcher);
            session.QueuePress(.001, Buttons.Light);
            session.Advance(1.0 / 60);
            Assert.That(session.Simulation.Player.Move.Id, Is.EqualTo(MoveId.Launcher));
        }

        [Test]
        public void WarmCombatReplayAndRestartAllocateNoManagedMemory()
        {
            var sim = new ImpactSimulation();
            for (int i = 0; i < 600; i++) { sim.Submit(ReplayCommand(i)); sim.Step(); }
            sim.Restart();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int pass = 0; pass < 10; pass++)
            {
                for (int tick = 0; tick < 600; tick++) { sim.Submit(ReplayCommand(tick)); sim.Step(); }
                sim.Restart();
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        [Test]
        public void InvalidFrameTimeAndInputAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerCommand(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpactSession().Advance(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpactSession().Advance(-1));
        }
    }
}
