using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Small horizontal progress/cooldown bar widget. Sticks to the HUD and can
    /// show fill percentage with a colored bar on a dark background.
    ///
    /// Usage:
    /// <code>
    ///   var bar = CooldownBar.Create("Douse", Color.red);
    ///   // In FixedUpdate:
    ///   bar.SetProgress(timer / maxDuration);
    ///   // When done:
    ///   bar.Destroy();
    /// </code>
    /// </summary>
    public class CooldownBar
    {
        private readonly GameObject _go;
        private readonly SpriteRenderer _bg;
        private readonly SpriteRenderer _fill;
        private float _progress = 1f;

        /// <summary>
        /// Creates a cooldown bar at the given screen position (world units).
        /// Returns null when HudManager is not available.
        /// </summary>
        public static CooldownBar Create(string name, Color fillColor, Vector3 position,
            float width = 1.8f, float height = 0.18f)
        {
            var hud = HudManager.Instance;
            if (hud == null) return null;

            var go = new GameObject("CooldownBar_" + name);
            go.transform.SetParent(hud.transform, false);
            go.transform.localPosition = position;
            go.layer = hud.gameObject.layer;

            // Background.
            var bg = new GameObject("bg");
            bg.transform.SetParent(go.transform, false);
            bg.transform.localPosition = Vector3.zero;
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = CreatePixelSprite();
            bgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
            bg.transform.localScale = new Vector3(width, height, 1f);

            // Fill.
            var fill = new GameObject("fill");
            fill.transform.SetParent(go.transform, false);
            fill.transform.localPosition = new Vector3(-width / 2f, 0f, -0.1f);
            fill.transform.localScale = new Vector3(width, height, 1f);
            var fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite = CreatePixelSprite();
            fillSr.color = fillColor;
            fillSr.size = new Vector2(1f, 1f);

            fill.transform.SetLocalPivot(new Vector2(0f, 0.5f));

            return new CooldownBar(go, bgSr, fillSr);
        }

        private CooldownBar(GameObject go, SpriteRenderer bg, SpriteRenderer fill)
        {
            _go = go;
            _bg = bg;
            _fill = fill;
        }

        /// <summary>Updates the bar fill (0 = empty, 1 = full). Color tracks the fill.</summary>
        public void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
            if (_fill != null)
            {
                _fill.transform.localScale = new Vector3(
                    _bg != null ? _bg.transform.localScale.x * _progress : 0.5f * _progress,
                    _fill.transform.localScale.y,
                    1f);

                // Gradient: red (low) → yellow (mid) → green (full).
                _fill.color = _progress < 0.3f
                    ? Color.Lerp(Color.red, Color.yellow, _progress / 0.3f)
                    : Color.Lerp(Color.yellow, Color.green, (_progress - 0.3f) / 0.7f);
            }
        }

        /// <summary>Show or hide the entire bar.</summary>
        public void SetVisible(bool visible)
        {
            if (_go != null) _go.SetActive(visible);
        }

        /// <summary>Returns the current progress value.</summary>
        public float Progress => _progress;

        /// <summary>Destroys the bar.</summary>
        public void Destroy()
        {
            if (_go != null) UnityEngine.Object.Destroy(_go);
        }

        private static Sprite CreatePixelSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    /// <summary>Extension for setting a RectTransform's pivot via a Transform.</summary>
    internal static class TransformPivotExtensions
    {
        public static void SetLocalPivot(this Transform t, Vector2 pivot)
        {
            // Adjust local position so the pivot stays in place.
            var rect = t.GetComponent<RectTransform>();
            if (rect != null)
            {
                var delta = pivot - rect.pivot;
                rect.pivot = pivot;
                rect.localPosition += new Vector3(delta.x * rect.sizeDelta.x, delta.y * rect.sizeDelta.y, 0f);
            }
        }
    }
}