using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Timed on-screen notification system. Shows colored text messages that fade
    /// and auto-dismiss after a configurable duration. Multiple notifications
    /// stack vertically in the top-center of the screen.
    ///
    /// Usage:
    /// <code>
    ///   NotificationAPI.Show("Jester Wins!", Color.magenta, 3f);
    ///   NotificationAPI.Show("Body reported!", Color.yellow, 2f);
    /// </code>
    ///
    /// Call <see cref="Tick"/> every frame from your HUD update loop.
    /// </summary>
    public static class NotificationAPI
    {
        private static readonly List<ActiveNotification> _active = new();
        private static bool _initialized;

        /// <summary>Shows a notification that auto-dismisses after the given duration (seconds).</summary>
        public static void Show(string message, Color color, float duration = 2f)
        {
            if (!_initialized) Initialize();
            _active.Add(new ActiveNotification
            {
                Message = message,
                Color = color,
                Duration = duration,
                Remaining = duration,
                TargetAlpha = 1f,
            });
        }

        /// <summary>
        /// Call every frame from HudManager.Update or FixedUpdate. Renders all
        /// active notifications.
        /// </summary>
        public static void Tick()
        {
            if (_active.Count == 0) return;

            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var n = _active[i];
                n.Remaining -= dt;

                // Fade out in last 0.5s.
                if (n.Remaining < 0.5f)
                    n.TargetAlpha = Mathf.Max(0f, n.Remaining / 0.5f);

                if (n.Remaining <= 0f)
                {
                    if (n.GameObject != null) Object.Destroy(n.GameObject);
                    _active.RemoveAt(i);
                }
                else
                {
                    RenderNotification(n, i);
                }
            }
        }

        /// <summary>Immediately dismisses all notifications.</summary>
        public static void Clear()
        {
            foreach (var n in _active)
                if (n.GameObject != null) Object.Destroy(n.GameObject);
            _active.Clear();
        }

        private static void Initialize()
        {
            _initialized = true;
            GameEvents.GameEnded += _ => Clear();
        }

        private static void RenderNotification(ActiveNotification notification, int index)
        {
            var hud = HudManager.Instance;
            if (hud == null) return;

            if (notification.GameObject == null)
            {
                var go = new GameObject("ManuAPI_Notif_" + index);
                go.transform.SetParent(hud.transform, false);
                go.transform.localPosition = new Vector3(0f, 2.5f - index * 0.6f, -5f);
                go.layer = hud.gameObject.layer;

                var tmp = go.AddComponent<TextMeshPro>();
                tmp.fontSize = 2.4f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = true;
                tmp.rectTransform.sizeDelta = new Vector2(6f, 1f);
                tmp.sortingOrder = 10;

                notification.GameObject = go;
                notification.TextMesh = tmp;
            }

            notification.TextMesh.text = notification.Message;
            notification.TextMesh.color = new Color(
                notification.Color.r,
                notification.Color.g,
                notification.Color.b,
                notification.TargetAlpha);
        }

        private sealed class ActiveNotification
        {
            public GameObject GameObject;
            public TextMeshPro TextMesh;
            public string Message;
            public Color Color;
            public float Duration;
            public float Remaining;
            public float TargetAlpha;
        }
    }
}