using System;

namespace DodgerMover.Combat
{
    public sealed class AttackDefinition
    {
        public AttackDefinition(
            string id,
            int damage,
            int hitStunTicks,
            float horizontalKnockback,
            float verticalKnockback)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("An attack requires a stable identifier.", nameof(id));
            }

            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            if (hitStunTicks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hitStunTicks));
            }

            if (!IsFinite(horizontalKnockback))
            {
                throw new ArgumentOutOfRangeException(nameof(horizontalKnockback));
            }

            if (!IsFinite(verticalKnockback))
            {
                throw new ArgumentOutOfRangeException(nameof(verticalKnockback));
            }

            Id = id;
            Damage = damage;
            HitStunTicks = hitStunTicks;
            HorizontalKnockback = horizontalKnockback;
            VerticalKnockback = verticalKnockback;
        }

        public string Id { get; }

        public int Damage { get; }

        public int HitStunTicks { get; }

        public float HorizontalKnockback { get; }

        public float VerticalKnockback { get; }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

