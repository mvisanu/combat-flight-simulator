using System;
using UnityEngine;

namespace PacificCombat.Editor
{
    public static class ArtAcceptance
    {
        public static void Run()
        {
            foreach (string name in new[] { "P51D", "A6MZero", "Bf109", "P38Lightning", "VolcanicIsland" })
            {
                var prefab = Resources.Load<GameObject>("Art/" + name);
                if (!prefab) throw new Exception("Missing Blender prefab: " + name);
                int triangles = 0; var bounds = new Bounds(Vector3.zero, Vector3.zero);
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = filter.sharedMesh;
                    if (!mesh || mesh.vertexCount < 3) throw new Exception("Empty authored mesh: " + name);
                    triangles += mesh.triangles.Length / 3; bounds.Encapsulate(mesh.bounds);
                    var positions = mesh.vertices; var normals = mesh.normals; var indices = mesh.triangles;
                    int reversed = 0;
                    for (int t = 0; t < indices.Length; t += 3)
                    {
                        int a = indices[t], b = indices[t+1], c = indices[t+2];
                        var face = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
                        if (Vector3.Dot(face, normals[a] + normals[b] + normals[c]) < -0.000001f) reversed++;
                    }
                    if (reversed > indices.Length / 3 * .02f) throw new Exception("Winding disagrees with normals: " + name + "/" + mesh.name);
                    foreach (var normal in mesh.normals)
                        if (!float.IsFinite(normal.x) || !float.IsFinite(normal.y) || !float.IsFinite(normal.z) || normal.sqrMagnitude < .8f)
                            throw new Exception("Invalid authored normal: " + name);
                    var material = filter.GetComponent<Renderer>().sharedMaterial;
                    if (!material || !material.shader || !material.shader.isSupported) throw new Exception("Invalid authored material: " + name);
                }
                if (triangles < 10000 || triangles > 85000) throw new Exception("Unexpected mesh budget: " + name + " " + triangles);
                float minSpan = name == "Bf109" ? 9 : name == "P38Lightning" ? 15 : 10;
                float maxSpan = name == "P38Lightning" ? 17 : 13;
                if (name != "VolcanicIsland" && (bounds.size.x < minSpan || bounds.size.x > maxSpan || bounds.size.z < 8 || bounds.size.z > 12))
                    throw new Exception("Incorrect aircraft scale: " + name + " " + bounds.size);
                Debug.Log($"ART ACCEPTANCE PASSED: {name}, {triangles} triangles, bounds {bounds.size}");
            }
        }
    }
}
