using DodgerMover.Simulation;
using UnityEngine;

namespace DodgerMover.Presentation
{
    public sealed class ImpactFeedback
    {
        private readonly SpriteRenderer[] rays = new SpriteRenderer[8];
        private readonly AudioSource source;
        private readonly AudioClip light, heavy;
        private float remaining;
        private Vector3 origin;
        private int seenHits;
        public float CameraKick { get; private set; }

        public ImpactFeedback(ArenaArt art, Transform owner)
        {
            for (int i = 0; i < rays.Length; i++)
            {
                rays[i] = art.Rect("Impact ray", 0, 0, .4f, .06f, ArenaArt.Pale, 40);
                rays[i].enabled = false;
            }
            source = owner.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.volume = .30f;
            light = MakeClip("Authored light impact", 125, .10f);
            heavy = MakeClip("Authored heavy impact", 65, .18f);
        }

        private static AudioClip MakeClip(string name, float frequency, float seconds)
        {
            const int rate = 44100;
            var samples = new float[(int)(seconds * rate)];
            uint seed = 4729;
            for (int i = 0; i < samples.Length; i++)
            {
                seed = seed * 1664525 + 1013904223;
                float t = i / (float)rate;
                float noise = ((seed >> 16) / 32767.5f - 1) * .32f;
                samples[i] = (Mathf.Sin(2 * Mathf.PI * frequency * t * (1 - t * 2)) * .65f + noise) * Mathf.Exp(-t * 38) * Mathf.Min(1, t * 1500);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void Render(ImpactSimulation simulation, float delta)
        {
            if (simulation.HitCount != seenHits)
            {
                seenHits = simulation.HitCount;
                origin = new Vector3(simulation.ImpactX, simulation.ImpactY, 0);
                remaining = .19f; CameraKick = simulation.LastHit == MoveId.Heavy ? .15f : .085f;
                source.PlayOneShot(simulation.LastHit == MoveId.Heavy ? heavy : light);
            }
            remaining = Mathf.Max(0, remaining - delta);
            CameraKick = Mathf.MoveTowards(CameraKick, 0, delta * .8f);
            float progress = 1 - remaining / .19f;
            for (int i = 0; i < rays.Length; i++)
            {
                SpriteRenderer ray = rays[i];
                ray.enabled = remaining > 0;
                if (!ray.enabled) continue;
                float angle = i * Mathf.PI / 4 + .15f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                ray.transform.position = origin + direction * (.15f + progress * .65f);
                ray.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                ray.transform.localScale = new Vector3(.40f * (1 - progress), .05f, 1);
                ray.color = i % 2 == 0 ? ArenaArt.Pale : ArenaArt.Orange;
            }
        }

        public void Reset()
        {
            seenHits = 0; remaining = 0; CameraKick = 0;
            source.Stop();
            for (int i = 0; i < rays.Length; i++) rays[i].enabled = false;
        }

        public void Dispose() { Object.Destroy(light); Object.Destroy(heavy); }
    }
}
