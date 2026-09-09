using UnityEngine;

namespace PacificCombat
{
    /// <summary>Heading-up gameplay contact display. Affiliation follows Team, never airframe or nationality.</summary>
    public sealed class RadarScope
    {
        public struct Contact
        {
            public AircraftController Aircraft;
            public Vector2 Position, IconPosition;
            public bool Friendly, Selected;
        }

        public const float Radius = 108;
        public const float MilesToMetres = 1609.344f;
        public readonly float[] Ranges = { 2, 5, 10 };
        public Contact[] Contacts { get; private set; } = new Contact[32];
        public int Count { get; private set; }
        public int Friends { get; private set; }
        public int Foes { get; private set; }
        public int RangeIndex { get; private set; } = 1;
        public string Counts { get; private set; } = "0 FRIEND   0 FOE";
        public string Nearest { get; private set; } = "No foes in range";
        AircraftController[] aircraft = System.Array.Empty<AircraftController>();
        float nextScan, nextRead;
        int oldFriends = -1, oldFoes = -1, oldNearest = -1;
        Vector3 heading = Vector3.forward;

        public void SetRange(int index) { RangeIndex = Mathf.Clamp(index, 0, Ranges.Length - 1); nextRead = 0; }
        public void Refresh(MissionManager mission)
        {
            if (!mission || !mission.Player || Time.unscaledTime < nextRead) return;
            nextRead = Time.unscaledTime + .1f;
            // Discovery is bounded to once a second; no scene searches in OnGUI.
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 1;
                aircraft = Object.FindObjectsByType<AircraftController>(FindObjectsSortMode.InstanceID);
            }
            Read(mission.Player, mission.SelectedTarget, aircraft);
        }

        public static bool IsContact(AircraftController player, AircraftController other) =>
            player && other && other != player && other.isActiveAndEnabled && !other.IsDestroyed;

        public static bool Project(Vector3 delta, Vector3 forward, float range, out Vector2 point)
        {
            var horizontal = new Vector2(delta.x, delta.z);
            if (!float.IsFinite(horizontal.sqrMagnitude) || range <= 0 || horizontal.sqrMagnitude > range * range)
            { point = default; return false; }
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            delta.y = 0; // Radar range is horizontal, independent of contact altitude.
            point = new Vector2(Vector3.Dot(delta, right), -Vector3.Dot(delta, forward)) * (Radius / range);
            return true;
        }

        public void Read(AircraftController player, AircraftController selected, AircraftController[] sources)
        {
            Count = Friends = Foes = 0;
            if (!player) return;
            Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up);
            if (forward.sqrMagnitude > .001f) heading = forward.normalized;
            if (sources.Length > Contacts.Length) Contacts = new Contact[sources.Length];
            float nearest = float.PositiveInfinity, range = Ranges[RangeIndex] * MilesToMetres;
            // Selected contact claims its position first so crowding never hides it.
            for (int pass = 0; pass < 2; pass++)
                foreach (var other in sources)
                {
                    if (!IsContact(player, other) || (other == selected) != (pass == 0)) continue;
                    Vector3 delta = other.transform.position - player.transform.position;
                    if (!Project(delta, heading, range, out var point)) continue;
                    bool friendly = other.Team == player.Team;
                    if (friendly) Friends++;
                    else { Foes++; nearest = Mathf.Min(nearest, new Vector2(delta.x, delta.z).magnitude); }
                    Contacts[Count] = new Contact { Aircraft = other, Position = point,
                        IconPosition = Place(point), Friendly = friendly, Selected = !friendly && other == selected };
                    Count++;
                }
            if (Friends != oldFriends || Foes != oldFoes)
            { Counts = Friends + " FRIEND   " + Foes + " FOE"; oldFriends = Friends; oldFoes = Foes; }
            int hundredths = Foes == 0 ? -1 : Mathf.RoundToInt(nearest / MilesToMetres * 100);
            if (hundredths != oldNearest)
            { Nearest = Foes == 0 ? "No foes in range" : "Nearest foe  " + (hundredths / 100f).ToString("0.00") + " mi"; oldNearest = hundredths; }
        }

        Vector2 Place(Vector2 anchor)
        {
            if (Free(anchor)) return anchor;
            // Fan out crowded symbols, retaining leader lines to exact positions.
            for (int ring = 1; ring <= 9; ring++)
                for (int step = 0; step < 24; step++)
                {
                    float angle = step * Mathf.PI / 12;
                    Vector2 candidate = anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (ring * 25);
                    if (Free(candidate)) return candidate;
                }
            return Vector2.ClampMagnitude(anchor, Radius);
        }
        bool Free(Vector2 p)
        {
            if (p.sqrMagnitude > Radius * Radius || p.sqrMagnitude < 25 * 25) return false;
            for (int i = 0; i < Count; i++)
            {
                float separation = Contacts[i].Selected ? 34 : 25;
                if ((Contacts[i].IconPosition - p).sqrMagnitude < separation * separation) return false;
            }
            return true;
        }
    }
}
