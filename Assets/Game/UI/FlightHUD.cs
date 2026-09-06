using UnityEngine;

namespace PacificCombat
{
    public sealed class FlightHUD : MonoBehaviour
    {
        MissionManager mission;
        GUIStyle small, text, large, title, button, smallButton;
        Texture2D pixel;
        Texture2D buttonNormal, buttonHover, buttonActive;
        bool settings;
        float fps, nextRead;
        string flightReadout, engineReadout, weaponReadout, debugReadout;
        readonly Color ivory = new Color(.95f, .94f, .82f);
        readonly Color amber = new Color(1, .72f, .23f);
        readonly Color panel = new Color(.045f, .07f, .08f, .87f);
        public void Initialize(MissionManager owner) { mission = owner; }
        public void SetSettingsVisible(bool visible) { settings = visible; }
        void Update()
        {
            fps = Mathf.Lerp(fps, 1 / Mathf.Max(.0001f, Time.unscaledDeltaTime), .04f);
            if (!mission || !mission.Player || Time.unscaledTime < nextRead) return;
            nextRead = Time.unscaledTime + .1f;
            var plane = mission.Player; var physics = plane.Physics;
            flightReadout = $"{physics.Airspeed * 2.23694f:0}  MPH     {physics.Altitude * 3.28084f:0}  FT";
            engineReadout = $"THR  {plane.Controls.Throttle * 100:0}%    RPM  {plane.Engine.RPM:0}    G  {physics.GForce:0.0}    FUEL  {plane.Engine.Fuel.FuelFraction * 100:0}%";
            weaponReadout = $".50 CAL   {mission.Weapons.Ammo:N0} ROUNDS";
            debugReadout = $"{fps:0} FPS   AoA {physics.AngleOfAttack:0.0}°   Lift {physics.Lift:0} N   Drag {physics.Drag:0} N\nThrust {plane.Engine.Thrust:0} N   V/S {plane.Body.linearVelocity.y:0.0} m/s   Airframe {mission.Damage.OverallHealth:P0}";
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
        }
        static Texture2D Swatch(Color color) { var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture; }
        void OnDestroy() { if (buttonNormal) Destroy(buttonNormal); if (buttonHover) Destroy(buttonHover); if (buttonActive) Destroy(buttonActive); }
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
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1600f, Screen.height / 900f, 1));
            if (mission.ShowHUD)
            {
                Box(new Rect(30, 25, 455, 92), panel);
                GUI.Label(new Rect(48, 36, 430, 32), "PACIFIC FIGHTER SWEEP", text);
                GUI.Label(new Rect(48, 75, 430, 30), $"Destroy all enemy fighters   •   {mission.Remaining} remaining", small);
                Box(new Rect(1280, 25, 290, 70), panel);
                GUI.Label(new Rect(1298, 35, 260, 28), $"HDG  {mission.Player.transform.eulerAngles.y:000}°", text);
                GUI.Label(new Rect(1298, 65, 260, 25), $"{mission.FlightCamera.Mode}    •    {mission.MissionTime / 60:00}:{mission.MissionTime % 60:00}", small);
                Box(new Rect(30, 779, 620, 95), panel);
                GUI.Label(new Rect(48, 786, 600, 43), flightReadout, large);
                GUI.Label(new Rect(48, 840, 600, 30), engineReadout, small);
                Box(new Rect(1140, 764, 430, 110), panel);
                GUI.Label(new Rect(1158, 775, 405, 34), weaponReadout, text);
                string target = mission.SelectedTarget ? $"{mission.SelectedTarget.name.ToUpperInvariant()}   {Vector3.Distance(mission.Player.transform.position, mission.SelectedTarget.transform.position):0} M" : "NO TARGET";
                GUI.Label(new Rect(1158, 816, 405, 30), target, small);
                GUI.Label(new Rect(1158, 845, 405, 24), "TAB  Select target     SPACE  Fire", small);
                var aim = mission.FlightCamera.Camera.WorldToScreenPoint(mission.Player.transform.position + mission.Player.transform.forward * mission.Weapons.Convergence);
                if (aim.z > 0 && mission.FlightCamera.Mode != FlightCameraMode.Flyby)
                    Sight(new Vector2(aim.x / Screen.width * 1600, 900 - aim.y / Screen.height * 900), 30, amber);
                TargetMarker();
                if (mission.Player.Physics.IsStalled) GUI.Label(new Rect(640, 670, 500, 40), "STALL — LOWER THE NOSE", large);
                else if (mission.Damage.Burning) GUI.Label(new Rect(640, 670, 500, 40), "ENGINE FIRE", large);
                else if (mission.Player.EngineHealth < .5f) GUI.Label(new Rect(640, 670, 500, 40), "ENGINE DAMAGED", text);
                else if (!mission.Player.Engine.Fuel.HasFuel) GUI.Label(new Rect(640, 670, 500, 40), "FUEL EMPTY — ENGINE STOPPED", text);
                else if (mission.Player.Engine.Fuel.LeakRate > .01f) GUI.Label(new Rect(640, 670, 500, 40), "FUEL LEAK", text);
                else if (mission.Player.Engine.Fuel.FuelFraction < .15f) GUI.Label(new Rect(640, 670, 500, 40), "LOW FUEL", text);
                GUI.Label(new Rect(48, 744, 900, 30), $"{(mission.Player.Controls.Flaps ? "FLAPS DOWN  " : "")}{(mission.Player.Controls.Gear ? "GEAR DOWN  " : "")}C  Camera    V  Cockpit    RMB  Look    ESC  Pause", small);
            }
            if (mission.ShowDebug) GUI.Label(new Rect(40, 135, 1000, 100), debugReadout, text);
            if (mission.State != MissionState.Flying) Menu();
            GUI.matrix = previous;
        }
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
            Box(new Rect(465, 135, 670, 630), panel);
            if (settings) { Settings(); return; }
            bool briefing = mission.State == MissionState.Briefing;
            bool result = mission.State == MissionState.Victory || mission.State == MissionState.Defeat;
            string heading = briefing ? "Pacific Fighter Sweep" : mission.State == MissionState.Victory ? "Airspace secured" : mission.State == MissionState.Defeat ? "Aircraft lost" : "Flight paused";
            GUI.Label(new Rect(510, 177, 600, 65), heading, title);
            var paragraph = new GUIStyle(text) { wordWrap = true };
            if (briefing)
            {
                GUI.Label(new Rect(510, 256, 580, 110), $"{mission.Player.Data.AircraftName}  /  {mission.Enemies.Length} × {mission.Definition.EnemyAircraft.AircraftName}\n{mission.Definition.StartingAltitude * 3.28084f:N0} feet. {mission.Definition.StartingSpeed * 2.23694f:0} mph. Enemies at {mission.Definition.EnemyRange / 1000:0.#} km.\nKeep your speed. Strike, extend, climb.", paragraph);
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
            GUI.Label(new Rect(510, 703, 580, 27), "Prototype aircraft and synthesized audio", small);
        }
        void Settings()
        {
            var preferences = GameSettings.Current;
            bool changed = false;
            GUI.Label(new Rect(510, 177, 600, 60), "Flight settings", title);
            GUI.Label(new Rect(510, 258, 580, 30), "Difficulty", text);
            for (int i = 0; i < 4; i++)
                if (GUI.Toggle(new Rect(510 + i * 145, 302, 140, 35), (int)preferences.Difficulty == i, ((FighterDifficulty)i).ToString(), button) && (int)preferences.Difficulty != i)
                { preferences.Difficulty = (FighterDifficulty)i; changed = true; }
            bool lead = SettingToggle(360, preferences.LeadIndicator, "Training lead indicator");
            bool ammunition = SettingToggle(404, preferences.UnlimitedAmmo, "Unlimited ammunition");
            bool hud = SettingToggle(448, preferences.ShowHUD, "Show flight HUD");
            if (lead != preferences.LeadIndicator || ammunition != preferences.UnlimitedAmmo || hud != preferences.ShowHUD) changed = true;
            preferences.LeadIndicator = lead;
            preferences.UnlimitedAmmo = ammunition;
            preferences.ShowHUD = hud;
            mission.ShowDebug = SettingToggle(492, mission.ShowDebug, "Developer telemetry (F3)");
            GUI.Label(new Rect(510, 540, 200, 30), "Master audio", text);
            preferences.MasterVolume = GUI.HorizontalSlider(new Rect(760, 553, 330, 25), preferences.MasterVolume, 0, 1);
            AudioListener.volume = preferences.MasterVolume;
            GUI.Label(new Rect(510, 589, 200, 30), "Graphics", text);
            string[] levels = { "Low", "Medium", "High", "Ultra" };
            for (int i = 0; i < 4; i++) if (GUI.Toggle(new Rect(710 + i * 95, 589, 90, 35), preferences.GraphicsPreset == i, levels[i], smallButton) && preferences.GraphicsPreset != i)
            {
                preferences.GraphicsPreset = i;
                changed = true;
            }
            if (changed) GameSettings.Commit(mission);
            // Volume previews while dragging; flush only on release or leaving the panel.
            if (Event.current.rawType == EventType.MouseUp || Event.current.rawType == EventType.KeyUp) GameSettings.SaveCurrent();
            if (Button(665, "Back")) { GameSettings.Commit(mission); settings = false; }
        }
        bool SettingToggle(float y, bool value, string label) => GUI.Toggle(new Rect(510, y, 580, 35), value, (value ? "ON    " : "OFF   ") + label, button);
    }
}
