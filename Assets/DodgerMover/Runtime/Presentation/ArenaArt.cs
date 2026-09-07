using DodgerMover.Simulation;
using UnityEngine;

namespace DodgerMover.Presentation
{
    // All geometry is authored in code; no downloaded or generated bitmap assets.
    public sealed class ArenaArt
    {
        public static readonly Color Ink = new Color(.035f, .063f, .095f);
        public static readonly Color Teal = new Color(.32f, .96f, .81f);
        public static readonly Color Orange = new Color(1f, .41f, .23f);
        public static readonly Color Pale = new Color(.86f, .91f, .90f);
        private readonly Sprite square;
        private readonly Material material;
        private readonly Transform parent;

        public ArenaArt(Transform parent, Material material)
        {
            this.parent = parent; this.material = material;
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            square.name = "Authored unit square";
        }

        public SpriteRenderer Rect(string name, float x, float y, float width, float height, Color color, int order, Transform owner = null)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(owner != null ? owner : parent, false);
            obj.transform.localPosition = new Vector3(x, y, 0);
            obj.transform.localScale = new Vector3(width, height, 1);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = square; renderer.sharedMaterial = material; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }

        public void BuildRoom()
        {
            Rect("Back wall", 0, 3, 40, 24, Ink, -30);
            for (int i = -6; i <= 6; i++)
            {
                Rect("Wall seam", i * 3f, 4, .014f, 14, new Color(.10f, .15f, .18f), -25);
                Rect("High window", i * 3f + .1f, 6.5f, .025f, 1.8f, new Color(.14f, .23f, .25f), -24);
            }
            Rect("Upper stripe", 0, 7.8f, 30, .018f, new Color(.15f, .23f, .25f), -24);
            Rect("Distant inset", -4, 2.9f, 6.2f, 3.5f, new Color(.047f, .083f, .115f), -23);
            Rect("Distant inset base", -4, 1.15f, 6.2f, .035f, new Color(.10f, .19f, .20f), -22);
            Rect("Floor mass", 0, -3.4f, 26, 6.6f, new Color(.065f, .105f, .14f), -10);
            Rect("Floor edge", 0, -.05f, 24, .10f, new Color(.46f, .59f, .60f), -9);
            Rect("Floor accent", -4.5f, -.15f, 7, .035f, Teal * .65f, -8);
            for (int i = -11; i < 12; i++) Rect("Floor tick", i, -.38f, .02f, .13f, new Color(.25f, .34f, .37f), -8);
            float center = (ImpactSimulation.PlatformLeft + ImpactSimulation.PlatformRight) / 2;
            float width = ImpactSimulation.PlatformRight - ImpactSimulation.PlatformLeft;
            Rect("Raised platform", center, ImpactSimulation.PlatformY - .22f, width, .44f, new Color(.16f, .23f, .26f), -6);
            Rect("Platform contact edge", center, ImpactSimulation.PlatformY - .025f, width, .05f, Teal, -5);
            Rect("Platform support", center + .7f, 1, .18f, 2, new Color(.12f, .18f, .21f), -7);
            Rect("Platform underlight", center, ImpactSimulation.PlatformY - .47f, width - .3f, .025f, Teal * .5f, -5);
            Rect("Left boundary", -12, 1.8f, .15f, 3.6f, new Color(.25f, .36f, .39f), -6);
            Rect("Right boundary", 12, 1.8f, .15f, 3.6f, new Color(.25f, .36f, .39f), -6);
        }

        public void Dispose() { Object.Destroy(square); }
    }

    public sealed class FighterView
    {
        private readonly Transform root, torso, head, frontArm, backArm, frontLeg, backLeg;
        private readonly SpriteRenderer body, visor, shadow, slash;
        private readonly Color accent;
        private readonly int baseOrder;

        public FighterView(ArenaArt art, Transform parent, string name, Color accent, int order)
        {
            this.accent = accent; baseOrder = order;
            root = new GameObject(name).transform; root.SetParent(parent, false);
            shadow = art.Rect(name + " shadow", 0, .02f, 1.05f, .08f, new Color(0, 0, 0, .35f), -3);
            backLeg = art.Rect("Back boot", -.22f, .25f, .24f, .5f, accent * .45f, order, root).transform;
            frontLeg = art.Rect("Front boot", .20f, .25f, .27f, .5f, accent * .7f, order + 2, root).transform;
            backArm = art.Rect("Back arm", -.4f, .92f, .22f, .6f, accent * .6f, order, root).transform;
            body = art.Rect("Torso", 0, .87f, .67f, .74f, accent, order + 1, root); torso = body.transform;
            art.Rect("Chest plate", .12f, .91f, .26f, .38f, new Color(.05f, .12f, .15f), order + 2, root);
            head = art.Rect("Head", .07f, 1.43f, .53f, .48f, accent, order + 2, root).transform;
            visor = art.Rect("Visor", .25f, 1.47f, .30f, .10f, InkColor(), order + 3, root);
            frontArm = art.Rect("Front gauntlet", .41f, .89f, .29f, .45f, ArenaArt.Pale, order + 3, root).transform;
            slash = art.Rect("Attack stroke", .9f, .9f, 1.3f, .12f, accent, order + 5, root);
            slash.enabled = false;
        }

        private static Color InkColor() => new Color(.035f, .08f, .11f);

        public void Render(Fighter fighter, float time)
        {
            bool defeated = fighter.Phase == ActionPhase.Defeated;
            bool stunned = fighter.Phase == ActionPhase.HitStun;
            float stride = fighter.Grounded ? Mathf.Sin(time * 16f) * Mathf.Min(1, Mathf.Abs(fighter.VelocityX) / 5) : .5f;
            root.localPosition = new Vector3(fighter.X, fighter.Y, 0);
            root.localScale = new Vector3(fighter.Facing, defeated ? .26f : 1, 1);
            root.localRotation = Quaternion.Euler(0, 0, stunned ? -fighter.Facing * 11 : 0);
            frontLeg.localRotation = Quaternion.Euler(0, 0, stride * 28);
            backLeg.localRotation = Quaternion.Euler(0, 0, -stride * 28);
            bool attacking = fighter.Move != null;
            bool active = fighter.Phase == ActionPhase.Active;
            float windup = fighter.Phase == ActionPhase.Startup ? -30 : active ? 85 : 0;
            frontArm.localRotation = Quaternion.Euler(0, 0, -windup);
            frontArm.localPosition = new Vector3(active ? .82f : .41f, active ? 1.05f : .89f, 0);
            backArm.localRotation = Quaternion.Euler(0, 0, attacking ? 20 : stride * 25);
            torso.localRotation = Quaternion.Euler(0, 0, active ? -9 : 0);
            head.localRotation = Quaternion.Euler(0, 0, active ? -7 : 0);
            body.color = stunned && fighter.StunTicks > 20 ? ArenaArt.Pale : accent;
            visor.color = defeated ? accent * .25f : InkColor();
            slash.enabled = active;
            if (active)
            {
                bool launcher = fighter.Move.Id == MoveId.Launcher;
                slash.transform.localPosition = new Vector3(launcher ? .7f : 1.15f, launcher ? 1.45f : 1, 0);
                slash.transform.localScale = new Vector3(launcher ? .13f : fighter.Move.Reach, launcher ? 1.4f : .14f, 1);
                slash.transform.localRotation = Quaternion.Euler(0, 0, launcher ? -24 : 10);
                slash.sortingOrder = baseOrder + 5;
            }
            float floor = fighter.Y >= ImpactSimulation.PlatformY && fighter.X > ImpactSimulation.PlatformLeft && fighter.X < ImpactSimulation.PlatformRight ? ImpactSimulation.PlatformY : 0;
            shadow.transform.position = new Vector3(fighter.X, floor + .015f, 0);
            shadow.transform.localScale = new Vector3(Mathf.Max(.3f, 1.05f - (fighter.Y - floor) * .12f), .07f, 1);
        }
    }
}
