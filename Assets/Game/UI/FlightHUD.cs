using UnityEngine;
using Unity.Profiling;

namespace PacificCombat
{
    public sealed class FlightHUD : MonoBehaviour
    {
        MissionManager mission;
        GUIStyle small, text, large, title, button, smallButton, paragraph, advice;
        FlightSettingsPanel settingsPanel;
        static readonly ProfilerMarker TelemetryMarker = new ProfilerMarker("PacificCombat.HUD.Telemetry");
        static readonly ProfilerMarker DrawMarker = new ProfilerMarker("PacificCombat.HUD.Draw");
        public long TelemetryAllocatedBytes { get; private set; }
        public int TelemetryRefreshCount { get; private set; }
        Texture2D pixel;
        Texture2D buttonNormal, buttonHover, buttonActive;
        readonly Texture2D[] countryFlags = new Texture2D[3];
        bool settings;
        readonly RadarScope radar = new RadarScope();
        public RadarScope Radar => radar;
        Texture2D radarGrid, radarFriend, radarFoe, radarPlayer;
        GUIStyle radarText, radarButton;
        readonly string[] radarLabels = { "2 mi", "5 mi", "10 mi" };
        readonly string[] radarRingLabels = { "Rings: 1 / 2 mi radius", "Rings: 2.5 / 5 mi radius", "Rings: 5 / 10 mi radius" };

        float fps, nextRead;
        string flightReadout, engineReadout, weaponReadout, debugReadout;
        string remainingReadout, headingReadout, cameraReadout, targetReadout, targetName, cockpitReadout;
        AircraftController namedTarget;
        int oldSpeed = -1, oldAltitude = -1, oldRPM = -1, oldThrottle = -1, oldG = -999, oldFuel = -1, oldAmmo = -1, oldRemaining = -1, oldHeading = -1, oldSeconds = -1, oldDistance = -1;
        FlightCameraMode oldCamera = (FlightCameraMode)(-1);
        static readonly string[] MissionNames = { "Fighter sweep", "Intercept", "Free flight", "Landing" };
        static readonly string[] WeatherNames = { "Clear", "Scattered", "Overcast", "Squall" };
        readonly Color ivory = new Color(.95f, .94f, .82f);
        readonly Color amber = new Color(1, .72f, .23f);
        readonly Color panel = new Color(.045f, .07f, .08f, .87f);
        public void Initialize(MissionManager owner)
        {
            mission = owner; settingsPanel = new FlightSettingsPanel(); gameObject.AddComponent<UIClickSound>();
            for (int i=0;i<countryFlags.Length;i++) countryFlags[i]=WartimeFlags.Create(i);
        }
        public void SetSettingsVisible(bool visible) { settings = visible; }
        public void SetSettingsPage(int page) { settingsPanel.SetPage(page); }
        void Update()
        {
            if (mission && mission.ShowHUD && mission.State == MissionState.Flying) radar.Refresh(mission);
            fps = Mathf.Lerp(fps, 1 / Mathf.Max(.0001f, Time.unscaledDeltaTime), .04f);
            if (!mission || !mission.Player || Time.unscaledTime < nextRead) return;
            nextRead = Time.unscaledTime + .1f;
            if (!mission.ShowHUD && !mission.ShowDebug) return;
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            using (TelemetryMarker.Auto())
            {
            var plane = mission.Player; var physics = plane.Physics;
            int speed = Mathf.RoundToInt(physics.Airspeed * 2.23694f), altitude = Mathf.RoundToInt(physics.Altitude * 3.28084f);
            if (speed != oldSpeed || altitude != oldAltitude)
            { flightReadout = $"{speed}  MPH     {altitude}  FT"; oldSpeed = speed; oldAltitude = altitude; }
            int throttle = Mathf.RoundToInt(plane.Controls.Throttle * 100), rpm = Mathf.RoundToInt(plane.Engine.RPM), g = Mathf.RoundToInt(physics.GForce * 10), fuel = Mathf.RoundToInt(plane.Engine.Fuel.FuelFraction * 100);
            if (throttle != oldThrottle || rpm != oldRPM || g != oldG || fuel != oldFuel)
            {
                engineReadout = $"THR  {throttle}%    RPM  {rpm}    G  {g * .1f:0.0}    FUEL  {fuel}%";
                cockpitReadout = $"THROTTLE  {throttle}%     G  {g * .1f:0.0}";
                oldThrottle = throttle; oldRPM = rpm; oldG = g; oldFuel = fuel;
            }
            if (mission.Weapons.Ammo != oldAmmo) { oldAmmo = mission.Weapons.Ammo; weaponReadout = $"{mission.Definition.PlayerWeapons.DisplayLabel}   {oldAmmo:N0} ROUNDS"; }
            if (mission.Remaining != oldRemaining)
            {
                oldRemaining = mission.Remaining;
                remainingReadout = oldRemaining > 0 ? $"Destroy enemy fighters   |   {oldRemaining} remaining" : mission.Definition.Scenario == MissionPreset.LandingPractice ? "Land on runway 36 and brake to a stop" : "Free flight | Explore and practice";
            }
            int heading = Mathf.RoundToInt(plane.transform.eulerAngles.y) % 360;
            if (heading != oldHeading) { oldHeading = heading; headingReadout = $"HDG  {heading:000}°"; }
            int seconds = Mathf.FloorToInt(mission.MissionTime);
            if (seconds != oldSeconds || oldCamera != mission.FlightCamera.Mode)
            { oldSeconds = seconds; oldCamera = mission.FlightCamera.Mode; cameraReadout = $"{oldCamera}    ·    {seconds / 60:00}:{seconds % 60:00}"; }
            if (namedTarget != mission.SelectedTarget)
            { namedTarget = mission.SelectedTarget; targetName = namedTarget ? namedTarget.name.ToUpperInvariant() : "NO TARGET"; oldDistance = -1; }
            int distance = namedTarget ? Mathf.RoundToInt(Vector3.Distance(plane.transform.position, namedTarget.transform.position)) : 0;
            if (distance != oldDistance || targetReadout == null)
            { oldDistance = distance; targetReadout = namedTarget ? targetName + "   " + distance + " M" : "NO TARGET"; }
            if (mission.ShowDebug) debugReadout = $"{fps:0} FPS   AoA {physics.AngleOfAttack:0.0}°   Lift {physics.Lift:0} N   Drag {physics.Drag:0} N\nThrust {plane.Engine.Thrust:0} N   V/S {plane.Body.linearVelocity.y:0.0} m/s   Airframe {mission.Damage.OverallHealth:P0}";
            }
            TelemetryRefreshCount++;
            TelemetryAllocatedBytes += System.GC.GetAllocatedBytesForCurrentThread() - before;
        }
        void Styles()
        {
            if (pixel) return;
            pixel = Texture2D.whiteTexture;
            text = new GUIStyle(GUI.skin.label) { fontSize = 22, normal = { textColor = ivory } };
            small = new GUIStyle(text) { fontSize = 16 };
            large = new GUIStyle(text) { fontSize = 32, fontStyle = FontStyle.Bold };
            title = new GUIStyle(large) { fontSize = 44 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 21, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(20, 10, 8, 8) };
            buttonNormal = Swatch(new Color(.13f, .19f, .2f));
            buttonHover = Swatch(new Color(.23f, .31f, .32f));
            buttonActive = Swatch(new Color(.32f, .36f, .24f));
            button.normal.background = buttonNormal; button.normal.textColor = ivory;
            button.hover.background = buttonHover; button.hover.textColor = Color.white;
            button.focused.background = buttonHover; button.focused.textColor = Color.white;
            button.active.background = buttonActive; button.active.textColor = Color.white;
            button.onNormal.background = buttonActive; button.onNormal.textColor = Color.white;
            button.onHover.background = buttonHover; button.onHover.textColor = Color.white;
            button.onFocused.background = buttonHover; button.onFocused.textColor = Color.white;
            smallButton = new GUIStyle(button) { fontSize = 16, padding = new RectOffset(8, 8, 3, 3) };
            paragraph = new GUIStyle(text) { wordWrap = true };
            advice = new GUIStyle(small) { wordWrap = true };
            radarText = new GUIStyle(text) { fontSize = 18 };
            radarButton = new GUIStyle(smallButton) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            radarGrid = RadarSymbols.Grid(); radarFriend = RadarSymbols.Contact(true);
            radarFoe = RadarSymbols.Contact(false); radarPlayer = RadarSymbols.Player();
        }
        static Texture2D Swatch(Color color) { var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture; }
        void OnDestroy() { if (buttonNormal) Destroy(buttonNormal); if (buttonHover) Destroy(buttonHover); if (buttonActive) Destroy(buttonActive); foreach(var flag in countryFlags) if(flag) Destroy(flag);
            if (radarGrid) Destroy(radarGrid); if (radarFriend) Destroy(radarFriend); if (radarFoe) Destroy(radarFoe); if (radarPlayer) Destroy(radarPlayer); }
        void Box(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, pixel); GUI.color = Color.white; }
        void Line(Vector2 from, Vector2 to, Color color, float width = 2)
        {
            var matrix = GUI.matrix;
            GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(from.x, from.y, 0),
                Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, to - from)), Vector3.one);
            Box(new Rect(0, 0, Vector2.Distance(from, to), width), color);
            GUI.matrix = matrix;
        }
        void Sight(Vector2 center, float radius, Color color)
        {
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48, b = (i + 1) * Mathf.PI * 2 / 48;
                Line(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, color, 1.5f);
            }
            Line(center + Vector2.left * (radius + 12), center + Vector2.left * (radius - 8), color);
            Line(center + Vector2.right * (radius - 8), center + Vector2.right * (radius + 12), color);
            Line(center + Vector2.up * (radius + 12), center + Vector2.up * (radius - 8), color);
            Box(new Rect(center.x - 2, center.y - 2, 4, 4), color);
        }
        void OnGUI()
        {
            if (!mission || !mission.Player) return;
            Styles();
            // The rebind component owns its pixel-space panel.
            if (mission.Input.ShowBindings) return;
            using (DrawMarker.Auto())
            {
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1600f, Screen.height / 900f, 1));
            if (mission.ShowHUD)
            {
                Box(new Rect(30, 25, 455, 92), panel);
                GUI.Label(new Rect(48, 36, 430, 32), mission.Definition.MissionName, text);
                GUI.Label(new Rect(48, 75, 430, 30), remainingReadout, small);
                Box(new Rect(1280, 25, 290, 70), panel);
                GUI.Label(new Rect(1298, 35, 260, 28), headingReadout, text);
                GUI.Label(new Rect(1298, 65, 260, 25), cameraReadout, small);
                if (mission.FlightCamera.Mode == FlightCameraMode.Cockpit)
                {
                    // Physical gauges supply flight telemetry in the cockpit. Keep
                    // throttle and G available without covering the instrument panel.
                    Box(new Rect(30, 805, 460, 69), panel);
                    GUI.Label(new Rect(48, 815, 435, 32), cockpitReadout, text);
                }
                else
                {
                    Box(new Rect(30, 779, 620, 95), panel);
                    GUI.Label(new Rect(48, 786, 600, 43), flightReadout, large);
                    GUI.Label(new Rect(48, 840, 600, 30), engineReadout, small);
                }
                Box(new Rect(1140, 764, 430, 110), panel);
                GUI.Label(new Rect(1158, 775, 405, 34), weaponReadout, text);
                GUI.Label(new Rect(1158, 816, 405, 30), targetReadout, small);
                GUI.Label(new Rect(1158, 845, 405, 24), "TAB  Select target     SPACE  Fire", small);
                var aim = mission.FlightCamera.Camera.WorldToScreenPoint(mission.Player.transform.position + mission.Player.transform.forward * mission.Weapons.Convergence);
                if (aim.z > 0 && mission.FlightCamera.Mode != FlightCameraMode.Flyby)
                    Sight(new Vector2(aim.x / Screen.width * 1600, 900 - aim.y / Screen.height * 900), 30, amber);
                TargetMarker();
                DrawRadar();
                if (mission.Player.Physics.IsStalled) GUI.Label(new Rect(640, 670, 500, 40), "STALL — LOWER THE NOSE", large);
                else if (mission.Damage.Burning) GUI.Label(new Rect(640, 670, 500, 40), "ENGINE FIRE", large);
                else if (mission.Player.EngineHealth < .5f) GUI.Label(new Rect(640, 670, 500, 40), "ENGINE DAMAGED", text);
                else if (!mission.Player.Engine.Fuel.HasFuel) GUI.Label(new Rect(640, 670, 500, 40), "FUEL EMPTY — ENGINE STOPPED", text);
                else if (mission.Player.Engine.Fuel.LeakRate > .01f) GUI.Label(new Rect(640, 670, 500, 40), "FUEL LEAK", text);
                else if (mission.Player.Engine.Fuel.FuelFraction < .15f) GUI.Label(new Rect(640, 670, 500, 40), "LOW FUEL", text);
                GUI.Label(new Rect(48, mission.FlightCamera.Mode == FlightCameraMode.Cockpit ? 847 : 744, 900, 30), ControlHelp(mission.Player.Controls.Flaps, mission.Player.Controls.Gear), small);
            }
            if (mission.ShowDebug) GUI.Label(new Rect(40, 135, 1000, 100), debugReadout, text);
            if (mission.State != MissionState.Flying) Menu();
            GUI.matrix = previous;
            }
        }
        void DrawRadar()
        {
            if (mission.State != MissionState.Flying) return;
            Box(new Rect(1280, 116, 290, 458), new Color(.025f, .055f, .065f, .94f));
            GUI.Label(new Rect(1298, 124, 254, 30), "AIR CONTACTS", text);
            GUI.Label(new Rect(1474, 129, 80, 24), "HDG UP", small);
            for (int i = 0; i < radar.Ranges.Length; i++)
            {
                var rect = new Rect(1298 + i * 86, 159, 80, 32);
                bool active = i == radar.RangeIndex;
                if (GUI.Toggle(rect, active, radarLabels[i], radarButton) && !active) radar.SetRange(i);
                if (active) Box(new Rect(rect.x + 8, rect.yMax - 3, rect.width - 16, 2), ivory);
            }
            Vector2 center = new Vector2(1425, 316);
            // 0.94 is the outer ring's normalized radius in the cached grid image.
            const float extent = RadarScope.Radius / .94f;
            GUI.DrawTexture(new Rect(center.x-extent, center.y-extent, extent*2, extent*2), radarGrid);
            for (int i = 0; i < radar.Count; i++)
            {
                var contact = radar.Contacts[i];
                if (!contact.Aircraft || contact.Aircraft.IsDestroyed || !contact.Aircraft.isActiveAndEnabled) continue;
                Vector2 point = center + contact.IconPosition, anchor = center + contact.Position;
                if ((point-anchor).sqrMagnitude > 4)
                {
                    Line(anchor, point, new Color(.5f,.62f,.64f,.7f), 1);
                    Box(new Rect(anchor.x-1.5f, anchor.y-1.5f, 3, 3), ivory);
                }
            }
            // Selected symbol is painted last, keeping its brackets visible in crowds.
            for (int i = radar.Count-1; i >= 0; i--)
            {
                var contact = radar.Contacts[i];
                if (!contact.Aircraft || contact.Aircraft.IsDestroyed || !contact.Aircraft.isActiveAndEnabled) continue;
                Vector2 point = center + contact.IconPosition;
                RadarIcon(point, contact.Friendly ? radarFriend : radarFoe);
                if (contact.Selected) RadarSelection(point);
            }
            Box(new Rect(center.x-11, center.y-11, 22, 22), RadarSymbols.Ink);
            RadarIcon(center, radarPlayer);
            RadarIcon(new Vector2(1307,449), radarFriend);
            GUI.Label(new Rect(1326,437,98,26), "FRIEND", radarText);
            RadarIcon(new Vector2(1440,449), radarFoe);
            GUI.Label(new Rect(1459,437,94,26), "FOE", radarText);
            RadarIcon(new Vector2(1307,478), radarPlayer);
            GUI.Label(new Rect(1326,466,98,26), "YOU", radarText);
            RadarIcon(new Vector2(1440,478), radarFoe); RadarSelection(new Vector2(1440,478));
            GUI.Label(new Rect(1459,466,94,26), "TARGET", radarText);
            GUI.Label(new Rect(1298,499,256,26), radar.Counts, radarText);
            GUI.Label(new Rect(1298,525,256,26), radar.Nearest, radarText);
            GUI.Label(new Rect(1298,549,256,24), radarRingLabels[radar.RangeIndex], small);
        }

        void RadarIcon(Vector2 point, Texture2D icon) => GUI.DrawTexture(new Rect(point.x-14,point.y-14,28,28),icon);
        void RadarSelection(Vector2 point)
        {
            for (int x=-1;x<=1;x+=2) for (int y=-1;y<=1;y+=2)
            {
                Vector2 corner = point + new Vector2(x*16,y*16);
                Line(corner, corner-new Vector2(x*6,0), amber, 2);
                Line(corner, corner-new Vector2(0,y*6), amber, 2);
            }
        }

        static string ControlHelp(bool flaps, bool gear) => flaps ? (gear ? "FLAPS DOWN  GEAR DOWN  C  Camera    V  Cockpit    RMB  Look    ESC  Pause" : "FLAPS DOWN  C  Camera    V  Cockpit    RMB  Look    ESC  Pause") : (gear ? "GEAR DOWN  C  Camera    V  Cockpit    RMB  Look    ESC  Pause" : "C  Camera    V  Cockpit    RMB  Look    ESC  Pause");
        void TargetMarker()
        {
            if (!mission.SelectedTarget) return;
            var target = mission.SelectedTarget;
            Vector3 screen = mission.FlightCamera.Camera.WorldToScreenPoint(target.transform.position);
            var p = new Vector2(screen.x / Screen.width * 1600, 900 - screen.y / Screen.height * 900);
            bool visible = screen.z > 0 && p.x > 24 && p.x < 1576 && p.y > 130 && p.y < 735;
            if (visible)
            {
                const float r = 12;
                Line(p + new Vector2(-r, -r), p + new Vector2(-r, 0), amber);
                Line(p + new Vector2(-r, -r), p + new Vector2(0, -r), amber);
                Line(p + new Vector2(r, r), p + new Vector2(r, 0), amber);
                Line(p + new Vector2(r, r), p + new Vector2(0, r), amber);
            }
            else
            {
                var direction = p - new Vector2(800, 450); if (screen.z < 0) direction = -direction;
                p = new Vector2(800, 450) + direction.normalized * 310;
                Line(p, p - direction.normalized * 20 + new Vector2(-direction.y, direction.x).normalized * 10, amber);
                Line(p, p - direction.normalized * 20 - new Vector2(-direction.y, direction.x).normalized * 10, amber);
            }
            if (mission.LeadIndicator)
            {
                var intercept = FighterAIAiming.Intercept(mission.Player.transform.position, mission.Player.Body.linearVelocity, target.transform.position, target.Body.linearVelocity, mission.Weapons.MuzzleVelocity);
                screen = mission.FlightCamera.Camera.WorldToScreenPoint(intercept);
                if (screen.z > 0) Sight(new Vector2(screen.x / Screen.width * 1600, 900 - screen.y / Screen.height * 900), 7, new Color(.5f, .95f, .65f));
            }
        }
        bool Button(float y, string label) => GUI.Button(new Rect(510, y, 580, 46), label, button);
        void Menu()
        {
            Box(new Rect(0, 0, 1600, 900), new Color(.01f, .025f, .035f, .45f));
            bool briefing = mission.State == MissionState.Briefing;
            Box(briefing && !settings ? new Rect(465, 75, 670, 750) : new Rect(465, 135, 670, 630), panel);
            if (settings) { Settings(); return; }
            bool result = mission.State == MissionState.Victory || mission.State == MissionState.Defeat;
            string heading = briefing ? mission.Definition.MissionName : mission.State == MissionState.Victory ? (mission.Definition.Scenario == MissionPreset.LandingPractice ? "Landing complete" : "Airspace secured") : mission.State == MissionState.Defeat ? "Aircraft lost" : "Flight paused";
            GUI.Label(new Rect(510, briefing ? 108 : 177, 600, 65), heading, title);
            if (briefing)
            {
                Briefing();
                return;
            }
            else if (result)
            {
                float accuracy = mission.Weapons.ShotsFired > 0 ? 100f * mission.Weapons.Hits / mission.Weapons.ShotsFired : 0;
                GUI.Label(new Rect(510, 256, 580, 110), $"Enemies lost  {mission.Kills} / {mission.Enemies.Length}\nRounds fired  {mission.Weapons.ShotsFired}     Hits  {mission.Weapons.Hits}\nAccuracy  {accuracy:0.0}%     Flight time  {mission.MissionTime:0}s", paragraph);
            }
            else GUI.Label(new Rect(510, 256, 580, 95), "S / W  Pitch up / down     A / D  Roll\nQ / E  Rudder     SHIFT / CTRL  Throttle\nF  Flaps     G  Gear     M  Mouse flight", paragraph);
            if (Button(388, briefing ? "Fly mission" : result ? "Fly again" : "Resume flight"))
            { if (briefing) mission.BeginMission(); else if (result) mission.Restart(); else mission.Resume(); }
            if (Button(447, "Controls & remapping")) mission.Input.ShowBindings = true;
            if (Button(506, "Settings & training assists")) settings = true;
            if (!briefing)
            {
                if (GUI.Button(new Rect(510, 565, 280, 46), "Restart mission", button)) mission.Restart();
                if (GUI.Button(new Rect(810, 565, 280, 46), "Main menu", button)) mission.MainMenu();
            }
            if (Button(624, "Quit")) mission.Quit();
            GUI.Label(new Rect(510, 703, 580, 27), "Prototype aircraft | Recorded and synthesized audio", small);
        }

        void Briefing()
        {
            PresetRow(188, "MISSION", MissionNames, (int)GameSettings.Current.Mission, false);
            PresetRow(264, "WEATHER", WeatherNames, (int)GameSettings.Current.Weather, true);
            AircraftRow(340, "YOUR AIRCRAFT", mission.PlayerAircraftType, false);
            AircraftRow(416, "ENEMY AIRCRAFT", mission.EnemyAircraftType, true);
            GUI.Label(new Rect(510, 495, 580, 50), mission.Definition.Description, advice);
            GUI.Label(new Rect(510, 552, 580, 32), $"{mission.Definition.StartingAltitude * 3.28084f:N0} feet | {mission.Definition.StartingSpeed * 2.23694f:0} mph | {mission.Enemies.Length} enemy fighters", small);
            if (Button(598, "Fly mission")) { UIClickSound.Play(); mission.BeginMission(); }
            if (GUI.Button(new Rect(510, 660, 280, 46), "Controls & remapping", button)) mission.Input.ShowBindings = true;
            if (GUI.Button(new Rect(810, 660, 280, 46), "Settings & training", button)) settings = true;
            if (Button(722, "Quit")) mission.Quit();
            GUI.Label(new Rect(510, 786, 580, 27), "Prototype aircraft | Recorded and synthesized audio", small);
        }

        void PresetRow(float y, string label, string[] names, int selected, bool weather)
        {
            GUI.Label(new Rect(510, y, 580, 24), label + " / " + names[selected], small);
            for (int i = 0; i < 4; i++)
                if (GUI.Toggle(new Rect(510 + i * 145, y + 29, 140, 36), i == selected, names[i], smallButton) && i != selected)
                {
                    if (weather) mission.SelectWeather((WeatherPreset)i); else mission.SelectScenario((MissionPreset)i);
                    GUIUtility.ExitGUI();
                }
        }

        void AircraftRow(float y, string label, AircraftType selected, bool enemy)
        {
            GUI.Label(new Rect(510, y, 580, 24), label + "  /  " + mission.Catalog.Get(selected).ShortName + " | " + WartimeFlags.CountryName(selected), small);
            for (int i = 0; i < 4; i++)
            {
                var type = (AircraftType)i;
                var entry = mission.Catalog.Get(type);
                if (GUI.Toggle(new Rect(510 + i * 145, y + 29, 140, 42), type == selected, entry.ShortName, smallButton) && type != selected)
                {
                    mission.SelectAircraft(type, enemy);
                    GUIUtility.ExitGUI();
                }
                GUI.DrawTexture(new Rect(614+i*145,y+42,28,15),countryFlags[WartimeFlags.Country(type)],ScaleMode.StretchToFill);
            }
        }
        void Settings() { settings = settingsPanel.Draw(mission, title, text, small, smallButton); }
        bool SettingToggle(float y, bool value, string label) => GUI.Toggle(new Rect(510, y, 580, 35), value, (value ? "ON    " : "OFF   ") + label, button);
    }
}
