using System;

namespace DodgerMover.Combat
{
    public static class HitResolver
    {
        public static HitResolution Resolve(
            AttackDefinition attack,
            bool attackerFacesRight,
            float damageScale = 1.0f)
        {
            if (attack == null)
            {
                throw new ArgumentNullException(nameof(attack));
            }

            if (float.IsNaN(damageScale) ||
                float.IsInfinity(damageScale) ||
                damageScale < 0.0f ||
                damageScale > 100.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(damageScale));
            }

            double scaledDamage = attack.Damage * (double)damageScale;
            int damage = scaledDamage >= int.MaxValue
                ? int.MaxValue
                : (int)Math.Round(scaledDamage, MidpointRounding.AwayFromZero);
            float direction = attackerFacesRight ? 1.0f : -1.0f;

            return new HitResolution(
                attack.Id,
                damage,
                attack.HitStunTicks,
                attack.HorizontalKnockback * direction,
                attack.VerticalKnockback);
        }
    }
}
