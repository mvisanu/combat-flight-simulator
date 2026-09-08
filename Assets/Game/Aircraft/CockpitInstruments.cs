using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PacificCombat
{
    /// <summary>Physical cockpit instruments driven by the same telemetry as the flight HUD.</summary>
    public sealed class CockpitInstruments : MonoBehaviour
    {
        public static readonly Vector3 EyePosition = new Vector3(0, 1.28f, -.12f);
        public bool IsVisible => instrumentRoot && instrumentRoot.gameObject.activeSelf;
        public float IndicatedAirspeed { get; private set; }
        public float IndicatedAltitude { get; private set; }
        public float IndicatedRPM { get; private set; }
        public float IndicatedFuel { get; private set; }
        public float IndicatedHeading { get; private set; }
        public float IndicatedClimb { get; private set; }
        AircraftController aircraft;
        Transform instrumentRoot, horizon;
        readonly Transform[] needles = new Transform[10];
        readonly List<Object> owned = new List<Object>();
        Material ivory, orange, bezel, sky, earth;
        static readonly Color32 Ink = new Color32(15, 20, 20, 255);
        static readonly Color32 Mark = new Color32(225, 224, 192, 255);

        public void Initialize(AircraftController owner)
        {
            aircraft = owner;
            instrumentRoot = new GameObject("Working cockpit instruments").transform;
            instrumentRoot.SetParent(transform, false);
            ivory = Solid("Instrument luminous markings", new Color(.86f, .88f, .68f), true);
            orange = Solid("Instrument warning markings", new Color(.94f, .56f, .19f), true);
            bezel = Solid("Instrument machined bezels", new Color(.065f, .075f, .07f), false);
            sky = Solid("Attitude sky", new Color(.24f, .47f, .65f), true);
            earth = Solid("Attitude earth", new Color(.38f, .24f, .13f), true);
            bool metric = owner.Data.Type == AircraftType.A6MZero || owner.Data.Type == AircraftType.Bf109;
            needles[0] = Dial("IAS", metric ? "KM H" : "MPH", new Vector2(-.32f, .785f), .09f, 0, metric ? 800 : 500, 5, metric ? 650 : 400);
            Attitude(new Vector2(-.105f, .785f), .09f);
            needles[1] = Dial("ALT", metric ? "1000 M" : "1000 FT", new Vector2(.105f, .785f), .09f, 0, metric ? 12 : 30, 6, -1);
            needles[2] = Dial("HDG", "", new Vector2(.32f, .785f), .09f, 0, 360, 4, -1, true);
            needles[3] = Dial("RPM", "X100", new Vector2(-.27f, .54f), .102f, 0, 35, 7, 30);
            needles[4] = Dial("FUEL", "PERCENT", new Vector2(0, .54f), .102f, 0, 100, 4, -1);
            needles[5] = Dial("CLIMB", "1000 FPM", new Vector2(.27f, .54f), .102f, -6, 6, 6, -1);
            needles[6] = Dial("OIL", "C", new Vector2(-.32f,.32f), .065f, 0, 150, 3, 110);
            needles[7] = Dial("TEMP", "C", new Vector2(-.105f,.32f), .065f, 0, 200, 4, 130);
            needles[8] = Dial("BOOST", metric ? "ATA" : "IN HG", new Vector2(.105f,.32f), .065f, 0, metric ? 2 : 60, 4, -1);
            needles[9] = Dial("OIL P", "BAR", new Vector2(.32f,.32f), .065f, 0, 8, 4, -1);
            SetVisible(false);
            Refresh(1f);
        }

        public void SetVisible(bool visible)
        {
            if (instrumentRoot) instrumentRoot.gameObject.SetActive(visible);
            if (visible) Refresh(1f);
        }

        void LateUpdate()
        {
            if (IsVisible) Refresh(1f - Mathf.Exp(-Time.unscaledDeltaTime * 9f));
        }

        public void Refresh(float response = 1f)
        {
            if (!aircraft || !aircraft.Physics || !aircraft.Engine) return;
            // Density correction gives the cockpit airspeed indicator IAS; the HUD
            // retains true airspeed, so stall reference speeds remain useful here.
            IndicatedAirspeed = aircraft.Physics.Airspeed * Mathf.Sqrt(Mathf.Max(.01f, aircraft.Physics.AirDensity) / 1.225f) * 2.23694f;
            IndicatedAltitude = aircraft.Physics.Altitude * 3.28084f;
            IndicatedRPM = aircraft.Engine.RPM;
            IndicatedFuel = aircraft.Engine.Fuel ? aircraft.Engine.Fuel.FuelFraction * 100f : 0;
            IndicatedHeading = Mathf.Repeat(aircraft.transform.eulerAngles.y, 360f);
            IndicatedClimb = aircraft.Body.linearVelocity.y * 196.8504f;
            bool metric = aircraft.Data.Type == AircraftType.A6MZero || aircraft.Data.Type == AircraftType.Bf109;
            Point(needles[0], metric ? IndicatedAirspeed * 1.609344f : IndicatedAirspeed, 0, metric ? 800 : 500, response);
            Point(needles[1], IndicatedAltitude / 1000f * (metric ? .3048f : 1), 0, metric ? 12 : 30, response);
            if (needles[2]) needles[2].localRotation = Quaternion.Slerp(needles[2].localRotation, Quaternion.Euler(0, 0, -IndicatedHeading), response);
            Point(needles[3], IndicatedRPM, 0, 3500, response);
            Point(needles[4], IndicatedFuel, 0, 100, response);
            Point(needles[5], IndicatedClimb / 1000f, -6, 6, response);
            Point(needles[6], aircraft.Engine.OilTemperatureC, 0, 150, response);
            Point(needles[7], aircraft.Engine.CoolantTemperatureC, 0, 200, response);
            Point(needles[8], aircraft.Engine.ManifoldPressureInHg * (metric ? .033421f : 1), 0, metric ? 2 : 60, response);
            Point(needles[9], aircraft.Engine.OilPressureBar, 0, 8, response);
            if (horizon)
            {
                float pitch = Mathf.Asin(Mathf.Clamp(aircraft.transform.forward.y, -1, 1)) * Mathf.Rad2Deg;
                float bank = Mathf.Atan2(aircraft.transform.right.y, aircraft.transform.up.y) * Mathf.Rad2Deg;
                horizon.localRotation = Quaternion.Slerp(horizon.localRotation, Quaternion.Euler(-pitch, 0, -bank), response);
            }
        }

        static void Point(Transform needle, float value, float min, float max, float response)
        {
            if (!needle) return;
            float angle = Mathf.Lerp(-135f, 135f, Mathf.InverseLerp(min, max, value));
            needle.localRotation = Quaternion.Slerp(needle.localRotation, Quaternion.Euler(0, 0, -angle), response);
        }

        Transform Dial(string label, string units, Vector2 position, float radius, float min, float max, int divisions, float redline, bool compass = false)
        {
            var dial = DialRoot(label, position, radius);
            var texture = Face(label, units, min, max, divisions, redline, compass);
            var material = Solid(label + " dial", Color.white, true);
            material.SetTexture("_BaseMap", texture);
            MeshObject("Printed dial face", dial, Disc(radius, 0), material);
            var pivot = new GameObject(label + " needle").transform;
            pivot.SetParent(dial, false);
            pivot.localPosition = new Vector3(0, 0, -.008f);
            var mesh = new Mesh { name = label + " needle mesh" };
            float width = radius * .034f;
            mesh.vertices = new[] { new Vector3(-width, -radius * .15f, 0), new Vector3(width, -radius * .15f, 0), new Vector3(width * .65f, radius * .63f, 0), new Vector3(0, radius * .78f, 0), new Vector3(-width * .65f, radius * .63f, 0) };
            mesh.triangles = new[] { 0, 2, 1, 0, 4, 2, 4, 3, 2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            owned.Add(mesh);
            MeshObject("Needle", pivot, mesh, ivory);
            MeshObject("Needle spindle", dial, Disc(radius * .065f, -.011f), bezel);
            return pivot;
        }

        Transform DialRoot(string label, Vector2 position, float radius)
        {
            var dial = new GameObject(label + " instrument").transform;
            dial.SetParent(instrumentRoot, false);
            dial.localPosition = new Vector3(position.x, position.y, .775f);
            MeshObject("Bevelled instrument bezel", dial, Ring(radius * .975f, radius * 1.105f), bezel);
            return dial;
        }

        void Attitude(Vector2 position, float radius)
        {
            var dial = DialRoot("ATTITUDE", position, radius);
            var flattened = new GameObject("Attitude gyro enclosure").transform;
            flattened.SetParent(dial, false);
            flattened.localScale = new Vector3(1, 1, .19f);
            horizon = new GameObject("Attitude gyro").transform;
            horizon.SetParent(flattened, false);
            var mesh = HemisphereSphere(radius * .98f);
            var display = MeshObject("Sky and earth", horizon, mesh, sky);
            display.sharedMaterials = new[] { sky, earth };
            Bar(dial, new Vector3(-radius * .34f, 0, -.024f), new Vector2(radius * .5f, radius * .035f), orange);
            Bar(dial, new Vector3(radius * .34f, 0, -.024f), new Vector2(radius * .5f, radius * .035f), orange);
            Bar(dial, new Vector3(0, -radius * .06f, -.024f), new Vector2(radius * .035f, radius * .14f), orange);
            for (int i = -3; i <= 3; i++)
            {
                float angle = i * 20 * Mathf.Deg2Rad;
                var mark = Bar(dial, new Vector3(Mathf.Sin(angle) * radius * .85f, Mathf.Cos(angle) * radius * .85f, -.025f), new Vector2(radius * .025f, radius * .12f), ivory);
                mark.localRotation = Quaternion.Euler(0, 0, -i * 20);
            }
            var label = new Texture2D(128, 24, TextureFormat.RGBA32, false);
            var pixels = new Color32[128 * 24];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Ink;
            Print(pixels, 128, 24, "ATTITUDE", 64, 8, 2, Mark);
            label.SetPixels32(pixels); label.Apply(false, true); owned.Add(label);
            var material = Solid("Attitude label", Color.white, true); material.SetTexture("_BaseMap", label);
            var plate = MeshObject("Attitude label", dial, Quad(1), material).transform;
            plate.localPosition = new Vector3(0, -radius * .74f, -.026f);
            plate.localScale = new Vector3(radius * .62f, radius * .115f, 1);
        }

        Transform Bar(Transform parent, Vector3 position, Vector2 size, Material material)
        {
            var renderer = MeshObject("Reference mark", parent, Quad(1), material);
            renderer.transform.localPosition = position;
            renderer.transform.localScale = new Vector3(size.x * .5f, size.y * .5f, 1);
            return renderer.transform;
        }

        Material Solid(string label, Color color, bool unlit)
        {
            var material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.name = label;
            material.SetColor("_BaseColor", color);
            if (!unlit) { material.SetFloat("_Metallic", .55f); material.SetFloat("_Smoothness", .48f); }
            material.enableInstancing = true;
            owned.Add(material);
            return material;
        }

        MeshRenderer MeshObject(string label, Transform parent, Mesh mesh, Material material)
        {
            var item = new GameObject(label);
            item.transform.SetParent(parent, false);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        Mesh Quad(float radius)
        {
            var mesh = new Mesh { name = "Cockpit face quad" };
            mesh.vertices = new[] { new Vector3(-radius, -radius, 0), new Vector3(radius, -radius, 0), new Vector3(radius, radius, 0), new Vector3(-radius, radius, 0) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh);
            return mesh;
        }

        Mesh Disc(float radius, float depth)
        {
            const int count = 32;
            var vertices = new Vector3[count + 1];
            var uv = new Vector2[count + 1];
            var triangles = new int[count * 3];
            vertices[0] = new Vector3(0, 0, depth);
            uv[0] = new Vector2(.5f, .5f);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                vertices[i + 1] = new Vector3(Mathf.Sin(a) * radius, Mathf.Cos(a) * radius, depth);
                uv[i + 1] = new Vector2(.5f + Mathf.Sin(a) * .5f, .5f + Mathf.Cos(a) * .5f);
                triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = (i + 1) % count + 1;
            }
            var mesh = new Mesh { name = "Instrument disc", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh); return mesh;
        }

        Mesh Ring(float inner, float outer)
        {
            const int count = 64;
            var vertices = new Vector3[count * 4];
            var triangles = new int[count * 18];
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count;
                var direction = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0);
                vertices[i * 4] = direction * inner + Vector3.back * .012f;
                vertices[i * 4 + 1] = direction * (outer * .975f) + Vector3.back * .019f;
                vertices[i * 4 + 2] = direction * outer + Vector3.back * .012f;
                vertices[i * 4 + 3] = direction * outer + Vector3.forward * .01f;
                for (int band = 0; band < 3; band++)
                {
                    int a = i * 4 + band, b = ((i + 1) % count) * 4 + band, t = i * 18 + band * 6;
                    triangles[t] = a; triangles[t + 1] = a + 1; triangles[t + 2] = b;
                    triangles[t + 3] = b; triangles[t + 4] = a + 1; triangles[t + 5] = b + 1;
                }
            }
            var mesh = new Mesh { name = "Machined instrument rim", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh); return mesh;
        }

        Mesh HemisphereSphere(float radius)
        {
            const int rings = 16, segments = 40;
            var vertices = new Vector3[(rings + 1) * (segments + 1)];
            var upper = new List<int>(); var lower = new List<int>();
            for (int row = 0; row <= rings; row++)
            {
                float latitude = row * Mathf.PI / rings;
                for (int col = 0; col <= segments; col++)
                {
                    float longitude = col * Mathf.PI * 2 / segments;
                    vertices[row * (segments + 1) + col] = new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude)) * radius;
                    if (row == rings || col == segments) continue;
                    int a = row * (segments + 1) + col, b = a + segments + 1;
                    var indices = row < rings / 2 ? upper : lower;
                    indices.Add(a); indices.Add(a + 1); indices.Add(b);
                    indices.Add(a + 1); indices.Add(b + 1); indices.Add(b);
                }
            }
            var mesh = new Mesh { name = "Attitude gyro sphere", vertices = vertices, subMeshCount = 2 };
            mesh.SetTriangles(upper, 0); mesh.SetTriangles(lower, 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh); return mesh;
        }

        Texture2D Face(string label, string units, float min, float max, int divisions, float redline, bool compass)
        {
            const int size = 256;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                int noise = (x * 17 + y * 31) % 4;
                pixels[y * size + x] = new Color32((byte)(Ink.r + noise), (byte)(Ink.g + noise), (byte)(Ink.b + noise), 255);
            }
            int ticks = compass ? 36 : divisions * 5;
            for (int i = 0; i <= ticks; i++)
            {
                float fraction = i / (float)ticks;
                float degrees = compass ? fraction * 360 : Mathf.Lerp(-135, 135, fraction);
                float angle = degrees * Mathf.Deg2Rad;
                bool major = compass ? i % 9 == 0 : i % 5 == 0;
                Color32 color = redline > 0 && Mathf.Lerp(min, max, fraction) >= redline ? new Color32(217, 92, 56, 255) : Mark;
                Line(pixels, size, size, 128 + Mathf.RoundToInt(Mathf.Sin(angle) * (major ? 91 : 99)), 128 + Mathf.RoundToInt(Mathf.Cos(angle) * (major ? 91 : 99)), 128 + Mathf.RoundToInt(Mathf.Sin(angle) * 111), 128 + Mathf.RoundToInt(Mathf.Cos(angle) * 111), color);
                if (major && (!compass || i < ticks))
                {
                    string number = Mathf.RoundToInt(Mathf.Lerp(min, max, fraction)).ToString();
                    if (compass) number = fraction < .125f ? "N" : fraction < .375f ? "E" : fraction < .625f ? "S" : "W";
                    Print(pixels, size, size, number, 128 + Mathf.RoundToInt(Mathf.Sin(angle) * 76), 122 + Mathf.RoundToInt(Mathf.Cos(angle) * 76), 2, color);
                }
            }
            Print(pixels, size, size, label, 128, 154, 2, Mark);
            Print(pixels, size, size, units, 128, 88, 2, Mark);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = label + " engraved face", filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, anisoLevel = 4 };
            texture.SetPixels32(pixels); texture.Apply(true, true); owned.Add(texture); return texture;
        }

        static void Line(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, error = dx + dy;
            while (true)
            {
                if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height) pixels[y0 * width + x0] = color;
                if (x0 == x1 && y0 == y1) break;
                int twice = 2 * error;
                if (twice >= dy) { error += dy; x0 += sx; }
                if (twice <= dx) { error += dx; y0 += sy; }
            }
        }

        // Compact built-in stencil lettering avoids a runtime font/atlas dependency.
        static void Print(Color32[] pixels, int width, int height, string text, int center, int bottom, int scale, Color32 color)
        {
            int left = center - (text.Length * 6 - 1) * scale / 2;
            for (int letter = 0; letter < text.Length; letter++)
            {
                string glyph = Glyph(text[letter]);
                for (int row = 0; row < 7; row++) for (int col = 0; col < 5; col++)
                    if (glyph[row * 5 + col] == '1')
                        for (int sy = 0; sy < scale; sy++) for (int sx = 0; sx < scale; sx++)
                        {
                            int x = left + (letter * 6 + col) * scale + sx, y = bottom + (6 - row) * scale + sy;
                            if (x >= 0 && x < width && y >= 0 && y < height) pixels[y * width + x] = color;
                        }
            }
        }

        static string Glyph(char character)
        {
            switch (character)
            {
                case '0': return "01110100011001110101110011000101110";
                case '1': return "00100011000010000100001000010001110";
                case '2': return "01110100010000100010001000100011111";
                case '3': return "11110000010000101110000010000111110";
                case '4': return "00010001100101010010111110001000010";
                case '5': return "11111100001000011110000010000111110";
                case '6': return "01110100001000011110100011000101110";
                case '7': return "11111000010001000100010000100001000";
                case '8': return "01110100011000101110100011000101110";
                case '9': return "01110100011000101111000010000101110";
                case 'A': return "01110100011000111111100011000110001";
                case 'B': return "11110100011000111110100011000111110";
                case 'C': return "01111100001000010000100001000001111";
                case 'D': return "11110100011000110001100011000111110";
                case 'E': return "11111100001000011110100001000011111";
                case 'F': return "11111100001000011110100001000010000";
                case 'G': return "01111100001000010111100011000101110";
                case 'H': return "10001100011000111111100011000110001";
                case 'I': return "01110001000010000100001000010001110";
                case 'L': return "10000100001000010000100001000011111";
                case 'M': return "10001110111010110101100011000110001";
                case 'N': return "10001110011010110011100011000110001";
                case 'O': return "01110100011000110001100011000101110";
                case 'P': return "11110100011000111110100001000010000";
                case 'R': return "11110100011000111110101001001010001";
                case 'S': return "01111100001000001110000010000111110";
                case 'T': return "11111001000010000100001000010000100";
                case 'U': return "10001100011000110001100011000101110";
                case 'V': return "10001100011000110001100010101000100";
                case 'W': return "10001100011000110101101011101110001";
                case 'X': return "10001100010101000100010101000110001";
                case '-': return "00000000000000011111000000000000000";
                default: return "00000000000000000000000000000000000";
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < owned.Count; i++) if (owned[i])
            {
                if (Application.isPlaying) Destroy(owned[i]); else DestroyImmediate(owned[i]);
            }
        }
    }
}
