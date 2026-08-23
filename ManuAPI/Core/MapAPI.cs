using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Map-level query API: vents, consoles, rooms, and ShipStatus helpers.
    /// All lookups go through the authoritative <see cref="ShipStatus.Instance"/>.
    /// </summary>
    public static class MapAPI
    {
        /// <summary>True when ShipStatus.Instance exists and the game is in a playable state.</summary>
        public static bool IsGameActive => ShipStatus.Instance != null;

        /// <summary>The current map type, or -1 if no ShipStatus is loaded.</summary>
        public static byte MapId => ShipStatus.Instance != null ? (byte)ShipStatus.Instance.Type : (byte)255;

        /// <summary>Human-readable map name. Returns "Unknown" when not in a game.</summary>
        public static string MapName => MapId switch
        {
            0 => "The Skeld",
            1 => "MIRA HQ",
            2 => "Polus",
            4 => "The Airship",
            5 => "The Fungle",
            _ => "Unknown"
        };

        /// <summary>Returns all vents on the current map.</summary>
        public static Vent[] AllVents()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllVents == null) return Array.Empty<Vent>();
            var arr = ShipStatus.Instance.AllVents;
            var result = new Vent[arr.Count];
            for (int i = 0; i < arr.Count; i++) result[i] = arr[i];
            return result;
        }

        /// <summary>
        /// Finds a vent by its Id (the network id used in RpcEnterVent/RpcExitVent).
        /// </summary>
        public static Vent FindVentById(int ventId)
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllVents == null) return null;
            foreach (var vent in ShipStatus.Instance.AllVents)
                if (vent != null && vent.Id == ventId)
                    return vent;
            return null;
        }

        /// <summary>
        /// Returns the closest vent to a world position, within <paramref name="maxRange"/>.
        /// </summary>
        public static Vent ClosestVent(Vector2 position, float maxRange = 2f)
        {
            Vent best = null;
            float bestDist = maxRange;
            foreach (var vent in AllVents())
            {
                if (vent == null) continue;
                float dist = Vector2.Distance(position, vent.transform.position);
                if (dist < bestDist) { bestDist = dist; best = vent; }
            }
            return best;
        }

        /// <summary>
        /// Returns connected vents for a given vent. Returns empty array on failure.
        /// </summary>
        public static Vent[] ConnectedVents(Vent vent)
        {
            if (vent == null || vent.connectedVents == null) return Array.Empty<Vent>();
            return vent.connectedVents.ToArray();
        }

        /// <summary>
        /// Returns all console objects (tasks, emergency button, admin table, etc.)
        /// on the current map.
        /// </summary>
        public static Console[] AllConsoles()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllConsoles == null) return Array.Empty<Console>();
            var arr = ShipStatus.Instance.AllConsoles;
            var result = new Console[arr.Count];
            for (int i = 0; i < arr.Count; i++) result[i] = arr[i];
            return result;
        }

        /// <summary>
        /// Finds the closest console to a world position, within <paramref name="maxRange"/>.
        /// Optionally filter by a predicate (e.g. only tasks, only usable consoles).
        /// </summary>
        public static Console ClosestConsole(Vector2 position, float maxRange = 2f, Predicate<Console> filter = null)
        {
            Console best = null;
            float bestDist = maxRange;
            foreach (var con in AllConsoles())
            {
                if (con == null) continue;
                if (filter != null && !filter(con)) continue;
                float dist = Vector2.Distance(position, con.transform.position);
                if (dist < bestDist) { bestDist = dist; best = con; }
            }
            return best;
        }

        /// <summary>
        /// Returns all doors on the current map (Skeld/Polus/Airship have doors;
        /// MIRA HQ and Fungle may return empty).
        /// </summary>
        public static PlainDoor[] AllDoors()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return Array.Empty<PlainDoor>();
            var arr = ShipStatus.Instance.AllDoors;
            var result = new PlainDoor[arr.Count];
            for (int i = 0; i < arr.Count; i++) result[i] = arr[i];
            return result;
        }

        /// <summary>
        /// Closes all doors of a given system type. Host-only (uses RpcRepairSystem).
        /// The system type is map-dependent (e.g. 0 = Skeld doors, 16 = Polus doors).
        /// </summary>
        public static void CloseDoors(byte systemType)
        {
            if (ShipStatus.Instance == null) return;
            ShipStatus.Instance.RpcCloseDoorsOfType((SystemTypes)systemType);
        }

        /// <summary>
        /// Repairs (opens) a system. Host-only.
        /// </summary>
        public static void RepairSystem(byte systemType, byte amount = 0)
        {
            if (ShipStatus.Instance == null) return;
            ShipStatus.Instance.RpcRepairSystem((SystemTypes)systemType, amount);
        }

        /// <summary>
        /// Gets a random spawn position on the current map for a player.
        /// Falls back to (0,0) when no map is loaded.
        /// </summary>
        public static Vector2 RandomSpawnPosition()
        {
            var ship = ShipStatus.Instance;
            if (ship == null) return Vector2.zero;

            var center = ship.MeetingSpawnCenter != null
                ? (Vector2)ship.MeetingSpawnCenter.transform.position
                : Vector2.zero;

            var radius = ship.SpawnRadius > 0f ? ship.SpawnRadius : 3f;
            return center + UnityEngine.Random.insideUnitCircle * radius;
        }
    }
}