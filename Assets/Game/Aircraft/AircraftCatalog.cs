using System;
using UnityEngine;

namespace PacificCombat
{
    [Serializable]
    public sealed class AircraftCatalogEntry
    {
        public AircraftType Type;
        public string ShortName;
        public string ModelResourcePath;
        [TextArea] public string FlyingAdvice;
        public AircraftData Aircraft;
        public WeaponData Weapons;
    }

    [CreateAssetMenu(menuName = "Pacific Combat/Aircraft Catalog")]
    public sealed class AircraftCatalog : ScriptableObject
    {
        public AircraftCatalogEntry[] Entries = Array.Empty<AircraftCatalogEntry>();
        public const string ResourcePath = "AircraftCatalog";

        public AircraftCatalogEntry Get(AircraftType type)
        {
            for (int i = 0; i < Entries.Length; i++)
                if (Entries[i] != null && Entries[i].Type == type && Entries[i].Aircraft && Entries[i].Weapons) return Entries[i];
            throw new InvalidOperationException("Aircraft catalog is missing a configured entry for " + type + ". Generate mission assets before launching.");
        }

        public static bool IsValidType(AircraftType type) => (int)type >= 0 && (int)type < 4;

        public static bool TryParseCode(string code, out AircraftType type)
        {
            type = AircraftType.P51D;
            if (string.IsNullOrWhiteSpace(code)) return false;
            switch (code.Trim().ToLowerInvariant())
            {
                case "p51": case "p51d": case "p-51": type = AircraftType.P51D; return true;
                case "zero": case "a6m": case "a6mzero": type = AircraftType.A6MZero; return true;
                case "bf109": case "bf-109": type = AircraftType.Bf109; return true;
                case "p38": case "p38lightning": case "p-38": type = AircraftType.P38Lightning; return true;
                default: return false;
            }
        }

        public static void ApplyCommandLine(GameSettingsData settings, string[] arguments)
        {
            if (settings == null || arguments == null) return;
            foreach (string argument in arguments)
            {
                if (string.IsNullOrEmpty(argument)) continue;
                bool player = argument.StartsWith("--player=", StringComparison.OrdinalIgnoreCase);
                bool enemy = argument.StartsWith("--enemy=", StringComparison.OrdinalIgnoreCase);
                if (!player && !enemy) continue;
                string code = argument.Substring(player ? 9 : 8);
                if (!TryParseCode(code, out var type))
                {
                    Debug.LogWarning("Unknown aircraft option '" + argument + "'. Use p51, zero, bf109 or p38; keeping the current selection.");
                    continue;
                }
                if (player) settings.PlayerAircraft = type; else settings.EnemyAircraft = type;
            }
            settings.Normalize();
        }
    }
}
