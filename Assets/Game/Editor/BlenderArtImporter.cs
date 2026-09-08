using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PacificCombat.Editor
{
    /// <summary>Deterministic Blender MCP mesh bridge; source stays editable in ArtSource.</summary>
    public static class BlenderArtImporter
    {
        public static void BuildArtwork() { Import(); ArtAcceptance.Run(); ProjectBuilder.Build(); }
        [Serializable] sealed class Document { public string name; public Surface[] materials; public Node[] nodes; }
        [Serializable] sealed class Surface { public string name; public float[] color; public float metallic, smoothness; }
        [Serializable] sealed class Node { public string name, material; public float[] vertices, normals, uv; public int[] triangles; }
        [MenuItem("Pacific Combat/Import Blender artwork")]
        public static void Import()
        {
            const string destination = "Assets/Game/Resources/Art";
            Directory.CreateDirectory(destination);
            AssetDatabase.Refresh();
            foreach (string name in new[] { "P51D", "A6MZero", "Bf109", "P38Lightning", "VolcanicIsland" })
            {
                string source = "ArtSource/" + name + ".json";
                if (!File.Exists(source)) throw new FileNotFoundException("Run Tools/author_blender_art.py through Blender MCP first.", source);
                var document = JsonUtility.FromJson<Document>(File.ReadAllText(source));
                string folder = destination + "/" + name + "Assets";
                Directory.CreateDirectory(folder); AssetDatabase.Refresh();
                var materials = new Dictionary<string, Material>();
                foreach (var surface in document.materials)
                {
                    string path = folder + "/" + surface.name.Replace(' ', '_') + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
                    material.name = surface.name; material.enableInstancing = true;
                    material.SetColor("_BaseColor", new Color(surface.color[0], surface.color[1], surface.color[2], surface.color[3]));
                    material.SetFloat("_Metallic", surface.metallic); material.SetFloat("_Smoothness", surface.smoothness);
                    if (surface.name.Contains("aluminum") || surface.name.Contains("weathered") || surface.name.Contains("paint"))
                    {
                        string wearPath = folder + "/" + surface.name.Replace(" ", "_") + "_wear.asset";
                        var wear = AssetDatabase.LoadAssetAtPath<Texture2D>(wearPath);
                        if (!wear)
                        {
                            wear = new Texture2D(256,256,TextureFormat.RGBA32,true) { name = "Authored paint wear", wrapMode = TextureWrapMode.Repeat };
                            var pixels = new Color32[256*256]; var random = new System.Random(1944);
                            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                            {
                                float grain = .92f + (float)random.NextDouble()*.08f;
                                if (x % 83 == 0 && random.NextDouble() > .7) grain = .67f;
                                byte value = (byte)(grain*255); pixels[y*256+x] = new Color32(value,value,value,255);
                            }
                            wear.SetPixels32(pixels); wear.Apply(true,false); AssetDatabase.CreateAsset(wear,wearPath);
                        }
                        material.SetTexture("_BaseMap",wear);
                    }
                    if (surface.color[3] < 1)
                    {
                        material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
                        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
                    }
                    EditorUtility.SetDirty(material); materials.Add(surface.name, material);
                }
                var root = new GameObject(document.name);
                try
                {
                    foreach (var node in document.nodes)
                    {
                        string path = folder + "/" + node.name + ".asset";
                        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if (!mesh) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
                        mesh.Clear(); mesh.name = node.name; mesh.indexFormat = IndexFormat.UInt32;
                        var vertices = new Vector3[node.vertices.Length / 3]; var normals = new Vector3[vertices.Length]; var uv = new Vector2[vertices.Length];
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            vertices[i] = new Vector3(node.vertices[i*3], node.vertices[i*3+1], node.vertices[i*3+2]);
                            normals[i] = new Vector3(node.normals[i*3], node.normals[i*3+1], node.normals[i*3+2]);
                            uv[i] = new Vector2(node.uv[i*2], node.uv[i*2+1]);
                        }
                        mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = node.triangles;
                        mesh.RecalculateBounds(); mesh.RecalculateTangents(); EditorUtility.SetDirty(mesh);
                        var child = new GameObject(node.name); child.transform.SetParent(root.transform, false);
                        child.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var renderer = child.AddComponent<MeshRenderer>(); renderer.sharedMaterial = materials[node.material];
                        if (renderer.sharedMaterial.renderQueue >= 3000) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, destination + "/" + name + ".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("BLENDER ART IMPORT PASSED: four aircraft and volcanic island prefabs.");
        }
    }
}
