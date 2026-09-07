using System;

namespace DodgerMover.Combat
{
    public sealed class CombatantState
    {
        public CombatantState(int maxHealth)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public int MaxHealth { get; }

        public int Health { get; private set; }

        public int HitStunTicksRemaining { get; private set; }

        public float HorizontalVelocity { get; private set; }

        public float VerticalVelocity { get; private set; }

        public bool IsDefeated => Health == 0;

        public void Reset()
        {
            Health = MaxHealth;
            HitStunTicksRemaining = 0;
            HorizontalVelocity = 0;
            VerticalVelocity = 0;
        }

        public void Apply(HitResolution hit)
        {
            if (IsDefeated)
            {
                return;
            }

            Health = Math.Max(0, Health - hit.Damage);
            HitStunTicksRemaining = Math.Max(HitStunTicksRemaining, hit.HitStunTicks);
            HorizontalVelocity = hit.HorizontalVelocity;
            VerticalVelocity = hit.VerticalVelocity;
            if (IsDefeated) HitStunTicksRemaining = 0;
        }

        public void AdvanceTick()
        {
            if (HitStunTicksRemaining > 0)
            {
                HitStunTicksRemaining--;
            }
        }
    }
}
