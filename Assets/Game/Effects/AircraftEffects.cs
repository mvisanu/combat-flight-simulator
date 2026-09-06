using UnityEngine;

namespace PacificCombat
{
    [DisallowMultipleComponent]
    public sealed class AircraftEffects : MonoBehaviour
    {
        AircraftController aircraft;
        AircraftDamage damage;
        ParticleSystem smoke, fire, impacts, muzzle;
        Material smokeMaterial, fireMaterial;
        Texture2D particleTexture;
        AircraftWeaponSystem weapons;
        float lastFlash = -1;
        public void Initialize(AircraftController owner, AircraftDamage damageModel)
        {
            aircraft = owner; damage = damageModel;
            smokeMaterial = ParticleMaterial(new Color(.1f, .12f, .14f, .5f));
            fireMaterial = ParticleMaterial(new Color(1, .38f, .04f, .9f));
            particleTexture = CreateSoftParticle();
            smokeMaterial.SetTexture("_BaseMap", particleTexture);
            fireMaterial.SetTexture("_BaseMap", particleTexture);
            smoke = CreateParticles("Engine smoke", smokeMaterial, 320, 7, 2.3f, 1);
            fire = CreateParticles("Engine fire", fireMaterial, 90, .65f, 1.2f, 2);
            impacts = CreateParticles("Impact sparks", fireMaterial, 96, .35f, .1f, 10);
            muzzle = CreateParticles("Gun muzzle flashes", fireMaterial, 48, .045f, .8f, 0);
            var main = muzzle.main; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            weapons = owner.GetComponent<AircraftWeaponSystem>();
            if (weapons) weapons.Fired += OnFired;
            smoke.transform.localPosition = fire.transform.localPosition = new Vector3(0, .3f, 1.8f);
            damage.Hit += OnHit;
            damage.Destroyed += OnDestroyed;
        }
        static Texture2D CreateSoftParticle()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Procedural soft smoke alpha";
            var colors = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float r = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1).magnitude;
                colors[y * size + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - r), 1.7f));
            }
            texture.SetPixels(colors); texture.Apply(false, true);
            return texture;
        }
        void OnFired()
        {
            if (lastFlash == Time.fixedTime) return;
            lastFlash = Time.fixedTime;
            for (int i = 0; i < weapons.Configuration.GunCount; i++)
            {
                var emit = new ParticleSystem.EmitParams
                {
                    position = muzzle.transform.InverseTransformPoint(weapons.GunPosition(i)),
                    velocity = Vector3.zero
                };
                muzzle.Emit(emit, 1);
            }
        }
        static Material ParticleMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            return material;
        }
        ParticleSystem CreateParticles(string label, Material material, int maximum, float lifetime, float size, float speed)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            var system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false; main.loop = true; main.maxParticles = maximum;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime; main.startSize = new ParticleSystem.MinMaxCurve(size * .5f, size);
            main.startSpeed = speed; main.startColor = Color.white;
            var emission = system.emission; emission.rateOverTime = 0;
            var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .35f;
            var color = system.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.gray, 1) },
                new[] { new GradientAlphaKey(.8f, 0), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var sizeOver = system.sizeOverLifetime; sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .4f, 1, 2));
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            system.Play();
            return system;
        }
        void Update()
        {
            if (!aircraft || !damage) return;
            var emission = smoke.emission;
            emission.rateOverTime = aircraft.IsDestroyed ? 25 : (1 - aircraft.EngineHealth) * 16 + (damage.FuelLeaking ? 5 : 0);
            emission = fire.emission; emission.rateOverTime = damage.Burning ? 30 : 0;
        }
        void OnHit(Vector3 point, float amount)
        {
            // Continuous fire damage should not create bullet impacts every physics step.
            if (amount < 1) return;
            var emit = new ParticleSystem.EmitParams { position = point, velocity = UnityEngine.Random.onUnitSphere * 9 };
            impacts.Emit(emit, 6);
        }
        void OnDestroyed(AircraftDamage _) { smoke.Emit(25); }
        public void ShiftParticles(Vector3 offset)
        {
            Shift(smoke, offset); Shift(fire, offset); Shift(impacts, offset);
        }
        readonly ParticleSystem.Particle[] shiftBuffer = new ParticleSystem.Particle[320];
        void Shift(ParticleSystem system, Vector3 offset)
        {
            if (!system) return;
            int count = system.GetParticles(shiftBuffer);
            for (int i = 0; i < count; i++) shiftBuffer[i].position -= offset;
            system.SetParticles(shiftBuffer, count);
        }
        void OnDestroy()
        {
            if (damage) { damage.Hit -= OnHit; damage.Destroyed -= OnDestroyed; }
            if (weapons) weapons.Fired -= OnFired;
            if (smokeMaterial) Destroy(smokeMaterial);
            if (fireMaterial) Destroy(fireMaterial);
            if (particleTexture) Destroy(particleTexture);
        }
    }
}
