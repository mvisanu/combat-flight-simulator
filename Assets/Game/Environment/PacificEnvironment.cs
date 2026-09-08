using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PacificCombat
{
    public sealed class PacificEnvironment : MonoBehaviour
    {
        // Only assets created by this world are owned here; primitive meshes and aircraft
        // materials returned by the shared factory remain owned by their respective systems.
        readonly List<Object> generatedAssets = new List<Object>(16);
        Material skyMaterial;
        bool built;
        static PacificEnvironment active;
        static int cloudQuality = 2;
        WeatherPreset weather;
        readonly List<Renderer> cloudRenderers = new List<Renderer>();
        readonly List<Renderer> terrainRenderers = new List<Renderer>();
        public static Vector3 WindVelocity(Vector3 position)
        {
            if (!active || active.weather != WeatherPreset.Squall) return Vector3.zero;
            return new Vector3(9 + Mathf.Sin(Time.time * .23f + position.z * .001f) * 2, Mathf.Sin(Time.time * .4f) * .7f, 3);
        }
        public static void SetCloudQuality(int quality)
        {
            cloudQuality = Mathf.Clamp(quality, 0, 3);
            if (!active) return;
            foreach (var renderer in active.cloudRenderers)
            {
                renderer.enabled = true;
                if (renderer.sharedMaterial.HasProperty("_Steps")) renderer.sharedMaterial.SetFloat("_Steps", cloudQuality == 0 ? 8 : cloudQuality == 1 ? 12 : cloudQuality == 2 ? 24 : 40);
            }
        }
        public static void SetTerrainQuality(int quality)
        {
            if (!active) return;
            foreach (var renderer in active.terrainRenderers)
                renderer.shadowCastingMode = quality > 1 ? ShadowCastingMode.On : ShadowCastingMode.Off;
            QualitySettings.lodBias = Mathf.Lerp(.65f, 2, Mathf.Clamp01(quality / 3f));
        }
        public static void SetEffectsQuality(int quality) { AircraftEffects.Quality = Mathf.Clamp(quality, 0, 3); }

        public static Material Material(string name, Color color, float metallic = 0, float smoothness = .35f)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        public void Build(WeatherPreset preset = WeatherPreset.Scattered)
        {
            if (built) return;
            built = true;
            active = this; weather = preset;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.55f, .71f, .77f);
            RenderSettings.fogDensity = .000035f;
            if (weather == WeatherPreset.Overcast || weather == WeatherPreset.Squall)
            { RenderSettings.fogDensity = weather == WeatherPreset.Squall ? .00013f : .00007f; RenderSettings.fogColor = new Color(.44f,.51f,.55f); }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.56f, .69f, .8f);
            RenderSettings.ambientEquatorColor = new Color(.32f, .43f, .48f);
            RenderSettings.ambientGroundColor = new Color(.16f, .24f, .29f);
            var sun = new GameObject("Pacific sunlight").AddComponent<Light>();
            sun.transform.SetParent(transform);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(32, -35, 0);
            sun.color = new Color(1, .92f, .78f);
            sun.intensity = 2.1f;
            if (weather == WeatherPreset.Overcast || weather == WeatherPreset.Squall) sun.intensity = .75f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            var skyShader = Shader.Find("PacificCombat/Atmosphere");
            if (skyShader)
            {
                skyMaterial = new Material(skyShader) { name = "Pacific maritime atmosphere" };
                skyMaterial.SetColor("_Zenith", new Color(.20f, .39f, .66f));
                skyMaterial.SetColor("_Horizon", RenderSettings.fogColor);
                skyMaterial.SetColor("_SunColor", sun.color);
                skyMaterial.SetVector("_SunDirection", -sun.transform.forward);
                RenderSettings.skybox = skyMaterial;
                generatedAssets.Add(skyMaterial);
                DynamicGI.UpdateEnvironment();
            }

            var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = "Pacific Ocean";
            ocean.transform.SetParent(transform);
            ocean.transform.localScale = new Vector3(12000, 1, 12000);
            var oceanShader = Shader.Find("PacificCombat/Ocean");
            var oceanMaterial = oceanShader ? new Material(oceanShader) : Material("Ocean", new Color(.025f, .22f, .3f), .3f, .85f);
            generatedAssets.Add(oceanMaterial);
            ocean.GetComponent<Renderer>().sharedMaterial = oceanMaterial;
            ocean.AddComponent<WaterSurface>();
            BuildAirfield();

            var land = new Material(Shader.Find("PacificCombat/Terrain")) { name = "Jungle basalt and coastal sand" };
            var beach = new Material(Shader.Find("PacificCombat/Shallows")) { name = "Coral lagoons and surf" };
            generatedAssets.Add(land); generatedAssets.Add(beach);
            var random = new System.Random(1944);
            var authored = Resources.Load<GameObject>("Art/VolcanicIsland");
            var authoredFilter = authored ? authored.GetComponentInChildren<MeshFilter>() : null;
            for (int i = 0; i < 11; i++)
            {
                float x = (float)(random.NextDouble() - .5) * 24000;
                float z = (float)(random.NextDouble() - .5) * 24000;
                float radius = 850 + (float)random.NextDouble() * 1600;
                if (i == 0) { x = 3100; z = 5100; radius = 2400; }
                // Reserve the airfield and approach corridor before generating island relief.
                if (Mathf.Abs(x) < radius + 500 && Mathf.Abs(z + 3500) < radius + 2000) continue;
                var island = new GameObject("Island " + (i + 1));
                island.transform.SetParent(transform);
                island.transform.position = new Vector3(x, 0, z);
                float height = i == 0 ? 1050 : 350 + (float)random.NextDouble() * 800;
                var terrain = new GameObject("Volcanic relief"); terrain.transform.SetParent(island.transform, false);
                Mesh mesh;
                if (i == 0 && authoredFilter && authoredFilter.sharedMesh)
                {
                    mesh = authoredFilter.sharedMesh;
                    terrain.transform.localScale = new Vector3(radius, height / .35f, radius * .72f);
                }
                else { mesh = IslandMesh(radius, height, i); generatedAssets.Add(mesh); }
                terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
                var terrainRenderer = terrain.AddComponent<MeshRenderer>(); terrainRenderer.sharedMaterial = land;
                terrainRenderers.Add(terrainRenderer);
                terrain.AddComponent<MeshCollider>().sharedMesh = mesh;
                var shore = new GameObject("Fringing reef and lagoon");
                shore.transform.SetParent(island.transform, false);
                var reef = ReefMesh(radius, i); generatedAssets.Add(reef);
                shore.AddComponent<MeshFilter>().sharedMesh = reef;
                var shoreRenderer = shore.AddComponent<MeshRenderer>(); shoreRenderer.sharedMaterial = beach;
                shoreRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            var cloudMaterial = new Material(Shader.Find("PacificCombat/VolumeCloud")) { name = "Volumetric maritime cumulus" };
            generatedAssets.Add(cloudMaterial);
            int cloudCount = weather == WeatherPreset.Clear ? 0 : weather == WeatherPreset.Scattered ? 26 : 48;
            for (int c = 0; c < cloudCount; c++)
            {
                var group = GameObject.CreatePrimitive(PrimitiveType.Cube); group.name = "Cumulus volume " + c;
                Destroy(group.GetComponent<Collider>());
                group.transform.SetParent(transform);
                group.transform.position = new Vector3((float)(random.NextDouble() - .5) * 35000, 1200 + (float)random.NextDouble() * 1600, (float)(random.NextDouble() - .5) * 35000);
                var visibility = group.AddComponent<EnvironmentVisibility>();
                group.transform.localScale = new Vector3(1400, 500, 1100);
                visibility.Radius = Vector3.one * .5f;
                visibility.Extinction = .009f;
                var renderer = group.GetComponent<MeshRenderer>(); renderer.sharedMaterial = cloudMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                cloudRenderers.Add(renderer);
            }
            SetCloudQuality(cloudQuality);
        }

        void BuildAirfield()
        {
            var sand = Material("Airfield coral", new Color(.49f,.46f,.33f)); generatedAssets.Add(sand);
            var tarmac = Material("Weathered runway", new Color(.13f,.15f,.15f)); generatedAssets.Add(tarmac);
            var paint = Material("Runway markings", new Color(.88f,.86f,.7f)); generatedAssets.Add(paint);
            Block("Flat coral airfield", new Vector3(0,3,-3500), new Vector3(450,8,2800), sand, true);
            Block("Runway 36", new Vector3(0,7.5f,-3500), new Vector3(65,1,2400), tarmac, true);
            for (int i = 0; i < 23; i++) Block("Runway centerline", new Vector3(0,8.015f,-4600+i*100), new Vector3(1.5f,.015f,35), paint, false);
            for (int end = -1; end <= 1; end += 2)
                for (int stripe = -4; stripe <= 4; stripe++)
                    if (stripe != 0) Block("Threshold stripe", new Vector3(stripe*5,8.02f,-3500+end*1120), new Vector3(2,.02f,35), paint, false);
        }
        void Block(string label, Vector3 position, Vector3 scale, Material material, bool collision)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = label;
            block.transform.SetParent(transform,false); block.transform.localPosition = position; block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Destroy(block.GetComponent<Collider>());
        }

        void OnDestroy()
        {
            if (active == this) active = null;
            if (RenderSettings.skybox == skyMaterial) RenderSettings.skybox = null;
            for (int i = 0; i < generatedAssets.Count; i++)
            {
                if (!generatedAssets[i]) continue;
                if (Application.isPlaying) Destroy(generatedAssets[i]); else DestroyImmediate(generatedAssets[i]);
            }
            generatedAssets.Clear();
        }

        static float CoastEdge(float a, int seed) => 1 + .12f * Mathf.Sin(a * 3 + seed) + .065f * Mathf.Sin(a * 7 + seed * .3f) + .035f * Mathf.Sin(a * 13 + seed);

        static Mesh ReefMesh(float radius, int seed)
        {
            const int segments = 160, rings = 5;
            var vertices = new Vector3[rings * segments]; var uv = new Vector2[vertices.Length]; var triangles = new int[(rings - 1) * segments * 6];
            for (int r = 0; r < rings; r++) for (int s = 0; s < segments; s++)
            {
                float t = r / (float)(rings - 1), a = s * Mathf.PI * 2 / segments;
                float distance = radius * Mathf.Lerp(.82f, 1.26f, t) * CoastEdge(a, seed);
                vertices[r * segments + s] = new Vector3(Mathf.Cos(a) * distance, .25f, Mathf.Sin(a) * distance * .72f);
                uv[r * segments + s] = new Vector2(t, s / (float)segments);
                if (r == rings - 1) continue;
                int k = (r * segments + s) * 6, next = (s + 1) % segments;
                triangles[k] = r * segments + s; triangles[k + 1] = r * segments + next; triangles[k + 2] = (r + 1) * segments + s;
                triangles[k + 3] = r * segments + next; triangles[k + 4] = (r + 1) * segments + next; triangles[k + 5] = (r + 1) * segments + s;
            }
            var mesh = new Mesh { name = "Irregular coral shelf", vertices = vertices, uv = uv, triangles = triangles }; mesh.RecalculateBounds(); return mesh;
        }
        static Mesh CloudMesh(System.Random random)
        {
            const int count = 12;
            var vertices = new Vector3[count * 4]; var uv = new Vector2[vertices.Length]; var sizes = new Vector2[vertices.Length]; var triangles = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                Vector3 center = new Vector3((float)random.NextDouble() * 1300, (float)random.NextDouble() * 200, (float)random.NextDouble() * 600);
                float width = 480 + (float)random.NextDouble() * 420, height = width * (.55f + (float)random.NextDouble() * .3f);
                for (int corner = 0; corner < 4; corner++) { vertices[i * 4 + corner] = center; uv[i * 4 + corner] = new Vector2(corner & 1, corner >> 1); sizes[i * 4 + corner] = new Vector2(width, height); }
                int k = i * 6, v = i * 4; triangles[k] = v; triangles[k + 1] = v + 2; triangles[k + 2] = v + 1; triangles[k + 3] = v + 1; triangles[k + 4] = v + 2; triangles[k + 5] = v + 3;
            }
            return new Mesh { name = "Batched cumulus billboards", vertices = vertices, uv = uv, uv2 = sizes, triangles = triangles, bounds = new Bounds(new Vector3(650, 100, 300), new Vector3(2600, 1500, 1900)) };
        }
        static Texture2D CloudTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Fractal cloud density", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size, v = (y + .5f) / size;
                float noise = Mathf.PerlinNoise(u * 7 + 13, v * 7 + 2) * .65f + Mathf.PerlinNoise(u * 18 + 5, v * 18 + 9) * .25f + Mathf.PerlinNoise(u * 41, v * 41) * .1f;
                float radial = 1 - new Vector2((u - .5f) * 2, (v - .5f) * 2).magnitude;
                float alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01((radial + (noise - .5f) * .7f) * 4));
                pixels[y * size + x] = new Color(noise, noise, noise, alpha);
            }
            texture.SetPixels(pixels); texture.Apply(true, true); return texture;
        }
        static Mesh IslandMesh(float radius, float height, int seed)
        {
            const int rings = 64, segments = 160;
            var vertices = new Vector3[(rings + 1) * segments];
            var indices = new int[rings * segments * 6];
            for (int ring = 0; ring <= rings; ring++)
                for (int s = 0; s < segments; s++)
                {
                    float t = (float)ring / rings;
                    float a = s * Mathf.PI * 2 / segments;
                    float edge = CoastEdge(a, seed);
                    float x = Mathf.Cos(a) * radius * t * edge;
                    float z = Mathf.Sin(a) * radius * t * edge * .72f;
                    float noise = Mathf.PerlinNoise(x / 500 + seed * 20, z / 500 + 100);
                    float detail = Mathf.PerlinNoise(x / 130 + seed * 13, z / 130 + 42);
                    float cone = Mathf.Pow(1 - t, 1.7f);
                    float crater = .18f * Mathf.Exp(-t * t * 190);
                    float erosion = .09f * Mathf.Sin(a * 15 + noise * 7) * Mathf.Sin(t * Mathf.PI);
                    vertices[ring * segments + s] = new Vector3(x, height * (cone * (.65f + (1 - Mathf.Abs(noise * 2 - 1)) * .42f + detail * .12f) - crater + erosion * cone) - 8, z);
                    if (ring == rings) continue;
                    int k = (ring * segments + s) * 6;
                    int n = (s + 1) % segments;
                    indices[k] = ring * segments + s; indices[k + 1] = ring * segments + n; indices[k + 2] = (ring + 1) * segments + s;
                    indices[k + 3] = ring * segments + n; indices[k + 4] = (ring + 1) * segments + n; indices[k + 5] = (ring + 1) * segments + s;
                }
            var mesh = new Mesh { name = "Procedural island", vertices = vertices, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
