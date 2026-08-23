using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Safe factory for creating custom clickable buttons in the HUD. Instead of
    /// cloning the KillButton (which copies its IL2CPP component tree and can
    /// trigger native delegate marshalling crashes), this creates minimal
    /// GameObjects from scratch: a sprite, a TextMeshPro label, and a PassiveButton
    /// driven by a managed click counter (no OnClick.AddListener).
    ///
    /// Usage:
    /// <code>
    ///   var btn = ButtonFactory.Create("MyButton", mySprite, new Vector3(2f, 1f, 0f));
    ///   btn.SetActive(true);
    ///   // In your HUD update loop:
    ///   if (btn.WasClicked()) DoThing();
    /// </code>
    /// </summary>
    public static class ButtonFactory
    {
        /// <summary>Creates a new button in the HUD. Returns a handle for ticking / cleanup.</summary>
        public static ManagedButton Create(string name, Sprite icon, Vector3 position,
            AspectPosition.EdgeAlignments alignment = AspectPosition.EdgeAlignments.RightBottom)
        {
            var hud = HudManager.Instance;
            if (hud == null || hud.KillButton == null) return ManagedButton.Invalid;

            var go = new GameObject(name);
            go.transform.SetParent(hud.transform, false);
            go.layer = hud.gameObject.layer;

            // Sprite renderer.
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = icon;
            sr.material = hud.KillButton.graphic?.material;
            sr.size = new Vector2(0.55f, 0.55f);

            // Position via AspectPosition (same anchoring as vanilla buttons).
            var ap = go.AddComponent<AspectPosition>();
            ap.parentCam = hud.UICamera;
            ap.Alignment = alignment;
            ap.DistanceFromEdge = position;
            ap.updateAlways = true;
            ap.AdjustPosition();

            // Minimal PassiveButton for click detection.
            var pb = go.AddComponent<PassiveButton>();
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.55f, 0.55f);
            collider.isTrigger = true;
            pb.ClickMask = LayerMask.GetMask("UI");

            var managed = new ManagedButton(go, sr, pb);
            return managed;
        }

        /// <summary>
        /// Creates a text-only button (no icon). Good for small action labels like
        /// "Swap" or "Skip".
        /// </summary>
        public static ManagedButton CreateText(string name, string label, Color color,
            Vector3 position, float fontSize = 1.8f,
            AspectPosition.EdgeAlignments alignment = AspectPosition.EdgeAlignments.RightBottom)
        {
            var hud = HudManager.Instance;
            if (hud == null) return ManagedButton.Invalid;

            var go = new GameObject(name);
            go.transform.SetParent(hud.transform, false);
            go.layer = hud.gameObject.layer;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.fontStyle = FontStyles.Bold;

            var ap = go.AddComponent<AspectPosition>();
            ap.parentCam = hud.UICamera;
            ap.Alignment = alignment;
            ap.DistanceFromEdge = position;
            ap.updateAlways = true;
            ap.AdjustPosition();

            var pb = go.AddComponent<PassiveButton>();
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 0.6f);
            collider.isTrigger = true;
            pb.ClickMask = LayerMask.GetMask("UI");

            return new ManagedButton(go, null, pb);
        }
    }

    /// <summary>
    /// Handle returned by ButtonFactory. Tick it every frame in your HUD update
    /// and call <see cref="WasClicked"/> to detect clicks. Destroy with
    /// <see cref="Destroy"/> when done.
    /// </summary>
    public sealed class ManagedButton
    {
        internal static readonly ManagedButton Invalid = new(null, null, null);

        private readonly GameObject _go;
        private readonly SpriteRenderer _sr;
        private readonly PassiveButton _pb;
        private int _lastClickCount;

        /// <summary>True when the button is valid and not yet destroyed.</summary>
        public bool IsValid => _go != null;

        /// <summary>The underlying GameObject. Read-only for visibility/position tweaks.</summary>
        public GameObject GameObject => _go;

        /// <summary>The SpriteRenderer, or null for text buttons.</summary>
        public SpriteRenderer SpriteRenderer => _sr;

        internal ManagedButton(GameObject go, SpriteRenderer sr, PassiveButton pb)
        {
            _go = go;
            _sr = sr;
            _pb = pb;
        }

        /// <summary>Show or hide the button.</summary>
        public void SetActive(bool active)
        {
            if (_go != null) _go.SetActive(active);
        }

        /// <summary>Change the icon. Null-safe.</summary>
        public void SetIcon(Sprite sprite)
        {
            if (_sr != null) _sr.sprite = sprite;
        }

        /// <summary>Gray out the button during cooldown.</summary>
        public void SetGrayedOut(bool grayed)
        {
            if (_sr != null)
                _sr.color = grayed ? new Color(0.35f, 0.35f, 0.35f, 1f) : Color.white;
        }

        /// <summary>Returns true if the button was clicked this frame.</summary>
        public bool WasClicked()
        {
            if (_pb == null) return false;
            int current = _pb.GetClickCount();
            if (current > _lastClickCount)
            {
                _lastClickCount = current;
                return true;
            }
            return false;
        }

        /// <summary>Destroys the button GameObject and all components.</summary>
        public void Destroy()
        {
            if (_go != null) UnityEngine.Object.Destroy(_go);
        }
    }
}