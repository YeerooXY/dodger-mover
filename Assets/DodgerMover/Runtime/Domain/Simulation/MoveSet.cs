using DodgerMover.Combat;

namespace DodgerMover.Simulation
{
    public enum MoveId { None, Light1, Light2, Light3, Heavy, Launcher, AirLight }
    public enum ActionPhase { Ready, Startup, Active, Recovery, HitStun, Defeated }

    public sealed class MoveDefinition
    {
        internal MoveDefinition(MoveId id, string label, int startup, int active, int recovery,
            int damage, int stun, float push, float lift, float reach, float top, int stop)
        {
            Id = id; Label = label; Startup = startup; Active = active; Recovery = recovery;
            Hit = new AttackDefinition(label, damage, stun, push, lift);
            Reach = reach; Top = top; HitStop = stop;
        }

        public MoveId Id { get; }
        public string Label { get; }
        public int Startup { get; }
        public int Active { get; }
        public int Recovery { get; }
        public int Total => Startup + Active + Recovery;
        public AttackDefinition Hit { get; }
        public float Reach { get; }
        public float Top { get; }
        public int HitStop { get; }
    }

    public static class MoveSet
    {
        public static readonly MoveDefinition Light1 = new MoveDefinition(MoveId.Light1, "LIGHT 01", 4, 3, 13, 8, 18, 2.8f, 0, 1.55f, 1.6f, 4);
        public static readonly MoveDefinition Light2 = new MoveDefinition(MoveId.Light2, "LIGHT 02", 4, 3, 14, 10, 20, 3.6f, 0, 1.75f, 1.7f, 4);
        public static readonly MoveDefinition Light3 = new MoveDefinition(MoveId.Light3, "LIGHT 03", 6, 4, 19, 16, 28, 8f, 4f, 2f, 1.8f, 6);
        public static readonly MoveDefinition Heavy = new MoveDefinition(MoveId.Heavy, "HEAVY", 13, 4, 23, 26, 32, 10f, 4.5f, 2.1f, 1.9f, 7);
        public static readonly MoveDefinition Launcher = new MoveDefinition(MoveId.Launcher, "LAUNCHER", 8, 4, 21, 12, 42, 2.4f, 14f, 1.65f, 2.25f, 5);
        public static readonly MoveDefinition AirLight = new MoveDefinition(MoveId.AirLight, "AIR FOLLOW-UP", 4, 4, 17, 20, 26, 7f, 4.5f, 1.9f, 1.9f, 5);
    }
}
