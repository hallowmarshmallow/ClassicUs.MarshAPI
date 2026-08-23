using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Persistent screen-space text overlay. Renders arbitrary text lines
    /// at fixed screen positions (top-left, bottom-right, etc.). Useful for
    /// debugging, role info panels, or game-mode status displays.
    ///
    /// Lines persist until cleared; no auto-dismiss.
    ///
    /// Usage:
    /// <code>
    ///   OverlayAPI.SetLine(0, "Sheriff shots: 2/3", Color.green, OverlayAPI.Corner.TopRight);
    ///   OverlayAPI.Tick(); // call every frame
    /// </code>
    /// </summary>
    public static class OverlayAPI
    {
        public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight, Center }

        private static readonly List<OverlayLine> _lines = new();
        private static bool _initialized;

        /// <summary>
        /// Sets a persistent text line at the given slot index and screen corner.
        /// Pass null or empty string to clear that slot.
        /// </summary>
        public static void SetLine(int slot, string text, Color color, Corner corner = Corner.TopLeft, float fontSize = 2f)
        {
            while (_lines.Count <= slot) _lines.Add(new OverlayLine());
            var line = _lines[slot];
            line.Text = text;
            line.Color = color;
            line.Corner = corner;
            line.FontSize = fontSize;
            line.Visible = !string.IsNullOrEmpty(text);
            if (line.Visible && line.GameObject == null) CreateGameObject(line);
        }

        /// <summary>Clears a specific slot.</summary>
        public static void ClearSlot(int slot)
        {
            if (slot < _lines.Count)
                SetLine(slot, null, Color.white, Corner.TopLeft);
        }

        /// <summary>Clears all overlay lines.</summary>
        public static void Clear()
        {
            foreach (var line in _lines)
                if (line.GameObject != null) UnityEngine.Object.Destroy(line.GameObject);
            _lines.Clear();
        }

        /// <summary>
        /// Call every frame from your HUD update. Re-positions and renders
        /// all active lines.
        /// </summary>
        public static void Tick()
        {
            if (!_initialized) { _initialized = true; GameEvents.GameEnded += _ => Clear(); }

            for (int i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                if (!line.Visible) continue;
                if (line.GameObject == null) CreateGameObject(line);
                if (line.GameObject == null) continue;

                line.TextMesh.text = line.Text;
                line.TextMesh.color = line.Color;
                line.TextMesh.fontSize = line.FontSize;

                var cam = HudManager.Instance?.UICamera;
                Vector3 worldPos;
                switch (line.Corner)
                {
                    case Corner.TopLeft:
                        worldPos = cam != null ? cam.ScreenToWorldPoint(new Vector3(80, Screen.height - 100 - i * 35, 1f)) : Vector3.zero;
                        break;
                    case Corner.TopRight:
                        worldPos = cam != null ? cam.ScreenToWorldPoint(new Vector3(Screen.width - 200, Screen.height - 100 - i * 35, 1f)) : Vector3.zero;
                        break;
                    case Corner.BottomLeft:
                        worldPos = cam != null ? cam.ScreenToWorldPoint(new Vector3(80, 80 + i * 35, 1f)) : Vector3.zero;
                        break;
                    case Corner.BottomRight:
                        worldPos = cam != null ? cam.ScreenToWorldPoint(new Vector3(Screen.width - 200, 80 + i * 35, 1f)) : Vector3.zero;
                        break;
                    default:
                        worldPos = cam != null ? cam.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f - i * 35, 1f)) : Vector3.zero;
                        break;
                }
                line.GameObject.transform.position = worldPos;
            }
        }

        private static void CreateGameObject(OverlayLine line)
        {
            var hud = HudManager.Instance;
            if (hud == null) return;

            var go = new GameObject("OverlayAPI_" + _lines.IndexOf(line));
            go.transform.SetParent(hud.transform, false);
            go.layer = hud.gameObject.layer;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize = 2f;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.enableWordWrapping = false;
            tmp.sortingOrder = 20;

            line.GameObject = go;
            line.TextMesh = tmp;
        }

        /// <summary>
        /// Shows a large centered banner that fades and auto-dismisses.
        /// Good for victory/defeat announcements, round-start messages, etc.
        /// </summary>
        public static void ShowBanner(string text, Color color, float fontSize = 5f, float duration = 3f)
        {
            var hud = HudManager.Instance;
            if (hud == null) return;

            var go = new GameObject("OverlayAPI_Banner");
            go.transform.SetParent(hud.transform, false);
            go.transform.localPosition = new Vector3(0f, 1f, -5f);
            go.layer = hud.gameObject.layer;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.rectTransform.sizeDelta = new Vector2(8f, 3f);
            tmp.sortingOrder = 25;

            // Auto-destroy after duration via CoroutineRunner.
            CoroutineRunner.Start(BannerFadeRoutine(go, tmp, duration));
        }

        private static System.Collections.IEnumerator BannerFadeRoutine(GameObject go, TextMeshPro tmp, float duration)
        {
            float elapsed = 0f;
            var startColor = tmp.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Fade out in last 20% of duration.
                float alpha = t < 0.8f ? 1f : 1f - (t - 0.8f) / 0.2f;
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            UnityEngine.Object.Destroy(go);
        }

        private sealed class OverlayLine
        {
            public GameObject GameObject;
            public TextMeshPro TextMesh;
            public string Text;
            public Color Color = Color.white;
            public Corner Corner;
            public float FontSize = 2f;
            public bool Visible;
        }
    }
}