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
        bool built;

        public static Material Material(string name, Color color, float metallic = 0, float smoothness = .35f)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        public void Build()
        {
            if (built) return;
            built = true;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.55f, .71f, .77f);
            RenderSettings.fogDensity = .000035f;
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
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;

            var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = "Pacific Ocean";
            ocean.transform.SetParent(transform);
            ocean.transform.localScale = new Vector3(12000, 1, 12000);
            var oceanShader = Shader.Find("PacificCombat/Ocean");
            var oceanMaterial = oceanShader ? new Material(oceanShader) : Material("Ocean", new Color(.025f, .22f, .3f), .3f, .85f);
            generatedAssets.Add(oceanMaterial);
            ocean.GetComponent<Renderer>().sharedMaterial = oceanMaterial;

            var land = Material("Volcanic jungle", new Color(.16f, .28f, .16f));
            var beach = Material("Coral sand", new Color(.68f, .65f, .44f));
            generatedAssets.Add(land); generatedAssets.Add(beach);
            var random = new System.Random(1944);
            for (int i = 0; i < 11; i++)
            {
                float x = (float)(random.NextDouble() - .5) * 24000;
                float z = (float)(random.NextDouble() - .5) * 24000;
                float radius = 700 + (float)random.NextDouble() * 1800;
                var island = new GameObject("Island " + (i + 1));
                island.transform.SetParent(transform);
                island.transform.position = new Vector3(x, 0, z);
                var mesh = IslandMesh(radius, 120 + (float)random.NextDouble() * 650, i);
                generatedAssets.Add(mesh);
                island.AddComponent<MeshFilter>().sharedMesh = mesh;
                island.AddComponent<MeshRenderer>().sharedMaterial = land;
                island.AddComponent<MeshCollider>().sharedMesh = mesh;
                var shore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shore.name = "Sand shelf";
                shore.transform.SetParent(island.transform, false);
                shore.transform.localPosition = new Vector3(0, -3, 0);
                shore.transform.localScale = new Vector3(radius * 1.9f, 4, radius * 1.4f);
                shore.GetComponent<Renderer>().sharedMaterial = beach;
            }
            var cloudMaterial = Material("Cloud sunlit", new Color(.88f, .91f, .91f));
            generatedAssets.Add(cloudMaterial);
            for (int c = 0; c < 32; c++)
            {
                var group = new GameObject("Cumulus bank " + c);
                group.transform.SetParent(transform);
                group.transform.position = new Vector3((float)(random.NextDouble() - .5) * 35000, 1200 + (float)random.NextDouble() * 1600, (float)(random.NextDouble() - .5) * 35000);
                for (int p = 0; p < 5; p++)
                {
                    var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    puff.name = "Cloud lobe";
                    puff.transform.SetParent(group.transform, false);
                    puff.transform.localPosition = new Vector3(p * 180, (float)random.NextDouble() * 100, (float)random.NextDouble() * 150);
                    puff.transform.localScale = new Vector3(500, 150 + (float)random.NextDouble() * 220, 330);
                    var collider = puff.GetComponent<Collider>();
                    collider.enabled = false;
                    if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
                    var renderer = puff.GetComponent<Renderer>();
                    renderer.sharedMaterial = cloudMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < generatedAssets.Count; i++)
            {
                if (!generatedAssets[i]) continue;
                if (Application.isPlaying) Destroy(generatedAssets[i]); else DestroyImmediate(generatedAssets[i]);
            }
            generatedAssets.Clear();
        }

        static Mesh IslandMesh(float radius, float height, int seed)
        {
            const int rings = 24, segments = 96;
            var vertices = new Vector3[(rings + 1) * segments];
            var indices = new int[rings * segments * 6];
            for (int ring = 0; ring <= rings; ring++)
                for (int s = 0; s < segments; s++)
                {
                    float t = (float)ring / rings;
                    float a = s * Mathf.PI * 2 / segments;
                    float edge = 1 + .12f * Mathf.Sin(a * 3 + seed) + .08f * Mathf.Sin(a * 7);
                    float x = Mathf.Cos(a) * radius * t * edge;
                    float z = Mathf.Sin(a) * radius * t * edge * .72f;
                    float noise = Mathf.PerlinNoise(x / 500 + seed * 20, z / 500 + 100);
                    vertices[ring * segments + s] = new Vector3(x, Mathf.Pow(1 - t, 1.6f) * height * (.55f + noise) - 5, z);
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
