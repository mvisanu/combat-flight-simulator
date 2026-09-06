using UnityEngine;

namespace PacificCombat
{
    public sealed class TracerPool : MonoBehaviour
    {
        LineRenderer[] lines;
        bool[] active;
        Material material;
        public void Initialize(int capacity, Color color)
        {
            lines = new LineRenderer[capacity]; active = new bool[capacity];
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color * 3);
            for (int i = 0; i < capacity; i++)
            {
                var child = new GameObject("Pooled tracer");
                child.transform.SetParent(transform, false);
                var line = child.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.positionCount = 2;
                line.startWidth = .09f; line.endWidth = .045f;
                line.useWorldSpace = true;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.enabled = false;
                lines[i] = line;
            }
        }
        public int Acquire(Vector3 position)
        {
            for (int i = 0; i < lines.Length; i++)
                if (!active[i]) { active[i] = true; lines[i].enabled = true; Move(i, position, position); return i; }
            return -1;
        }
        public void Move(int slot, Vector3 start, Vector3 end) { lines[slot].SetPosition(0, start); lines[slot].SetPosition(1, end); }
        public void Release(int slot) { active[slot] = false; lines[slot].enabled = false; }
        public void Shift(Vector3 offset)
        {
            for (int i = 0; i < lines.Length; i++) if (active[i]) Move(i, lines[i].GetPosition(0) - offset, lines[i].GetPosition(1) - offset);
        }
        void OnDestroy()
        {
            if (!material) return;
            if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
        }
    }
}
