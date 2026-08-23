using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Centralized player lookup and utility functions. Every role system that
    /// duplicates <c>FindPlayer</c> should use these instead.
    /// </summary>
    public static class PlayerUtils
    {
        /// <summary>Finds a PlayerControl by its PlayerId (byte). Returns null if not found.</summary>
        public static PlayerControl FindById(byte playerId)
        {
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null && p.Data != null && p.Data.PlayerId == playerId)
                    return p;
            return null;
        }

        /// <summary>Finds a PlayerControl by its OwnerId (int, the connection client id). Returns null if not found.</summary>
        public static PlayerControl FindByOwnerId(int ownerId)
        {
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null && p.OwnerId == ownerId)
                    return p;
            return null;
        }

        /// <summary>Returns the display name of a player, or "#N" if the player is not found.</summary>
        public static string NameSafe(byte playerId)
        {
            var p = FindById(playerId);
            return p != null && p.Data != null ? p.Data.PlayerName ?? ("#" + playerId) : "#" + playerId;
        }

        /// <summary>Returns the display name, or "#N".</summary>
        public static string NameSafe(PlayerControl player)
        {
            if (player == null) return "#?";
            return player.Data != null ? player.Data.PlayerName ?? ("#" + player.Data.PlayerId) : "#" + player.OwnerId;
        }

        /// <summary>True when a player is alive, connected, and not a dummy.</summary>
        public static bool IsValidTarget(PlayerControl player)
        {
            return player != null && player.Data != null && !player.Data.IsDead && !player.Data.Disconnected;
        }

        /// <summary>Returns all alive, connected players.</summary>
        public static List<PlayerControl> AllAlive()
        {
            var result = new List<PlayerControl>();
            foreach (var p in PlayerControl.AllPlayerControls)
                if (IsValidTarget(p))
                    result.Add(p);
            return result;
        }

        /// <summary>Returns all alive players on a given team.</summary>
        public static List<PlayerControl> AllAliveOnTeam(RoleTeamTypes team)
        {
            var result = new List<PlayerControl>();
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (!IsValidTarget(p)) continue;
                var role = p.Data.myRole;
                if (role != null && role.RoleTeamType == team)
                    result.Add(p);
            }
            return result;
        }

        /// <summary>Gets the distance between two players' world positions.</summary>
        public static float Distance(PlayerControl a, PlayerControl b)
        {
            if (a == null || b == null) return float.MaxValue;
            return Vector2.Distance(a.GetTruePosition(), b.GetTruePosition());
        }

        /// <summary>
        /// Finds the closest alive player (by world distance) to <paramref name="source"/>,
        /// within <paramref name="maxRange"/>. Returns null when no target is in range.
        /// </summary>
        public static PlayerControl ClosestAlive(PlayerControl source, float maxRange = float.MaxValue, Predicate<PlayerControl> filter = null)
        {
            if (source == null) return null;
            var srcPos = source.GetTruePosition();
            PlayerControl best = null;
            float bestDist = float.MaxValue;

            foreach (var p in AllAlive())
            {
                if (p == source) continue;
                if (filter != null && !filter(p)) continue;
                float dist = Vector2.Distance(srcPos, p.GetTruePosition());
                if (dist < bestDist && dist <= maxRange)
                {
                    bestDist = dist;
                    best = p;
                }
            }

            return best;
        }

        /// <summary>
        /// Teleports a player to a world position. Uses SnapTo for networked sync
        /// when the player is the local player; falls back to direct position set for
        /// other players (host-only: sync with the position over RPC if needed).
        /// </summary>
        public static void Teleport(PlayerControl player, Vector2 position)
        {
            if (player == null) return;
            if (player.NetTransform != null)
            {
                player.NetTransform.SnapTo(position);
                if (player.AmOwner)
                    player.NetTransform.RpcSnapTo(position);
            }
            else
            {
                player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
            }
        }

        /// <summary>
        /// Forces a player's layer to Ghost (dead). Useful for custom kills that skip
        /// the native murder flow.
        /// </summary>
        public static void SetGhost(PlayerControl player)
        {
            if (player == null) return;
            player.gameObject.layer = LayerMask.NameToLayer("Ghost");
        }

        /// <summary>
        /// Gets the lobby host's PlayerControl, or null.
        /// </summary>
        public static PlayerControl Host()
        {
            var client = AmongUsClient.Instance;
            if (client == null) return null;
            return FindByOwnerId(client.HostId);
        }
    }
}