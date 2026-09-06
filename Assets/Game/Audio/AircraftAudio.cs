using UnityEngine;

namespace PacificCombat
{
    [DisallowMultipleComponent]
    public sealed class AircraftAudio : MonoBehaviour
    {
        public static float MasterVolume = .65f;
        public static float EngineVolume = .65f;
        public static float WeaponVolume = .7f;
        AircraftController aircraft;
        AircraftWeaponSystem weapons;
        AircraftDamage damage;
        AudioSource engine, guns, wind, impact;
        AudioClip engineClip, gunClip, windClip, hitClip;
        float nextGunSound;
        public void Initialize(AircraftController owner, AircraftWeaponSystem weaponSystem)
        {
            aircraft = owner; weapons = weaponSystem;
            damage = owner.GetComponent<AircraftDamage>();
            engineClip = Synthesize("Procedural piston engine", 1, 0);
            gunClip = Synthesize("Procedural gun report", .12f, 1);
            windClip = Synthesize("Procedural slipstream", 2, 2);
            hitClip = Synthesize("Procedural metal impact", .25f, 3);
            engine = Source(engineClip, true, !owner.IsPlayer); engine.Play();
            wind = Source(windClip, true, !owner.IsPlayer); wind.Play();
            guns = Source(gunClip, false, !owner.IsPlayer);
            impact = Source(hitClip, false, !owner.IsPlayer);
            weapons.Fired += OnFired;
            if (damage) damage.Hit += OnHit;
        }
        AudioSource Source(AudioClip clip, bool loop, bool spatial)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip; source.loop = loop; source.playOnAwake = false;
            source.spatialBlend = spatial ? 1 : 0;
            source.minDistance = 12; source.maxDistance = 1600;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.dopplerLevel = spatial ? .6f : 0;
            source.volume = 0;
            return source;
        }
        void Update()
        {
            if (!aircraft) return;
            bool starved = aircraft.Engine && aircraft.Engine.Fuel && !aircraft.Engine.Fuel.HasFuel;
            float running = aircraft.IsDestroyed || starved ? 0 : aircraft.EngineHealth;
            engine.pitch = Mathf.Lerp(.6f, 1.6f, aircraft.Controls.Throttle);
            engine.volume = MasterVolume * EngineVolume * running * (.22f + .55f * aircraft.Controls.Throttle);
            wind.volume = MasterVolume * Mathf.Clamp01(aircraft.Airspeed / 220) * (aircraft.IsPlayer ? .13f : .025f);
            if (Time.timeScale == 0) { engine.Pause(); wind.Pause(); }
            else { if (!engine.isPlaying) engine.UnPause(); if (!wind.isPlaying) wind.UnPause(); }
        }
        void OnFired()
        {
            if (Time.time < nextGunSound) return;
            nextGunSound = Time.time + .065f;
            guns.volume = MasterVolume * WeaponVolume;
            guns.pitch = Random.Range(.95f, 1.05f);
            guns.PlayOneShot(gunClip);
        }
        void OnHit(Vector3 _, float amount)
        {
            if (amount < 1) return;
            impact.volume = MasterVolume * .55f;
            impact.PlayOneShot(hitClip);
        }
        static AudioClip Synthesize(string label, float seconds, int kind)
        {
            const int rate = 22050;
            var samples = new float[(int)(rate * seconds)];
            var random = new System.Random(51 + kind);
            float smooth = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float noise = (float)random.NextDouble() * 2 - 1;
                smooth = Mathf.Lerp(smooth, noise, .12f);
                if (kind == 0) samples[i] = (.22f * Mathf.Sin(t * 2 * Mathf.PI * 80) + .16f * Mathf.Sin(t * 2 * Mathf.PI * 160) + .1f * smooth) * (1 + .25f * Mathf.Sin(t * 2 * Mathf.PI * 20));
                else if (kind == 2) samples[i] = smooth * .65f;
                else samples[i] = (noise * .65f + Mathf.Sin(t * (kind == 1 ? 450 : 1600)) * .3f) * Mathf.Exp(-t * (kind == 1 ? 45 : 22));
            }
            var clip = AudioClip.Create(label, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        void OnDestroy()
        {
            if (weapons) weapons.Fired -= OnFired;
            if (damage) damage.Hit -= OnHit;
            if (engineClip) Destroy(engineClip); if (gunClip) Destroy(gunClip);
            if (windClip) Destroy(windClip); if (hitClip) Destroy(hitClip);
        }
    }
}
