using System.Collections.Generic;
using UnityEngine;

namespace PacificCombat
{
    public static class AircraftFactory
    {
        static Material silver, green, olive, camouflage, dark, glass, red, white, yellow, propellerBlur;
        static readonly GameObject[] aircraftArt = new GameObject[4];
        static Mesh ellipsoidCollider, propellerBlade;
        static void Materials()
        {
            if (silver) return;
            silver = PacificEnvironment.Material("Mustang aluminum", new Color(.64f, .69f, .69f), .72f, .66f);
            green = PacificEnvironment.Material("Zero navy green", new Color(.035f, .145f, .095f), .12f);
            olive = PacificEnvironment.Material("USAAF olive drab", new Color(.19f,.22f,.105f), .1f);
            camouflage = PacificEnvironment.Material("Luftwaffe grey green", new Color(.28f,.31f,.26f), .08f);
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
            var root = new GameObject(data.AircraftName + (player ? " — Player" : ""));
            root.transform.SetPositionAndRotation(position, rotation);
            var aircraft = root.AddComponent<AircraftController>();
            var damage = root.AddComponent<AircraftDamage>();
            damage.Initialize(aircraft);
            var animation = root.AddComponent<AircraftVisuals>(); animation.Aircraft = aircraft;
            animation.Ailerons = new Transform[2]; animation.Elevators = new Transform[2]; animation.Flaps = new Transform[2];
            animation.GeneratedMeshes = new Mesh[2];
            var visuals = new GameObject("Airframe").transform;
            visuals.SetParent(root.transform, false);
            if (data.Type == AircraftType.P38Lightning)
            {
                BuildLightning(visuals, damage, animation);
                return Finish(root, aircraft, damage, animation, visuals, data, player, team, speed, weaponData, null);
            }
            bool zero = data.Type == AircraftType.A6MZero;
            bool messerschmitt = data.Type == AircraftType.Bf109;
            Material skin = zero ? green : messerschmitt ? camouflage : silver;
            Part("Fuselage", PrimitiveType.Sphere, visuals, new Vector3(0, 0, -.1f), new Vector3(1.3f, 1.5f, zero ? 8.7f : messerschmitt ? 8.9f : 9.8f), skin, damage, DamageZoneType.Fuselage);
            Part("Engine cowling", PrimitiveType.Sphere, visuals, new Vector3(0, .07f, messerschmitt ? 2.9f : 3.15f), new Vector3(zero ? 1.75f : 1.2f, 1.45f, messerschmitt ? 2.1f : 2.4f), zero ? dark : skin, damage, DamageZoneType.Engine);
            Part("Spinner", PrimitiveType.Sphere, visuals, new Vector3(0, .05f, messerschmitt ? 4.15f : 4.5f), new Vector3(.72f, .72f, messerschmitt ? .6f : 1), zero ? skin : yellow, damage, DamageZoneType.Engine);
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
                animation.GeneratedMeshes[sideIndex] = WingMesh(side, data.Type);
                wing.AddComponent<MeshFilter>().sharedMesh = animation.GeneratedMeshes[sideIndex];
                wing.AddComponent<MeshRenderer>().sharedMaterial = skin;
                var box = wing.AddComponent<BoxCollider>();
                box.center = new Vector3(side * (data.WingSpan * .25f + .2f), -.1f, .6f);
                box.size = new Vector3(data.WingSpan * .5f - .45f, .23f, 1.45f);
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
            var prop = new GameObject("Propeller").transform; prop.SetParent(visuals, false); prop.localPosition = new Vector3(0, .05f, zero ? 4.02f : messerschmitt ? 3.995f : 4.25f);
            for (int p = 0; p < (zero || messerschmitt ? 3 : 4); p++)
            {
                Quaternion bladeRotation = Quaternion.Euler(0, 0, p * (zero || messerschmitt ? 120 : 90));
                CreatePropellerBlade(prop, bladeRotation);
            }
            animation.Propeller = prop;
            animation.PropellerBlur = Part("High RPM propeller disk", PrimitiveType.Cylinder, visuals, prop.localPosition, new Vector3(3.3f, .002f, 3.3f), propellerBlur);
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
            return Finish(root, aircraft, damage, animation, visuals, data, player, team, speed, weaponData, canopy.GetComponent<Renderer>());
        }

        static AircraftController Finish(GameObject root, AircraftController aircraft, AircraftDamage damage, AircraftVisuals animation, Transform visuals, AircraftData data, bool player, int team, float speed, WeaponData weaponData, Renderer canopy)
        {
            var renderers = new List<Renderer>(visuals.GetComponentsInChildren<Renderer>(true));
            var distant = new List<Renderer> { visuals.GetChild(0).GetComponent<Renderer>(), visuals.Find("Left wing").GetComponent<Renderer>(), visuals.Find("Right wing").GetComponent<Renderer>() };
            if (data.Type == AircraftType.P38Lightning)
            {
                distant.Add(visuals.Find("Left boom").GetComponent<Renderer>());
                distant.Add(visuals.Find("Right boom").GetComponent<Renderer>());
            }
            var distantRenderers = distant.ToArray();
            // Authored Blender art replaces the visible static airframe only. Existing
            // collider/damage volumes and all moving control surfaces retain their transforms.
            GameObject art = aircraftArt[(int)data.Type];
            if (!art)
            {
                art = Resources.Load<GameObject>(ArtPath(data.Type));
                aircraftArt[(int)data.Type] = art;
            }
            if (art)
            {
                var authored = Object.Instantiate(art, visuals, false);
                authored.name = "Detailed airframe";
                animation.DetailedAirframe = authored.transform;
                foreach (var collider in authored.GetComponentsInChildren<Collider>(true))
                { collider.enabled = false; if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider); }
                var nearRenderers = new List<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (IsAnimatedRenderer(renderer.transform, animation)) nearRenderers.Add(renderer);
                    else if (System.Array.IndexOf(distantRenderers, renderer) < 0) renderer.enabled = false;
                }
                nearRenderers.AddRange(authored.GetComponentsInChildren<Renderer>(true));
                renderers = nearRenderers;
            }
            if (canopy) animation.LegacyCanopy = canopy;
            root.AddComponent<LODGroup>().SetLODs(new[] { new LOD(.008f, renderers.ToArray()), new LOD(.00005f, distantRenderers) });
            root.GetComponent<LODGroup>().RecalculateBounds();
            aircraft.Initialize(data, player, team, speed);
            root.AddComponent<StructuralDebris>().Initialize(aircraft);
            if (player)
            {
                var cockpit = root.AddComponent<CockpitInstruments>();
                cockpit.Initialize(aircraft);
                animation.Cockpit = cockpit;
            }
            var weapons = root.AddComponent<AircraftWeaponSystem>(); weapons.Initialize(aircraft, weaponData);
            root.AddComponent<AircraftEffects>().Initialize(aircraft, damage);
            root.AddComponent<AircraftAudio>().Initialize(aircraft, weapons);
            if (player) root.AddComponent<AircraftInput>();
            return aircraft;
        }

        public static string ArtPath(AircraftType type)
        {
            switch (type)
            {
                case AircraftType.A6MZero: return "Art/A6MZero";
                case AircraftType.Bf109: return "Art/Bf109";
                case AircraftType.P38Lightning: return "Art/P38Lightning";
                default: return "Art/P51D";
            }
        }

        static void BuildLightning(Transform visuals, AircraftDamage damage, AircraftVisuals animation)
        {
            Part("Central cockpit pod", PrimitiveType.Sphere, visuals, new Vector3(0, .05f, .5f), new Vector3(1.4f, 1.55f, 6), silver, damage, DamageZoneType.Fuselage);
            var canopy = Part("Canopy", PrimitiveType.Sphere, visuals, new Vector3(0, .88f, .3f), new Vector3(1.1f, 1.08f, 2.5f), glass);
            animation.LegacyCanopy = canopy.GetComponent<Renderer>();
            DamageVolume("Cockpit frame volume", visuals, new Vector3(0, .84f, 1.05f), new Vector3(1.05f, .8f, .28f), damage, DamageZoneType.Cockpit);
            DamageVolume("Pilot volume", visuals, new Vector3(0, .94f, -.1f), new Vector3(.5f, .78f, .7f), damage, DamageZoneType.Pilot);
            animation.Propellers = new Transform[2]; animation.PropellerBlurs = new Transform[2]; animation.Rudders = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? -1 : 1;
                Part(i == 0 ? "Left boom" : "Right boom", PrimitiveType.Sphere, visuals, new Vector3(side * 2.65f, 0, -1), new Vector3(1.2f, 1.4f, 10.5f), silver);
                DamageVolume("Tail boom structure", visuals, new Vector3(side * 2.65f, -.05f, -2.2f), new Vector3(1.05f, 1.15f, 7.6f), damage, DamageZoneType.Fuselage);
                DamageVolume("Engine nacelle", visuals, new Vector3(side * 2.65f, .05f, 2.8f), new Vector3(1.2f, 1.3f, 3.1f), damage, DamageZoneType.Engine);
                DamageVolume("Wing fuel tank", visuals, new Vector3(side * 1.5f, -.18f, .15f), new Vector3(1.7f, .5f, 1.6f), damage, DamageZoneType.FuelTank);
                var wing = new GameObject(i == 0 ? "Left wing" : "Right wing"); wing.transform.SetParent(visuals, false);
                animation.GeneratedMeshes[i] = WingMesh(side, AircraftType.P38Lightning);
                wing.AddComponent<MeshFilter>().sharedMesh = animation.GeneratedMeshes[i]; wing.AddComponent<MeshRenderer>().sharedMaterial = silver;
                var collider = wing.AddComponent<BoxCollider>(); collider.center = new Vector3(side * 5.2f, -.1f, -1.35f); collider.size = new Vector3(5.3f, .23f, 1.1f);
                var zone = wing.AddComponent<DamageZone>(); zone.Owner = damage; zone.Type = i == 0 ? DamageZoneType.LeftWing : DamageZoneType.RightWing;
                DamageVolume("Inner wing structure", visuals, new Vector3(side * 1.55f, -.1f, -.2f), new Vector3(2.6f, .23f, 2.7f), damage, zone.Type);
                animation.Ailerons[i] = ControlSurface("Aileron", visuals, new Vector3(side * 5.6f, -.04f, -1.92f), new Vector3(0, 0, -.18f), new Vector3(2.7f, .085f, .36f), silver, damage, i == 0 ? DamageZoneType.LeftAileron : DamageZoneType.RightAileron);
                animation.Flaps[i] = ControlSurface("Flap", visuals, new Vector3(side * 1.5f, -.07f, -1.6f), new Vector3(0, 0, -.22f), new Vector3(1.6f, .085f, .44f), silver, damage, i == 0 ? DamageZoneType.LeftWing : DamageZoneType.RightWing);
                Part("Horizontal stabilizer", PrimitiveType.Cube, visuals, new Vector3(side * 1.325f, .18f, -5.32f), new Vector3(2.65f, .13f, .8f), silver, damage, DamageZoneType.HorizontalStabilizer);
                animation.Elevators[i] = ControlSurface("Elevator", visuals, new Vector3(side * 1.325f, .18f, -5.72f), new Vector3(0, 0, -.21f), new Vector3(2.65f, .1f, .42f), silver, damage, DamageZoneType.Elevator);
                Part("Vertical stabilizer", PrimitiveType.Sphere, visuals, new Vector3(side * 2.65f, .65f, -5.3f), new Vector3(.16f, 2.2f, 1.15f), silver, damage, DamageZoneType.VerticalStabilizer);
                animation.Rudders[i] = ControlSurface("Rudder", visuals, new Vector3(side * 2.65f, .7f, -5.85f), new Vector3(0, 0, -.22f), new Vector3(.13f, 1.75f, .44f), silver, damage, DamageZoneType.Rudder);
                var prop = new GameObject("Propeller " + (i + 1)).transform; prop.SetParent(visuals, false); prop.localPosition = new Vector3(side * 2.65f, .05f, 4.12f);
                for (int blade = 0; blade < 3; blade++)
                {
                    Quaternion rotation = Quaternion.Euler(0, 0, blade * 120);
                    CreatePropellerBlade(prop, rotation);
                }
                animation.Propellers[i] = prop;
                var blur = Part("High RPM propeller disk", PrimitiveType.Cylinder, visuals, prop.localPosition, new Vector3(3.3f, .002f, 3.3f), propellerBlur);
                blur.localRotation = Quaternion.Euler(90, 0, 0); blur.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; blur.gameObject.SetActive(false);
                animation.PropellerBlurs[i] = blur;
            }
            // One nose strut and two nacelle main struts: the central pod has no propeller.
            animation.Gear = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var gear = new GameObject(i == 0 ? "Nose landing gear" : "Main landing gear").transform; gear.SetParent(visuals, false);
                gear.localPosition = i == 0 ? new Vector3(0, -.4f, 2.05f) : new Vector3(i == 1 ? -2.65f : 2.65f, -.35f, -.8f);
                Part("Gear strut", PrimitiveType.Cube, gear, new Vector3(0, -.6f, 0), new Vector3(.1f, 1.2f, .1f), silver);
                var wheel = Part("Wheel", PrimitiveType.Cylinder, gear, new Vector3(0, -1.2f, 0), new Vector3(.6f, .13f, .6f), dark); wheel.localRotation = Quaternion.Euler(0, 0, 90);
                animation.Gear[i] = gear; gear.gameObject.SetActive(false);
            }
        }

        static void CreatePropellerBlade(Transform parent, Quaternion rotation)
        {
            if (!propellerBlade)
            {
                // Rounded airfoil sections taper and twist toward the tip. Shared
                // by the whole roster; rotation/blur still use the existing hubs.
                float[] radius = { .18f, .35f, .65f, 1f, 1.3f, 1.52f, 1.64f, 1.67f };
                float[] chord = { .09f, .14f, .22f, .24f, .20f, .14f, .07f, .004f };
                const int sides = 12;
                var vertices = new Vector3[radius.Length * sides];
                var body = new List<int>(); var tips = new List<int>();
                for (int j = 0; j < radius.Length; j++)
                {
                    float twist = Mathf.Lerp(35, 8, radius[j] / 1.67f);
                    for (int k = 0; k < sides; k++)
                    {
                        float angle = k * Mathf.PI * 2 / sides;
                        vertices[j * sides + k] = new Vector3(0, radius[j], 0) +
                            Quaternion.Euler(0, twist, 0) * new Vector3(Mathf.Cos(angle) * chord[j] * .5f, 0, Mathf.Sin(angle) * chord[j] * .08f);
                        if (j == radius.Length - 1) continue;
                        int a = j * sides + k, b = j * sides + (k + 1) % sides;
                        var indices = j >= 5 ? tips : body;
                        indices.AddRange(new[] { a, a + sides, b + sides, a, b + sides, b });
                    }
                }
                for (int k = 1; k < sides - 1; k++)
                {
                    body.AddRange(new[] { 0, k, k + 1 });
                    int end = (radius.Length - 1) * sides;
                    tips.AddRange(new[] { end, end + k + 1, end + k });
                }
                propellerBlade = new Mesh { name = "Tapered twisted propeller blade", vertices = vertices, subMeshCount = 2 };
                propellerBlade.SetTriangles(body, 0); propellerBlade.SetTriangles(tips, 1);
                propellerBlade.RecalculateNormals(); propellerBlade.RecalculateBounds();
            }
            var blade = new GameObject("Propeller blade"); blade.transform.SetParent(parent, false);
            blade.transform.localRotation = rotation;
            blade.AddComponent<MeshFilter>().sharedMesh = propellerBlade;
            blade.AddComponent<MeshRenderer>().sharedMaterials = new[] { dark, yellow };
        }

        static bool IsAnimatedRenderer(Transform item, AircraftVisuals animation)
        {
            if ((animation.Propeller && item.IsChildOf(animation.Propeller)) || item == animation.PropellerBlur) return true;
            if (IsChildOfAny(item, animation.Propellers) || IsChildOfAny(item, animation.PropellerBlurs) || IsChildOfAny(item, animation.Rudders)) return true;
            if (animation.Rudder && item.IsChildOf(animation.Rudder)) return true;
            return IsChildOfAny(item, animation.Gear) || IsChildOfAny(item, animation.Ailerons)
                || IsChildOfAny(item, animation.Elevators) || IsChildOfAny(item, animation.Flaps);
        }

        static bool IsChildOfAny(Transform item, Transform[] roots)
        {
            if (roots == null) return false;
            for (int i = 0; i < roots.Length; i++) if (roots[i] && item.IsChildOf(roots[i])) return true;
            return false;
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
            if (damage)
            {
                // PhysX spheres use the largest scale axis in every direction. A long
                // fuselage therefore needs a convex hull to preserve its narrow cross section.
                if (type == PrimitiveType.Sphere)
                {
                    var sphere = go.GetComponent<Collider>(); sphere.enabled = false;
                    if (Application.isPlaying) Object.Destroy(sphere); else Object.DestroyImmediate(sphere);
                    var hull = go.AddComponent<MeshCollider>(); hull.sharedMesh = EllipsoidCollider(); hull.convex = true;
                }
                var zone = go.AddComponent<DamageZone>(); zone.Owner = damage; zone.Type = zoneType;
            }
            else { var collider = go.GetComponent<Collider>(); collider.enabled = false; if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider); }
            return go.transform;
        }

        static Mesh EllipsoidCollider()
        {
            if (ellipsoidCollider) return ellipsoidCollider;
            // Low-resolution unit sphere keeps the convex hull below PhysX's face limit.
            var vertices = new List<Vector3> { Vector3.up * .5f, Vector3.down * .5f };
            const int slices = 12, rings = 7;
            for (int ring = 1; ring <= rings; ring++)
            {
                float latitude = Mathf.PI * ring / (rings + 1);
                for (int slice = 0; slice < slices; slice++)
                {
                    float longitude = 2 * Mathf.PI * slice / slices;
                    vertices.Add(new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude)) * .5f);
                }
            }
            var triangles = new List<int>();
            for (int slice = 0; slice < slices; slice++)
            {
                int next = (slice + 1) % slices;
                triangles.AddRange(new[] { 0, 2 + next, 2 + slice, 1, 2 + (rings - 1) * slices + slice, 2 + (rings - 1) * slices + next });
                for (int ring = 0; ring < rings - 1; ring++)
                {
                    int a = 2 + ring * slices + slice, b = 2 + ring * slices + next;
                    triangles.AddRange(new[] { a, b, a + slices, b, b + slices, a + slices });
                }
            }
            ellipsoidCollider = new Mesh { name = "Unit ellipsoid collision hull" };
            ellipsoidCollider.SetVertices(vertices); ellipsoidCollider.SetTriangles(triangles, 0); ellipsoidCollider.RecalculateBounds();
            return ellipsoidCollider;
        }

        static Mesh WingMesh(int side, AircraftType type)
        {
            bool zero = type == AircraftType.A6MZero;
            float span = type == AircraftType.P38Lightning ? 7.9f : type == AircraftType.Bf109 ? 4.95f : zero ? 5.95f : 5.6f;
            Vector3[] outline = {
                new Vector3(.45f, 0, 1.65f), new Vector3(span * .8f, .12f, .7f), new Vector3(span, .15f, zero ? .15f : .55f),
                new Vector3(span, .15f, -.25f), new Vector3(span * .8f, .12f, -.75f), new Vector3(.45f, 0, -1.3f)
            };
            if (type == AircraftType.P38Lightning)
                outline = new[] { new Vector3(.45f, 0, 1.45f), new Vector3(3, .12f, .7f), new Vector3(span, .15f, -.9f), new Vector3(span, .15f, -2), new Vector3(3, .12f, -1.9f), new Vector3(.45f, 0, -1.8f) };
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
