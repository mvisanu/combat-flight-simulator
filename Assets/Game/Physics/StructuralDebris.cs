using System.Collections.Generic;
using UnityEngine;

namespace PacificCombat
{
    /// <summary>Pre-partitioned visual structures: no Instantiate, mesh edits or allocations on a hit.</summary>
    public sealed class StructuralDebris : MonoBehaviour
    {
        static readonly DamageZoneType[] Zones = { DamageZoneType.LeftWing, DamageZoneType.RightWing, DamageZoneType.HorizontalStabilizer, DamageZoneType.VerticalStabilizer };
        sealed class Piece { public GameObject Object; public Rigidbody Body; public float Remaining; public bool Released; }
        sealed class Skin { public MeshFilter Filter; public Mesh Original; public Mesh[] Remaining = new Mesh[4]; }
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        readonly List<Skin> skins = new List<Skin>();
        readonly Piece[] pieces = new Piece[4];
        AircraftController aircraft;
        AircraftDamage damage;
        DamageZone[] colliderZones;
        public int ActiveCount { get; private set; }
        public int Capacity => pieces.Length;

        public void Initialize(AircraftController owner)
        {
            aircraft = owner; damage = owner.GetComponent<AircraftDamage>();
            colliderZones = owner.GetComponentsInChildren<DamageZone>(true);
            var visuals = owner.GetComponent<AircraftVisuals>();
            Transform sourceRoot = visuals && visuals.DetailedAirframe ? visuals.DetailedAirframe : owner.transform.Find("Airframe");
            if (!sourceRoot) return;
            for (int zone = 0; zone < pieces.Length; zone++)
            {
                var go = new GameObject("Pooled detached " + Zones[zone]); go.SetActive(false);
                var body = go.AddComponent<Rigidbody>(); body.mass = owner.Body.mass * .035f; body.isKinematic = true;
                body.linearDamping = .08f; body.angularDamping = .25f;
                pieces[zone] = new Piece { Object = go, Body = body };
            }
            foreach (var filter in sourceRoot.GetComponentsInChildren<MeshFilter>(true)) Partition(filter);
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>(true);
            foreach (var piece in pieces)
            {
                var filters = piece.Object.GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length == 0) continue;
                Bounds bounds = filters[0].sharedMesh.bounds;
                for (int i = 1; i < filters.Length; i++) bounds.Encapsulate(filters[i].sharedMesh.bounds);
                var collider = piece.Object.AddComponent<BoxCollider>(); collider.center = bounds.center; collider.size = bounds.size + Vector3.one * .03f;
                piece.Body.centerOfMass = bounds.center;
                foreach (var own in ownerColliders) UnityEngine.Physics.IgnoreCollision(collider, own);
            }
            damage.StructuralFailure += Release;
        }
        int Classify(Vector3 point)
        {
            bool twin = aircraft.Data.Type == AircraftType.P38Lightning;
            if (point.z > -2.5f && point.z < 1.9f && Mathf.Abs(point.x) > (twin ? 3.4f : .95f)) return point.x < 0 ? 0 : 1;
            if (point.z < (twin ? -4.85f : -3.1f))
            {
                if (point.y > .55f && (twin ? Mathf.Abs(point.x) > 2.2f : Mathf.Abs(point.x) < .45f)) return 3;
                if (Mathf.Abs(point.x) > .5f && (!twin || Mathf.Abs(point.x) < 2.5f)) return 2;
            }
            return -1;
        }
        void Partition(MeshFilter filter)
        {
            Mesh mesh = filter.sharedMesh; var renderer = filter.GetComponent<MeshRenderer>();
            if (!mesh || !renderer || mesh.subMeshCount != 1) return;
            var vertices = mesh.vertices; var rootVertices = new Vector3[vertices.Length]; var normals = mesh.normals; var rootNormals = new Vector3[normals.Length];
            for (int i = 0; i < vertices.Length; i++) rootVertices[i] = transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
            for (int i = 0; i < normals.Length; i++) rootNormals[i] = transform.InverseTransformDirection(filter.transform.TransformDirection(normals[i])).normalized;
            int[] indices = mesh.triangles; var categories = new int[indices.Length / 3];
            for (int t = 0; t < indices.Length; t += 3) categories[t / 3] = Classify((rootVertices[indices[t]] + rootVertices[indices[t + 1]] + rootVertices[indices[t + 2]]) / 3);
            var skin = new Skin { Filter = filter, Original = mesh }; skins.Add(skin);
            for (int zone = 0; zone < 4; zone++)
            {
                var selected = new List<int>(); var remaining = new List<int>();
                for (int t = 0; t < indices.Length; t += 3)
                {
                    var list = categories[t / 3] == zone ? selected : remaining;
                    list.Add(indices[t]); list.Add(indices[t + 1]); list.Add(indices[t + 2]);
                }
                if (selected.Count == 0) continue;
                skin.Remaining[zone] = MakeMesh(vertices, normals, mesh.uv, remaining.ToArray(), "Remaining airframe");
                Mesh fragment = MakeMesh(rootVertices, rootNormals, mesh.uv, selected.ToArray(), "Separated structure");
                var child = new GameObject(renderer.name); child.transform.SetParent(pieces[zone].Object.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = fragment; child.AddComponent<MeshRenderer>().sharedMaterial = renderer.sharedMaterial;
            }
        }
        Mesh MakeMesh(Vector3[] vertices, Vector3[] normals, Vector2[] uv, int[] triangles, string label)
        {
            // Compact selected vertices so bounds/colliders describe the detached part,
            // rather than including unused vertices from the entire original aircraft.
            var map = new Dictionary<int, int>(); var points = new List<Vector3>(); var directions = new List<Vector3>(); var texcoords = new List<Vector2>();
            var remapped = new int[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                int original = triangles[i];
                if (!map.TryGetValue(original, out int index))
                {
                    index = points.Count; map.Add(original, index); points.Add(vertices[original]);
                    directions.Add(normals.Length > original ? normals[original] : Vector3.up);
                    texcoords.Add(uv.Length > original ? uv[original] : Vector2.zero);
                }
                remapped[i] = index;
            }
            var mesh = new Mesh { name = label, indexFormat = points.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16, vertices = points.ToArray(), normals = directions.ToArray(), uv = texcoords.ToArray(), triangles = remapped };
            mesh.RecalculateBounds(); ownedMeshes.Add(mesh); return mesh;
        }
        public void Release(DamageZoneType failedZone)
        {
            int zone = System.Array.IndexOf(Zones, failedZone);
            if (zone < 0 || pieces[zone] == null || pieces[zone].Released) return;
            Piece piece = pieces[zone]; piece.Released = true; piece.Remaining = 35;
            piece.Object.transform.SetPositionAndRotation(transform.position, transform.rotation);
            piece.Object.SetActive(true); piece.Body.isKinematic = false;
            piece.Body.linearVelocity = aircraft.Body.GetPointVelocity(transform.TransformPoint(piece.Body.centerOfMass));
            piece.Body.angularVelocity = aircraft.Body.angularVelocity + transform.forward * (zone == 0 ? -.8f : .8f);
            foreach (var skin in skins) if (skin.Filter && skin.Remaining[zone]) skin.Filter.sharedMesh = skin.Remaining[zone];
            foreach (var item in colliderZones) if (item && item.Type == failedZone)
            {
                var collider = item.GetComponent<Collider>(); if (collider) collider.enabled = false;
                var renderer = item.GetComponent<Renderer>(); if (renderer) renderer.enabled = false;
            }
            ActiveCount++;
        }
        void Update() => Simulate(Time.deltaTime);
        public void Simulate(float dt)
        {
            foreach (var piece in pieces) if (piece != null && piece.Object.activeSelf)
            {
                piece.Remaining -= dt;
                if (piece.Remaining <= 0) { piece.Body.isKinematic = true; piece.Object.SetActive(false); ActiveCount--; }
            }
        }
        public void ResetState()
        {
            foreach (var skin in skins) if (skin.Filter) skin.Filter.sharedMesh = skin.Original;
            foreach (var piece in pieces) if (piece != null) { piece.Body.isKinematic = true; piece.Object.SetActive(false); piece.Released = false; }
            foreach (var zone in colliderZones) if (zone) { var collider = zone.GetComponent<Collider>(); if (collider) collider.enabled = true; }
            ActiveCount = 0;
        }
        public void ShiftOrigin(Vector3 offset) { foreach (var piece in pieces) if (piece != null && piece.Object.activeSelf) piece.Body.position -= offset; }
        void OnDestroy()
        {
            if (damage) damage.StructuralFailure -= Release;
            foreach (var piece in pieces) if (piece != null) Remove(piece.Object);
            foreach (var mesh in ownedMeshes) Remove(mesh);
        }
        static void Remove(Object item) { if (item) { if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); } }
    }
}
