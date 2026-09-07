using System;
using DodgerMover.Combat;
using NUnit.Framework;

namespace DodgerMover.Tests
{
    public sealed class CombatResolverTests
    {
        [Test]
        public void Resolve_FlipsHorizontalKnockbackWithFacing()
        {
            var attack = new AttackDefinition("launcher", 12, 9, 4.5f, 7.0f);

            HitResolution right = HitResolver.Resolve(attack, attackerFacesRight: true);
            HitResolution left = HitResolver.Resolve(attack, attackerFacesRight: false);

            Assert.That(right.HorizontalVelocity, Is.EqualTo(4.5f));
            Assert.That(left.HorizontalVelocity, Is.EqualTo(-4.5f));
            Assert.That(left.VerticalVelocity, Is.EqualTo(7.0f));
        }

        [Test]
        public void Apply_ClampsHealthAndPreservesLongestHitStun()
        {
            var state = new CombatantState(maxHealth: 20);
            state.Apply(new HitResolution("jab", 3, 10, 1.0f, 0.0f));
            state.Apply(new HitResolution("tap", 2, 4, 0.5f, 0.0f));

            Assert.That(state.Health, Is.EqualTo(15));
            Assert.That(state.HitStunTicksRemaining, Is.EqualTo(10));

            state.Apply(new HitResolution("finisher", 99, 12, 8.0f, 3.0f));

            Assert.That(state.Health, Is.Zero);
            Assert.That(state.IsDefeated, Is.True);
        }

        [Test]
        public void AdvanceTick_NeverProducesNegativeHitStun()
        {
            var state = new CombatantState(maxHealth: 10);
            state.Apply(new HitResolution("jab", 1, 1, 0.0f, 0.0f));

            state.AdvanceTick();
            state.AdvanceTick();

            Assert.That(state.HitStunTicksRemaining, Is.Zero);
        }

        [Test]
        public void AttackDefinition_RejectsNonFiniteKnockback()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AttackDefinition("invalid", 1, 1, float.NaN, 0.0f));
        }

        [Test]
        public void HitResolution_RejectsNegativeDamage()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HitResolution("invalid", -1, 1, 0.0f, 0.0f));
        }

        [Test]
        public void Resolve_SaturatesDamageInsteadOfOverflowing()
        {
            var attack = new AttackDefinition("extreme", int.MaxValue, 1, 0.0f, 0.0f);

            HitResolution result = HitResolver.Resolve(attack, true, 100.0f);

            Assert.That(result.Damage, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void DefeatClearsStunIgnoresFurtherHitsAndResetRestoresVitals()
        {
            var state = new CombatantState(10);
            state.Apply(new HitResolution("finish", 12, 30, 4, 6));
            Assert.That(state.IsDefeated, Is.True);
            Assert.That(state.HitStunTicksRemaining, Is.Zero);
            state.Apply(new HitResolution("late", 2, 60, 90, 90));
            Assert.That(state.HorizontalVelocity, Is.EqualTo(4));
            state.Reset();
            Assert.That(state.Health, Is.EqualTo(10));
            Assert.That(state.IsDefeated, Is.False);
            Assert.That(state.HorizontalVelocity, Is.Zero);
            Assert.That(state.VerticalVelocity, Is.Zero);
        }
    }
}
