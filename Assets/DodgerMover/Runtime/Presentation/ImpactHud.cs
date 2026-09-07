using DodgerMover.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace DodgerMover.Presentation
{
    public sealed class ImpactHud
    {
        private readonly Transform canvas;
        private readonly Font font;
        private readonly Text status, hitLabel, diagnostics;
        private readonly Text[] controls = new Text[5];
        private static readonly string[] KeyboardControls = { "A / D  or  ARROWS", "SPACE", "J / X", "K / C", "L / V" };
        private static readonly string[] GamepadControls = { "STICK / D-PAD", "A / CROSS", "X / SQUARE", "Y / TRIANGLE", "B / CIRCLE" };
        private readonly Image health;
        private readonly GameObject complete;
        private int previousHealth = -1, previousHits = -1;
        private bool previousGamepad, debugVisible;
        private float nextDiagnostic;

        public ImpactHud(Transform parent, Font font, UnityEngine.Events.UnityAction restart)
        {
            this.font = font;
            var obj = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            obj.transform.SetParent(parent, false); canvas = obj.transform;
            obj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;

            Label("FIELD TEST   /   001", 72, 48, 600, 24, 16, ArenaArt.Teal);
            Label("IMPACT PROOF", 69, 81, 800, 65, 48, ArenaArt.Pale);
            Label("A tiny room. Make every hit count.", 73, 154, 750, 34, 20, new Color(.56f, .66f, .69f));
            Label("D O D G E R   M O V E R", 1470, 48, 380, 30, 18, ArenaArt.Pale, TextAnchor.UpperRight);

            var restartPanel = Panel("Restart", 1608, 104, 240, 52, new Color(.12f, .22f, .25f));
            var button = restartPanel.gameObject.AddComponent<Button>(); button.targetGraphic = restartPanel;
            button.onClick.AddListener(restart);
            Label("RESTART   [R]", 1608, 104, 240, 52, 17, ArenaArt.Teal, TextAnchor.MiddleCenter);

            Label("TRAINING UNIT", 1470, 216, 378, 28, 16, new Color(.65f, .71f, .71f));
            Panel("Health track", 1470, 255, 378, 5, new Color(.18f, .25f, .27f));
            health = Panel("Health", 1470, 255, 378, 5, ArenaArt.Orange);
            status = Label("160 / 160", 1470, 277, 378, 30, 17, ArenaArt.Pale);
            hitLabel = Label("", 1470, 321, 378, 80, 25, ArenaArt.Orange);

            Panel("Footer line", 72, 888, 1776, 1, new Color(.19f, .28f, .30f));
            Label("THE FLOOR IS YOUR STARTING POINT.", 73, 832, 1400, 32, 17, new Color(.47f, .58f, .61f));
            Label("MOVE", 73, 923, 240, 24, 14, ArenaArt.Teal);
            Label("JUMP", 408, 923, 240, 24, 14, ArenaArt.Teal);
            Label("LIGHT", 745, 923, 240, 24, 14, ArenaArt.Teal);
            Label("HEAVY", 1080, 923, 240, 24, 14, ArenaArt.Teal);
            Label("LAUNCH", 1418, 923, 240, 24, 14, ArenaArt.Teal);
            for (int i = 0; i < controls.Length; i++)
                controls[i] = Label(KeyboardControls[i], 73 + i * 336, 962, 315, 36, 22, ArenaArt.Pale);
            Label("F1 / SELECT   diagnostics", 73, 1033, 700, 24, 14, new Color(.40f, .51f, .55f));
            Label("5-MINUTE EXPERIMENT    /    ESC TO EXIT", 1230, 1033, 618, 24, 14, new Color(.40f, .51f, .55f), TextAnchor.UpperRight);

            diagnostics = Label("", 73, 231, 790, 190, 17, new Color(.64f, .77f, .77f));
            diagnostics.gameObject.SetActive(false);
            complete = new GameObject("Room clear", typeof(RectTransform)); complete.transform.SetParent(canvas, false);
            var banner = Panel("Clear panel", 660, 364, 600, 150, new Color(.035f, .063f, .095f, .94f));
            banner.transform.SetParent(complete.transform, false);
            var clearTitle = Label("ROOM CLEAR", 680, 384, 560, 50, 36, ArenaArt.Teal, TextAnchor.MiddleCenter);
            clearTitle.transform.SetParent(complete.transform, false);
            var clearHint = Label("R / START to reset. Try another sequence.", 680, 446, 560, 35, 19, ArenaArt.Pale, TextAnchor.MiddleCenter);
            clearHint.transform.SetParent(complete.transform, false);
            var completeRect = (RectTransform)complete.transform;
            completeRect.anchorMin = Vector2.zero; completeRect.anchorMax = Vector2.one;
            completeRect.offsetMin = Vector2.zero; completeRect.offsetMax = Vector2.zero;
            complete.SetActive(false);
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }

        private Image Panel(string name, float x, float y, float width, float height, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(canvas, false); Place((RectTransform)obj.transform, x, y, width, height);
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = name == "Restart";
            return image;
        }

        private Text Label(string value, float x, float y, float width, float height, int size, Color color, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var obj = new GameObject(value.Length > 0 ? value : "Dynamic label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(canvas, false); Place((RectTransform)obj.transform, x, y, width, height);
            var text = obj.GetComponent<Text>(); text.font = font; text.fontSize = size; text.color = color;
            text.alignment = alignment; text.text = value; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public void ToggleDiagnostics() { debugVisible = !debugVisible; diagnostics.gameObject.SetActive(debugVisible); }

        public void Render(ImpactSimulation sim, bool gamepad, float time, float frameMs, double ownedMs, long ownedBytes)
        {
            if (sim.Enemy.Health != previousHealth)
            {
                previousHealth = sim.Enemy.Health;
                health.rectTransform.sizeDelta = new Vector2(378f * previousHealth / sim.Enemy.MaxHealth, 5);
                status.text = previousHealth + " / " + sim.Enemy.MaxHealth + (previousHealth == 0 ? "   DEFEATED" : "   APPROACHING");
                complete.SetActive(previousHealth == 0);
            }
            if (sim.HitCount != previousHits)
            {
                previousHits = sim.HitCount;
                hitLabel.text = previousHits == 0 ? "" : sim.LastDamage + "  /  " + HitName(sim.LastHit);
            }
            if (gamepad != previousGamepad)
            {
                previousGamepad = gamepad;
                for (int i = 0; i < controls.Length; i++)
                    controls[i].text = gamepad ? GamepadControls[i] : KeyboardControls[i];
            }
            // Optional diagnostics allocate formatted strings at 4 Hz, outside owned-loop measurement.
            if (debugVisible && time >= nextDiagnostic)
            {
                nextDiagnostic = time + .25f;
                diagnostics.text = "60 HZ  /  TICK " + sim.Tick + "  /  HIT-STOP " + sim.HitStopTicks +
                    "\nPLAYER  " + sim.Player.Phase + "   " + (sim.Player.Grounded ? "GROUNDED" : "AIRBORNE") +
                    "\nBUFFER  " + sim.Buffer.AttackAt(sim.Tick) + (sim.Buffer.HasJump(sim.Tick) ? " + JUMP" : "") +
                    "\nFRAME  " + frameMs.ToString("F2") + " ms   OWNED  " + ownedMs.ToString("F3") + " ms" +
                    "\nOWNED ALLOC  " + ownedBytes + " B   (HUD formatting excluded)";
            }
        }

        private static string HitName(MoveId move)
        {
            switch (move)
            {
                case MoveId.Light1: return "LIGHT 01";
                case MoveId.Light2: return "LIGHT 02";
                case MoveId.Light3: return "LIGHT 03";
                case MoveId.Heavy: return "HEAVY";
                case MoveId.Launcher: return "LAUNCHER";
                case MoveId.AirLight: return "AIR FOLLOW-UP";
                default: return "";
            }
        }
    }
}
