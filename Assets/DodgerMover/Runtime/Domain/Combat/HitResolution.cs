using System;

namespace DodgerMover.Combat
{
    public readonly struct HitResolution
    {
        public HitResolution(
            string attackId,
            int damage,
            int hitStunTicks,
            float horizontalVelocity,
            float verticalVelocity)
        {
            if (string.IsNullOrWhiteSpace(attackId))
            {
                throw new ArgumentException("A hit requires its source attack id.", nameof(attackId));
            }

            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            if (hitStunTicks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hitStunTicks));
            }

            if (!IsFinite(horizontalVelocity))
            {
                throw new ArgumentOutOfRangeException(nameof(horizontalVelocity));
            }

            if (!IsFinite(verticalVelocity))
            {
                throw new ArgumentOutOfRangeException(nameof(verticalVelocity));
            }

            AttackId = attackId;
            Damage = damage;
            HitStunTicks = hitStunTicks;
            HorizontalVelocity = horizontalVelocity;
            VerticalVelocity = verticalVelocity;
        }

        public string AttackId { get; }

        public int Damage { get; }

        public int HitStunTicks { get; }

        public float HorizontalVelocity { get; }

        public float VerticalVelocity { get; }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
