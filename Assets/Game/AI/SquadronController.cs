using System.Collections.Generic;
using UnityEngine;

namespace PacificCombat
{
    public enum SquadronRole { Attacker, Wingman, Support }
    // Small fixed mission roster, registered at spawn; no scene searches or per-tick allocations.
    public static class SquadronController
    {
        static readonly List<FighterAIController> Members = new List<FighterAIController>(16);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Members.Clear();
        public static void Register(FighterAIController member) { if (!Members.Contains(member)) Members.Add(member); }
        public static void Unregister(FighterAIController member) => Members.Remove(member);

        public static int AttackPriority(FighterAIController member)
        {
            int count = 0, rank = 0;
            for (int i = 0; i < Members.Count; i++)
            {
                FighterAIController other = Members[i];
                if (other == null || other.Aircraft == null || other.Aircraft.IsDestroyed
                    || other.Aircraft.Team != member.Aircraft.Team || other.Target != member.Target) continue;
                count++;
                if (other.SquadIndex < member.SquadIndex) rank++;
            }
            if (count < 2) return 0;
            // A deterministic handoff lets a wingman take the next pass rather than permanently
            // yielding to the original leader. Dead members are removed from the rotation.
            int lead = Mathf.FloorToInt(member.CombatTime / 30f) % count;
            return (rank - lead + count) % count;
        }

        public static Vector3 ApproachOffset(Transform target, int slot, float range)
        {
            float side = (slot & 1) == 0 ? -1f : 1f;
            float distance = Mathf.Clamp01((range - 1500f) / 2000f);
            return (target.right * side * (180f + slot * 95f)
                + Vector3.up * (slot * 90f)) * distance;
        }

        public static Vector3 Separation(FighterAIController member)
        {
            Vector3 offset = Vector3.zero;
            for (int i = 0; i < Members.Count; i++)
            {
                FighterAIController other = Members[i];
                if (other == null || other == member || other.Aircraft == null || other.Aircraft.IsDestroyed) continue;
                Vector3 delta = member.transform.position - other.transform.position;
                Vector3 future = delta + (member.Aircraft.Body.linearVelocity - other.Aircraft.Body.linearVelocity) * 1.2f;
                if (future.sqrMagnitude < delta.sqrMagnitude) delta = future;
                float distance = delta.magnitude;
                // The lead attacker owns its lane; wingmen yield first. Everyone still avoids
                // an imminent collision, but symmetric repulsion cannot spoil every gun pass.
                if (AttackPriority(member) < AttackPriority(other) && distance > 45f) continue;
                if (distance < 140f && distance > 0.01f) offset += delta / distance * (140f - distance) * 5f;
            }
            return Vector3.ClampMagnitude(offset, 450f);
        }

        public static bool ClearFireLane(FighterAIController member, Vector3 direction, float range)
        {
            for (int i = 0; i < Members.Count; i++)
            {
                FighterAIController other = Members[i];
                if (other == null || other == member || other.Aircraft == null || other.Aircraft.IsDestroyed
                    || other.Aircraft.Team != member.Aircraft.Team) continue;
                Vector3 delta = other.transform.position - member.transform.position;
                float along = Vector3.Dot(delta, direction);
                if (along > 0f && along < range && (delta - direction * along).sqrMagnitude < 400f) return false;
            }
            return true;
        }
    }
}
