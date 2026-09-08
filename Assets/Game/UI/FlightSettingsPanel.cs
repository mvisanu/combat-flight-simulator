using System.Collections.Generic;
using UnityEngine;

namespace PacificCombat
{
    /// <summary>Existing flight-menu visual language, split into focused settings pages.</summary>
    public sealed class FlightSettingsPanel
    {
        static readonly string[] Pages = { "Gameplay", "Graphics", "Audio", "Devices" };
        static readonly string[] Quality = { "Low", "Medium", "High", "Ultra" };
        static readonly string[] Shadows = { "Off", "Low", "Medium", "High" };
        static readonly string[] Textures = { "Full", "Half", "Quarter" };
        static readonly string[] AA = { "Off", "2× MSAA", "4× MSAA", "8× MSAA" };
        static readonly string[] Windows = { "Windowed", "Borderless", "Exclusive full screen" };
        static readonly string[] Difficulty = { "Rookie", "Regular", "Veteran", "Ace" };
        static readonly string[] FrameLabels = { "Uncapped", "30 FPS", "60 FPS", "90 FPS", "120 FPS", "144 FPS", "165 FPS", "240 FPS" };
        static readonly int[] FrameValues = { 0, 30, 60, 90, 120, 144, 165, 240 };
        readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        readonly List<string> resolutionLabels = new List<string>();
        GUIStyle text, small, button;
        int page;
        public void SetPage(int index) { page = Mathf.Clamp(index,0,3); scroll = Vector2.zero; }
        Vector2 scroll;
        bool changed, pending;

        public FlightSettingsPanel()
        {
            resolutions.Add(Vector2Int.zero); resolutionLabels.Add("Current display size");
            foreach (var resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (resolutions.Contains(size)) continue;
                resolutions.Add(size); resolutionLabels.Add(size.x + " × " + size.y);
            }
        }

        public bool Draw(MissionManager mission, GUIStyle title, GUIStyle body, GUIStyle label, GUIStyle control)
        {
            text = body; small = label; button = control;
            GUI.Label(new Rect(510, 177, 600, 60), "Flight settings", title);
            for (int i = 0; i < Pages.Length; i++)
                if (GUI.Toggle(new Rect(510 + i * 145, 250, 140, 36), page == i, Pages[i], button) && page != i)
                { page = i; scroll = Vector2.zero; UIClickSound.Play(); }
            changed = false;
            var data = GameSettings.Current;
            float contentHeight = page == 1 ? 706 : page == 0 ? 358 : 350;
            scroll = GUI.BeginScrollView(new Rect(510, 307, 580, 332), scroll, new Rect(0, 0, 553, contentHeight));
            if (page == 0) Gameplay(mission, data);
            else if (page == 1) Graphics(data);
            else if (page == 2) Audio(data);
            else Devices(mission);
            GUI.EndScrollView();
            pending |= changed;
            if (pending && (Event.current.rawType == EventType.MouseUp || Event.current.rawType == EventType.KeyUp))
            { GameSettings.Commit(mission); UIClickSound.Play(); pending = false; }
            if (GameSettings.LastStorageError != null) GUI.Label(new Rect(510, 643, 580, 24), "Changes apply now, but could not be saved to this device.", small);
            if (GUI.Button(new Rect(510, 677, 580, 46), "Back", button))
            { GameSettings.Commit(mission); UIClickSound.Play(); pending = false; return false; }
            return true;
        }

        void Gameplay(MissionManager mission, GameSettingsData data)
        {
            data.Difficulty = (FighterDifficulty)Choice(0, "AI difficulty", Difficulty, (int)data.Difficulty);
            data.LeadIndicator = Toggle(47, "Training lead indicator", data.LeadIndicator);
            data.UnlimitedAmmo = Toggle(91, "Unlimited ammunition", data.UnlimitedAmmo);
            data.ShowHUD = Toggle(135, "Flight HUD", data.ShowHUD);
            data.SimplifiedDamage = Toggle(179, "Simplified player damage", data.SimplifiedDamage);
            data.AdvancedFlight = Toggle(223, "Advanced aerodynamics", data.AdvancedFlight);
            mission.ShowDebug = Toggle(267, "Developer telemetry (F3)", mission.ShowDebug);
            GUI.Label(new Rect(0, 313, 545, 40), "Advanced mode includes stalls and simplified spin dynamics.\nDeveloper telemetry resets when the mission is reloaded.", small);
        }

        void Graphics(GameSettingsData data)
        {
            GUI.Label(new Rect(0, 0, 540, 26), data.CustomGraphics ? "Graphics preset — Custom" : "Graphics preset", text);
            for (int i = 0; i < 4; i++)
                if (GUI.Toggle(new Rect(i * 137, 34, 130, 34), !data.CustomGraphics && data.GraphicsPreset == i, Quality[i], button) && (data.CustomGraphics || data.GraphicsPreset != i))
                { data.ApplyPreset(i); changed = true; }
            int resolutionIndex = resolutions.IndexOf(new Vector2Int(data.ResolutionWidth, data.ResolutionHeight));
            if (resolutionIndex < 0)
            {
                resolutions.Add(new Vector2Int(data.ResolutionWidth, data.ResolutionHeight));
                resolutionLabels.Add(data.ResolutionWidth + " × " + data.ResolutionHeight);
                resolutionIndex = resolutions.Count - 1;
            }
            int selectedResolution = Choice(83, "Resolution", resolutionLabels, resolutionIndex);
            data.ResolutionWidth = resolutions[selectedResolution].x; data.ResolutionHeight = resolutions[selectedResolution].y;
            data.WindowMode = Choice(127, "Display mode", Windows, data.WindowMode);
            data.VSync = Toggle(171, "Vertical sync", data.VSync);
            int frameIndex = System.Array.IndexOf(FrameValues, data.FrameLimit);
            data.FrameLimit = FrameValues[Choice(215, "Frame limit", FrameLabels, frameIndex < 0 ? 4 : frameIndex)];
            bool earlierChange = changed; changed = false;
            data.RenderScale = Slider(259, "Render scale", data.RenderScale, .5f, 1.5f, true);
            data.TextureQuality = Choice(303, "Texture resolution", Textures, data.TextureQuality);
            data.ShadowQuality = Choice(347, "Shadows", Shadows, data.ShadowQuality);
            data.CloudQuality = Choice(391, "Cloud quality", Quality, data.CloudQuality);
            data.TerrainQuality = Choice(435, "Terrain quality", Quality, data.TerrainQuality);
            data.AntiAliasing = Choice(479, "Anti-aliasing", AA, data.AntiAliasing);
            data.EffectsQuality = Choice(523, "Effects quality", Quality, data.EffectsQuality);
            if (changed) data.CustomGraphics = true;
            changed |= earlierChange;
            GUI.Label(new Rect(0, 576, 540, 75), "VSync can override the frame limit.\nResolution and display mode apply when you release a control.\nCloud, terrain and effect quality affect visuals, never flight physics.", small);
        }

        void Audio(GameSettingsData data)
        {
            data.MasterVolume = Slider(0, "Master", data.MasterVolume);
            data.EngineVolume = Slider(48, "Engine", data.EngineVolume);
            data.WeaponVolume = Slider(96, "Weapons", data.WeaponVolume);
            data.EffectsVolume = Slider(144, "Effects / impacts", data.EffectsVolume);
            data.WindVolume = Slider(192, "Wind / slipstream", data.WindVolume);
            data.UIVolume = Slider(240, "Interface", data.UIVolume);
            if (changed) GameSettings.ApplyAudio();
            if (GUI.Button(new Rect(0, 292, 540, 36), "Preview interface sound", button)) UIClickSound.Play();
        }

        void Devices(MissionManager mission)
        {
            GUI.Label(new Rect(0, 0, 545, 96), "Bind each flight axis to your controller, joystick or pedals.\nThen use Device setup to capture travel, center, deadzone\nand direction while watching the live input meters.", small);
            if (GUI.Button(new Rect(0, 113, 540, 42), "Open bindings and device setup", button))
            { mission.Input.ShowBindings = true; UIClickSound.Play(); }
            GUI.Label(new Rect(0, 181, 545, 85), "Keyboard, gamepad and synthetic joystick checks run in\nacceptance tests. A passing simulation does not certify\na particular physical HOTAS or pedal device.", small);
        }

        bool Toggle(float y, string label, bool value)
        {
            bool next = GUI.Toggle(new Rect(0, y, 540, 34), value, (value ? "ON   " : "OFF   ") + label, button);
            if (next != value) changed = true;
            return next;
        }

        int Choice(float y, string label, IList<string> choices, int current)
        {
            GUI.Label(new Rect(0, y + 3, 200, 30), label, small);
            int next = current;
            if (GUI.Button(new Rect(203, y, 34, 34), "<", button)) next = (current + choices.Count - 1) % choices.Count;
            GUI.Label(new Rect(242, y + 3, 255, 30), choices[current], small);
            if (GUI.Button(new Rect(506, y, 34, 34), ">", button)) next = (current + 1) % choices.Count;
            if (next != current) changed = true;
            return next;
        }

        float Slider(float y, string label, float value, float min = 0, float max = 1, bool steps = false)
        {
            GUI.Label(new Rect(0, y, 245, 32), label + "  " + Mathf.RoundToInt(value * 100) + "%", small);
            float next = GUI.HorizontalSlider(new Rect(250, y + 12, 290, 24), value, min, max);
            if (steps) next = Mathf.Round(next * 20f) / 20f;
            if (!Mathf.Approximately(next, value)) changed = true;
            return next;
        }
    }
}
