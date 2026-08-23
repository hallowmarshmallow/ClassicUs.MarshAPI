using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Player nameplate and floating-indicator overlay API. Changes a player's
    /// name color, shows colored status labels under their name, and attaches
    /// floating world-space indicators above characters.
    ///
    /// All effects are local-only — sync state through Reactor RPCs for
    /// cross-client visibility.
    /// </summary>
    public static class HudOverlay
    {
        private static readonly Dictionary<byte, List<SubTextEntry>> _subTexts = new();
        private static readonly Dictionary<byte, GameObject> _indicators = new();

        /// <summary>Sets a player's name color. Pass null to restore default.</summary>
        public static void SetNameColor(byte playerId, Color? color)
        {
            var p = PlayerUtils.FindById(playerId);
            if (p == null || p.cosmetics == null || p.cosmetics.nameText == null) return;
            var target = color ?? Color.white;
            p.cosmetics.nameText.color = target;
        }

        /// <summary>Adds or updates a colored text line under a player's name.</summary>
        public static void SetSubText(byte playerId, string key, string text, Color color)
        {
            if (!_subTexts.TryGetValue(playerId, out var list))
            {
                list = new List<SubTextEntry>();
                _subTexts[playerId] = list;
            }
            list.RemoveAll(e => e.Key == key);
            if (!string.IsNullOrEmpty(text))
                list.Add(new SubTextEntry { Key = key, Text = text, Color = color });
            RebuildSubTexts(playerId, list);
        }

        /// <summary>Removes a sub-text line by key.</summary>
        public static void RemoveSubText(byte playerId, string key)
        {
            SetSubText(playerId, key, null, Color.clear);
        }

        /// <summary>Clears all sub-texts for a player.</summary>
        public static void ClearSubTexts(byte playerId)
        {
            _subTexts.Remove(playerId);
            RebuildSubTexts(playerId, null);
        }

        /// <summary>Creates a floating TextMeshPro above a player.</summary>
        public static TextMeshPro ShowFloatingText(byte playerId, string text, Color color,
            float fontSize = 2.5f, Vector3? offset = null)
        {
            var player = PlayerUtils.FindById(playerId);
            if (player == null) return null;
            RemoveIndicator(playerId);

            var go = new GameObject("HudOverlay_" + playerId);
            go.transform.SetParent(player.transform, false);
            go.transform.localPosition = offset ?? new Vector3(0f, 1.5f, -0.1f);
            go.layer = player.gameObject.layer;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.fontStyle = FontStyles.Bold;
            tmp.sortingOrder = 1;

            _indicators[playerId] = go;
            return tmp;
        }

        /// <summary>Destroys the floating indicator for a player.</summary>
        public static void RemoveIndicator(byte playerId)
        {
            if (_indicators.TryGetValue(playerId, out var go))
            {
                _indicators.Remove(playerId);
                if (go != null) UnityEngine.Object.Destroy(go);
            }
        }

        /// <summary>Destroys all indicators and sub-texts. Call from GameStarted/GameEnded.</summary>
        public static void ResetAll()
        {
            foreach (var go in _indicators.Values)
                if (go != null) UnityEngine.Object.Destroy(go);
            _indicators.Clear();
            _subTexts.Clear();
        }

        /// <summary>Returns current sub-text entries for a player.</summary>
        public static IReadOnlyList<(string Key, string Text, Color Color)> GetSubTexts(byte playerId)
        {
            if (!_subTexts.TryGetValue(playerId, out var list))
                return Array.Empty<(string, string, Color)>();
            var result = new (string, string, Color)[list.Count];
            for (int i = 0; i < list.Count; i++)
                result[i] = (list[i].Key, list[i].Text, list[i].Color);
            return result;
        }

        private static void RebuildSubTexts(byte playerId, List<SubTextEntry> entries)
        {
            // Sub-text lines are created as children of the player's transform,
            // positioned below the main nameplate. Each entry gets its own TMP object.
            var player = PlayerUtils.FindById(playerId);
            if (player == null) return;

            // Remove old sub-text objects.
            for (int i = player.transform.childCount - 1; i >= 0; i--)
            {
                var child = player.transform.GetChild(i);
                if (child.name.StartsWith("HudOverlay_Sub_"))
                    UnityEngine.Object.Destroy(child.gameObject);
            }

            if (entries == null || entries.Count == 0) return;

            float baseY = -0.4f;
            float lineHeight = 0.25f;
            for (int i = 0; i < entries.Count; i++)
            {
                var go = new GameObject("HudOverlay_Sub_" + entries[i].Key);
                go.transform.SetParent(player.transform, false);
                go.transform.localPosition = new Vector3(0f, baseY - lineHeight * i, -0.1f);
                go.layer = player.gameObject.layer;

                var tmp = go.AddComponent<TextMeshPro>();
                tmp.text = entries[i].Text;
                tmp.fontSize = 1.8f;
                tmp.color = entries[i].Color;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
                tmp.sortingOrder = 1;
            }
        }

        private sealed class SubTextEntry
        {
            public string Key;
            public string Text;
            public Color Color;
        }
    }
}