using System;
using UnityEngine;

namespace PacificCombat
{
    [DisallowMultipleComponent]
    public sealed class AircraftWeaponSystem : MonoBehaviour
    {
        public WeaponData Configuration;
        public bool UnlimitedAmmo;
        public int Ammo { get; private set; }
        public int ShotsFired { get; private set; }
        public int Hits { get; private set; }
        public bool Firing { get; private set; }
        public event Action Fired;
        public event Action<Vector3> ConfirmedHit;
        public float MuzzleVelocity => Configuration ? Configuration.MuzzleVelocity : 890;
        public float Convergence => Configuration ? Configuration.Convergence : 300;
        public int CannonAmmo { get; private set; }
        struct Bullet
        {
            public bool Active;
            public Vector3 Position, Velocity;
            public float Distance, Damage, Lifetime;
            public int Tracer;
        }
        readonly Bullet[] bullets = new Bullet[512];
        readonly RaycastHit[] hitBuffer = new RaycastHit[20];
        readonly float[] timers = new float[12];
        AircraftController aircraft;
        TracerPool tracers;
        int cursor;
        bool ownsConfiguration;

        public void Initialize(AircraftController owner, WeaponData data = null)
        {
            aircraft = owner;
            Configuration = data ? data : Configuration;
            if (!Configuration) { Configuration = WeaponData.ForAircraft(owner.Data.Type); ownsConfiguration = true; }
            Ammo = Configuration.Ammunition;
            CannonAmmo = Configuration.MixedCannon ? Mathf.Min(Configuration.CannonAmmunition, Ammo) : 0;
            ShotsFired = Hits = 0;
            tracers = gameObject.AddComponent<TracerPool>();
            tracers.Initialize(64, Configuration.MixedCannon ? new Color(1, .35f, .08f) : new Color(1, .8f, .28f));
        }

        void FixedUpdate() => Simulate(Time.fixedDeltaTime);

        public void Simulate(float deltaTime)
        {
            if (!aircraft || !Configuration) return;
            StepBullets(deltaTime);
            Firing = aircraft.Controls.Fire && !aircraft.IsDestroyed && (Ammo > 0 || UnlimitedAmmo);
            int count = Mathf.Clamp(Configuration.GunCount, 1, timers.Length);
            for (int gun = 0; gun < count; gun++)
            {
                timers[gun] -= deltaTime;
                if (!Firing) { timers[gun] = Mathf.Max(0, timers[gun]); continue; }
                if (timers[gun] > .00001f || (!UnlimitedAmmo && Ammo <= 0)) continue;
                bool cannon = Configuration.MixedCannon && gun < Configuration.CannonCount;
                if (cannon && CannonAmmo <= 0 && !UnlimitedAmmo) continue;
                if (!cannon && Ammo - CannonAmmo <= 0 && !UnlimitedAmmo) continue;
                timers[gun] = Mathf.Max(-deltaTime, timers[gun]) + 60 / (cannon ? Configuration.CannonRoundsPerMinute : Configuration.RoundsPerMinutePerGun);
                FireGun(gun, cannon);
            }
        }

        public Vector3 GunPosition(int gun)
        {
            if (Configuration && Configuration.MuzzlePositions != null && gun >= 0 && gun < Configuration.MuzzlePositions.Length)
                return transform.TransformPoint(Configuration.MuzzlePositions[gun]);
            bool mixed = Configuration && Configuration.MixedCannon;
            float x = mixed ? (gun < 2 ? 2.5f : .36f) : 2.35f + (gun / 2) * .35f;
            if ((gun & 1) == 0) x = -x;
            return transform.TransformPoint(new Vector3(x, -.05f, mixed && gun >= 2 ? 2.8f : .8f));
        }

        void FireGun(int gun, bool cannon)
        {
            Vector3 muzzle = GunPosition(gun);
            Vector3 aim = transform.position + transform.forward * Configuration.Convergence;
            Vector2 spread = UnityEngine.Random.insideUnitCircle * Mathf.Tan(Configuration.SpreadDegrees * Mathf.Deg2Rad);
            Vector3 direction = ((aim - muzzle).normalized + transform.right * spread.x + transform.up * spread.y).normalized;
            int slot = -1;
            for (int n = 0; n < bullets.Length; n++)
            {
                int index = (cursor + n) % bullets.Length;
                if (!bullets[index].Active) { slot = index; cursor = (index + 1) % bullets.Length; break; }
            }
            if (slot < 0) return;
            ShotsFired++;
            if (!UnlimitedAmmo) { Ammo--; if (cannon) CannonAmmo--; }
            bullets[slot] = new Bullet
            {
                Active = true, Position = muzzle,
                Velocity = direction * (cannon ? Configuration.CannonMuzzleVelocity : Configuration.MuzzleVelocity) + aircraft.Body.linearVelocity,
                Damage = cannon ? Configuration.CannonDamage : Configuration.Damage,
                Tracer = ShotsFired % Configuration.TracerEvery == 0 ? tracers.Acquire(muzzle) : -1
            };
            Fired?.Invoke();
        }

        void StepBullets(float dt)
        {
            for (int i = 0; i < bullets.Length; i++)
            {
                if (!bullets[i].Active) continue;
                ref Bullet bullet = ref bullets[i];
                bullet.Velocity += Physics.gravity * dt;
                bullet.Velocity *= 1 - .025f * dt;
                Vector3 travel = bullet.Velocity * dt;
                float length = travel.magnitude;
                int hits = Physics.RaycastNonAlloc(bullet.Position, travel.normalized, hitBuffer, length, ~0, QueryTriggerInteraction.Ignore);
                int nearest = -1;
                float distance = float.MaxValue;
                for (int h = 0; h < hits; h++)
                {
                    if (hitBuffer[h].collider.transform.IsChildOf(transform)) continue;
                    if (hitBuffer[h].distance < distance) { nearest = h; distance = hitBuffer[h].distance; }
                }
                if (nearest >= 0)
                {
                    RaycastHit hit = hitBuffer[nearest];
                    var zone = hit.collider.GetComponent<DamageZone>();
                    AircraftDamage victim = zone ? zone.Owner : hit.collider.GetComponentInParent<AircraftDamage>();
                    if (victim && victim.Aircraft && !victim.Aircraft.IsDestroyed)
                    {
                        victim.ApplyDamage(zone ? zone.Type : DamageZoneType.Fuselage,
                            bullet.Damage * Mathf.Lerp(1, .45f, bullet.Distance / Configuration.Range), hit.point);
                        Hits++;
                        ConfirmedHit?.Invoke(hit.point);
                    }
                    Release(ref bullet);
                    continue;
                }
                bullet.Position += travel;
                bullet.Distance += length;
                bullet.Lifetime += dt;
                if (bullet.Tracer >= 0) tracers.Move(bullet.Tracer, bullet.Position - travel.normalized * Mathf.Min(14, length), bullet.Position);
                if (bullet.Distance >= Configuration.Range || bullet.Lifetime > 5 || bullet.Position.y < 0) Release(ref bullet);
            }
        }

        void Release(ref Bullet bullet)
        {
            bullet.Active = false;
            if (bullet.Tracer >= 0) tracers.Release(bullet.Tracer);
        }

        public void ShiftProjectiles(Vector3 offset)
        {
            for (int i = 0; i < bullets.Length; i++) if (bullets[i].Active) bullets[i].Position -= offset;
            if (tracers) tracers.Shift(offset);
        }
        void OnDestroy()
        {
            if (!ownsConfiguration || !Configuration) return;
            if (Application.isPlaying) Destroy(Configuration); else DestroyImmediate(Configuration);
        }
    }
}
