using System.Collections.Generic;
using UnityEngine;

namespace PacificCombat
{
    public static class AircraftFactory
    {
        static Material silver, green, dark, glass, red, white, yellow, propellerBlur;
        static void Materials()
        {
            if (silver) return;
            silver = PacificEnvironment.Material("Mustang aluminum", new Color(.64f, .69f, .69f), .72f, .66f);
            green = PacificEnvironment.Material("Zero navy green", new Color(.12f, .23f, .16f), .22f);
            dark = PacificEnvironment.Material("Rubber and anti-glare", new Color(.04f, .055f, .055f));
            glass = PacificEnvironment.Material("Canopy blue glass", new Color(.16f, .34f, .4f), .65f, .95f);
            red = PacificEnvironment.Material("Hinomaru", new Color(.64f, .055f, .045f));
            white = PacificEnvironment.Material("Insignia ivory", new Color(.87f, .87f, .77f));
            yellow = PacificEnvironment.Material("Squadron yellow", new Color(.96f, .66f, .13f));
            propellerBlur = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Propeller blur", enableInstancing = true };
            propellerBlur.SetColor("_BaseColor", new Color(.12f, .16f, .17f, .065f));
            propellerBlur.SetFloat("_Surface", 1); propellerBlur.SetFloat("_ZWrite", 0); propellerBlur.SetFloat("_Cull", 0);
            propellerBlur.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            propellerBlur.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            propellerBlur.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); propellerBlur.renderQueue = 3000;
        }

        public static AircraftController Create(AircraftData data, bool player, int team, Vector3 position, Quaternion rotation, float speed, WeaponData weaponData = null)
        {
            Materials();
            var root = new GameObject(player ? "P-51D Mustang — Player" : "A6M Zero");
            root.transform.SetPositionAndRotation(position, rotation);
            var aircraft = root.AddComponent<AircraftController>();
            var damage = root.AddComponent<AircraftDamage>();
            damage.Initialize(aircraft);
            var animation = root.AddComponent<AircraftVisuals>(); animation.Aircraft = aircraft;
            animation.Ailerons = new Transform[2]; animation.Elevators = new Transform[2]; animation.Flaps = new Transform[2];
            animation.GeneratedMeshes = new Mesh[2];
            var visuals = new GameObject("Airframe").transform;
            visuals.SetParent(root.transform, false);
            bool zero = data.AircraftName.Contains("Zero");
            Material skin = zero ? green : silver;
            Part("Fuselage", PrimitiveType.Sphere, visuals, new Vector3(0, 0, -.1f), new Vector3(1.3f, 1.5f, zero ? 8.7f : 9.8f), skin, damage, DamageZoneType.Fuselage);
            Part("Engine cowling", PrimitiveType.Sphere, visuals, new Vector3(0, .07f, 3.15f), new Vector3(zero ? 1.75f : 1.2f, 1.45f, 2.4f), zero ? dark : skin, damage, DamageZoneType.Engine);
            Part("Spinner", PrimitiveType.Sphere, visuals, new Vector3(0, .05f, 4.5f), new Vector3(.72f, .72f, 1), zero ? skin : yellow, damage, DamageZoneType.Engine);
            var canopy = Part("Canopy", PrimitiveType.Sphere, visuals, new Vector3(0, .85f, .3f), new Vector3(1.05f, 1.05f, 2.5f), glass);
            // Separate pilot and canopy-frame volumes so a pilot hit is reachable from above/rear.
            DamageVolume("Cockpit frame volume", visuals, new Vector3(0, .82f, 1.05f), new Vector3(1.05f, .8f, .28f), damage, DamageZoneType.Cockpit);
            DamageVolume("Pilot volume", visuals, new Vector3(0, .94f, -.1f), new Vector3(.5f, .78f, .7f), damage, DamageZoneType.Pilot);
            DamageVolume("Fuel tank volume", visuals, new Vector3(0, -.43f, -1.15f), new Vector3(.95f, .7f, 1.4f), damage, DamageZoneType.FuelTank);
            Part("Cockpit frame", PrimitiveType.Cube, visuals, new Vector3(0, 1.15f, -.8f), new Vector3(1.03f, .08f, .09f), skin);
            if (!zero) Part("Radiator scoop", PrimitiveType.Sphere, visuals, new Vector3(0, -.62f, -1), new Vector3(1, .65f, 2), dark, damage, DamageZoneType.FuelTank);

            for (int side = -1; side <= 1; side += 2)
            {
                var wing = new GameObject(side < 0 ? "Left wing" : "Right wing");
                wing.transform.SetParent(visuals, false);
                int sideIndex = side < 0 ? 0 : 1;
                animation.GeneratedMeshes[sideIndex] = WingMesh(side, zero);
                wing.AddComponent<MeshFilter>().sharedMesh = animation.GeneratedMeshes[sideIndex];
                wing.AddComponent<MeshRenderer>().sharedMaterial = skin;
                var box = wing.AddComponent<BoxCollider>();
                box.center = new Vector3(side * 2.9f, -.1f, .6f);
                box.size = new Vector3(5.25f, .23f, 1.45f);
                var zone = wing.AddComponent<DamageZone>(); zone.Owner = damage; zone.Type = side < 0 ? DamageZoneType.LeftWing : DamageZoneType.RightWing;
                var mark = Part("National insignia", PrimitiveType.Cylinder, visuals, new Vector3(side * 3.9f, .06f, .3f), new Vector3(1.2f, .012f, 1.2f), zero ? red : dark);
                if (!zero)
                {
                    Part("Insignia bar", PrimitiveType.Cube, visuals, new Vector3(side * 3.9f, .09f, .3f), new Vector3(1.7f, .02f, .35f), white);
                    Part("Insignia center", PrimitiveType.Cylinder, visuals, new Vector3(side * 3.9f, .11f, .3f), new Vector3(.58f, .01f, .58f), white);
                }
                Part("Horizontal stabilizer", PrimitiveType.Cube, visuals, new Vector3(side * 1.15f, .1f, -3.6f), new Vector3(2.3f, .11f, .8f), skin, damage, DamageZoneType.HorizontalStabilizer);
                animation.Ailerons[sideIndex] = ControlSurface("Aileron", visuals, new Vector3(side * 3.8f, -.04f, -.25f), new Vector3(0, 0, -.22f), new Vector3(2.2f, .085f, .44f), skin, damage, side < 0 ? DamageZoneType.LeftAileron : DamageZoneType.RightAileron);
                animation.Flaps[sideIndex] = ControlSurface("Flap", visuals, new Vector3(side * 1.65f, -.07f, -.65f), new Vector3(0, 0, -.22f), new Vector3(1.65f, .085f, .44f), skin, damage, side < 0 ? DamageZoneType.LeftWing : DamageZoneType.RightWing);
                animation.Elevators[sideIndex] = ControlSurface("Elevator", visuals, new Vector3(side * 1.15f, .1f, -4f), new Vector3(0, 0, -.21f), new Vector3(2.3f, .1f, .42f), skin, damage, DamageZoneType.Elevator);
            }
            Part("Vertical stabilizer", PrimitiveType.Sphere, visuals, new Vector3(0, .7f, -3.4f), new Vector3(.15f, 1.95f, .95f), skin, damage, DamageZoneType.VerticalStabilizer);
            animation.Rudder = ControlSurface("Rudder", visuals, new Vector3(0, .75f, -3.88f), new Vector3(0, 0, -.31f), new Vector3(.13f, 1.65f, .62f), skin, damage, DamageZoneType.Rudder);
            var prop = new GameObject("Propeller").transform; prop.SetParent(visuals, false); prop.localPosition = new Vector3(0, .05f, 4.25f);
            for (int p = 0; p < (zero ? 3 : 4); p++)
            {
                Quaternion bladeRotation = Quaternion.Euler(0, 0, p * (zero ? 120 : 90));
                var blade = Part("Propeller blade", PrimitiveType.Cube, prop, bladeRotation * new Vector3(0, .825f, 0), new Vector3(.15f, 1.65f, .045f), dark);
                blade.localRotation = bladeRotation;
            }
            animation.Propeller = prop;
            animation.PropellerBlur = Part("High RPM propeller disk", PrimitiveType.Cylinder, visuals, new Vector3(0, .05f, 4.25f), new Vector3(3.3f, .002f, 3.3f), propellerBlur);
            animation.PropellerBlur.localRotation = Quaternion.Euler(90, 0, 0);
            var blurRenderer = animation.PropellerBlur.GetComponent<Renderer>();
            blurRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; blurRenderer.receiveShadows = false;
            animation.PropellerBlur.gameObject.SetActive(false);
            animation.Gear = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var gear = new GameObject("Landing gear").transform; gear.SetParent(visuals, false); gear.localPosition = new Vector3(i == 0 ? -2.2f : 2.2f, -.2f, .6f);
                Part("Gear strut", PrimitiveType.Cube, gear, new Vector3(0, -.6f, 0), new Vector3(.08f, 1.2f, .08f), silver);
                var wheel = Part("Wheel", PrimitiveType.Cylinder, gear, new Vector3(0, -1.2f, 0), new Vector3(.55f, .12f, .55f), dark);
                wheel.localRotation = Quaternion.Euler(0, 0, 90); animation.Gear[i] = gear; gear.gameObject.SetActive(false);
            }
            var renderers = new List<Renderer>(visuals.GetComponentsInChildren<Renderer>(true));
            var distantRenderers = new[] { visuals.GetChild(0).GetComponent<Renderer>(), visuals.Find("Left wing").GetComponent<Renderer>(), visuals.Find("Right wing").GetComponent<Renderer>() };
            root.AddComponent<LODGroup>().SetLODs(new[] { new LOD(.008f, renderers.ToArray()), new LOD(.00005f, distantRenderers) });
            root.GetComponent<LODGroup>().RecalculateBounds();
            aircraft.Initialize(data, player, team, speed);
            var weapons = root.AddComponent<AircraftWeaponSystem>(); weapons.Initialize(aircraft, weaponData);
            root.AddComponent<AircraftEffects>().Initialize(aircraft, damage);
            root.AddComponent<AircraftAudio>().Initialize(aircraft, weapons);
            if (player) root.AddComponent<AircraftInput>();
            return aircraft;
        }

        static void DamageVolume(string name, Transform parent, Vector3 position, Vector3 size, AircraftDamage owner, DamageZoneType type)
        {
            var volume = new GameObject(name); volume.transform.SetParent(parent, false); volume.transform.localPosition = position;
            volume.AddComponent<BoxCollider>().size = size;
            var zone = volume.AddComponent<DamageZone>(); zone.Owner = owner; zone.Type = type;
        }

        static Transform ControlSurface(string name, Transform parent, Vector3 hinge, Vector3 offset, Vector3 size, Material skin, AircraftDamage owner, DamageZoneType type)
        {
            var pivot = new GameObject(name + " hinge").transform; pivot.SetParent(parent, false); pivot.localPosition = hinge;
            Part(name, PrimitiveType.Cube, pivot, offset, size, skin, owner, type);
            return pivot;
        }

        static Transform Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, AircraftDamage damage = null, DamageZoneType zoneType = DamageZoneType.Fuselage)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (damage) { var zone = go.AddComponent<DamageZone>(); zone.Owner = damage; zone.Type = zoneType; }
            else { var collider = go.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider); }
            return go.transform;
        }

        static Mesh WingMesh(int side, bool zero)
        {
            float span = zero ? 5.95f : 5.6f;
            Vector3[] outline = {
                new Vector3(.45f, 0, 1.65f), new Vector3(span * .8f, .12f, .7f), new Vector3(span, .15f, zero ? .15f : .55f),
                new Vector3(span, .15f, -.25f), new Vector3(span * .8f, .12f, -.75f), new Vector3(.45f, 0, -1.3f)
            };
            var vertices = new Vector3[12];
            for (int i = 0; i < 6; i++) { var v = outline[i]; v.x *= side; vertices[i] = v + Vector3.down * .02f; vertices[i + 6] = v + Vector3.down * .18f; }
            var triangles = new List<int>();
            for (int i = 1; i < 5; i++) { triangles.AddRange(new[] {0, i, i+1, 6, i+7, i+6}); }
            for (int i = 0; i < 6; i++) { int next = (i + 1) % 6; triangles.AddRange(new[] { i, i+6, next, next, i+6, next+6 }); }
            if (side < 0) for (int i = 0; i < triangles.Count; i += 3) { int temp = triangles[i]; triangles[i] = triangles[i+1]; triangles[i+1] = temp; }
            var mesh = new Mesh { name = zero ? "Zero wing" : "Mustang wing", vertices = vertices, triangles = triangles.ToArray() }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
