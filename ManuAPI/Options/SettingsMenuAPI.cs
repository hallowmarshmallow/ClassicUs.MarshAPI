using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    // ──────────────────────────────────────────────
    // Builder — constructs rows from NumberOption template
    // ──────────────────────────────────────────────

    public class SettingsMenuBuilder
    {
        private readonly GameSettingMenu _menu;
        private readonly Transform _parent;
        private readonly NumberOption _template;
        private readonly bool _isHost;
        private readonly int _baseItemCount;
        private int _row;

        internal SettingsMenuBuilder(GameSettingMenu menu, Transform parent, NumberOption template, int startRow)
        {
            _menu = menu;
            _parent = parent;
            _template = template;
            _baseItemCount = menu.AllItems?.Count ?? 0;
            _row = startRow;
            _isHost = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
        }

        // ── Section header ──────────────────────────
        /// <summary>Adds a bold section title with no interactive controls.</summary>
        public TextMeshPro AddSectionHeader(string label, Color? color = null)
        {
            var (target, valueText) = CloneRow("Section_" + label.Replace(" ", "_"));
            _row++;

            if (valueText != null)
            {
                valueText.text = label;
                valueText.fontSize = 2.8f;
                valueText.fontStyle = FontStyles.Bold;
                valueText.color = color ?? new Color(0.9f, 0.85f, 0.4f, 1f); // gold
            }

            // Remove arrow buttons from a header row.
            foreach (var pb in target.GetComponentsInChildren<PassiveButton>())
                if (pb != null) pb.gameObject.SetActive(false);

            // Kill the title text so only the value shows (centered).
            var no = target.GetComponent<NumberOption>();
            if (no != null && no.TitleText != null)
                no.TitleText.text = "";
            if (no != null) UnityEngine.Object.Destroy(no);

            return valueText;
        }

        // ── Toggle ──────────────────────────────────
        public TextMeshPro AddToggle(string name, string label, Func<bool> getter, Action<bool> setter)
        {
            var (target, valueText) = CloneRow(name);
            _row++;

            if (valueText != null) valueText.text = getter() ? "On" : "Off";

            foreach (var pb in target.GetComponentsInChildren<PassiveButton>())
            {
                if (pb == null) continue;
                pb.gameObject.SetActive(_isHost);
                if (!_isHost || pb.OnClick == null) continue;
                pb.OnClick.RemoveAllListeners();
                var capturedText = valueText;
                pb.OnClick.AddListener(() =>
                {
                    setter(!getter());
                    if (capturedText != null) capturedText.text = getter() ? "On" : "Off";
                });
            }

            return valueText;
        }

        // ── Numeric (step ±) ───────────────────────
        public TextMeshPro AddNumeric(string name, string label, float step, float min, float max,
            string format, Func<float> getter, Action<float> setter)
        {
            var (target, valueText) = CloneRow(name);
            _row++;

            if (valueText != null) valueText.text = getter().ToString(format);

            var buttons = new List<PassiveButton>();
            foreach (var b in target.GetComponentsInChildren<PassiveButton>())
                if (b != null) buttons.Add(b);
            buttons.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

            foreach (var pb in buttons) pb.gameObject.SetActive(_isHost);

            if (_isHost && buttons.Count >= 2)
            {
                var dec = buttons[0];
                var inc = buttons[buttons.Count - 1];
                var capturedText = valueText;

                dec.OnClick.RemoveAllListeners();
                dec.OnClick.AddListener(() =>
                {
                    float val = Math.Max(min, getter() - step);
                    setter(val);
                    if (capturedText != null) capturedText.text = getter().ToString(format);
                });

                inc.OnClick.RemoveAllListeners();
                inc.OnClick.AddListener(() =>
                {
                    float val = Math.Min(max, getter() + step);
                    setter(val);
                    if (capturedText != null) capturedText.text = getter().ToString(format);
                });
            }

            return valueText;
        }

        // ── Dropdown (cycle choices) ───────────────
        /// <summary>Adds a dropdown that cycles through string choices.</summary>
        public TextMeshPro AddDropdown(string name, string label, string[] choices,
            Func<int> getIndex, Action<int> setIndex)
        {
            var (target, valueText) = CloneRow(name);
            _row++;

            if (valueText != null && choices.Length > 0)
                valueText.text = choices[Math.Max(0, Math.Min(getIndex(), choices.Length - 1))];

            var buttons = new List<PassiveButton>();
            foreach (var b in target.GetComponentsInChildren<PassiveButton>())
                if (b != null) buttons.Add(b);
            buttons.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

            foreach (var pb in buttons) pb.gameObject.SetActive(_isHost);

            if (_isHost && buttons.Count >= 2)
            {
                var dec = buttons[0];
                var inc = buttons[buttons.Count - 1];
                var capturedText = valueText;

                dec.OnClick.RemoveAllListeners();
                dec.OnClick.AddListener(() =>
                {
                    int idx = getIndex();
                    idx = (idx - 1 + choices.Length) % choices.Length;
                    setIndex(idx);
                    if (capturedText != null && choices.Length > 0)
                        capturedText.text = choices[idx];
                });

                inc.OnClick.RemoveAllListeners();
                inc.OnClick.AddListener(() =>
                {
                    int idx = getIndex();
                    idx = (idx + 1) % choices.Length;
                    setIndex(idx);
                    if (capturedText != null && choices.Length > 0)
                        capturedText.text = choices[idx];
                });
            }

            return valueText;
        }

        // ── Button (action) ─────────────────────────
        /// <summary>Adds a single action button. The label is the button text.</summary>
        public void AddButton(string name, string label, Action onClick, Color? color = null)
        {
            var (target, valueText) = CloneRow(name);
            _row++;

            // Repurpose the value text as the button label.
            if (valueText != null)
            {
                valueText.text = label;
                valueText.color = color ?? new Color(0.3f, 0.8f, 1f, 1f);
                valueText.fontStyle = FontStyles.Bold;
            }

            // Hide the left/right arrows; use any PassiveButton as the click target.
            var buttons = new List<PassiveButton>();
            foreach (var b in target.GetComponentsInChildren<PassiveButton>())
                if (b != null) buttons.Add(b);

            if (buttons.Count > 0)
            {
                // Keep one visible for clicking; hide the rest.
                for (int i = 0; i < buttons.Count; i++)
                    buttons[i].gameObject.SetActive(i == 0 && _isHost);

                if (_isHost)
                {
                    buttons[0].OnClick.RemoveAllListeners();
                    buttons[0].OnClick.AddListener(() => onClick());
                }
            }
        }

        // ── Slider (continuous float) ───────────────
        /// <summary>Adds a slider-like control using ± buttons with a fine step.</summary>
        public TextMeshPro AddSlider(string name, string label, float step, float min, float max,
            string format, Func<float> getter, Action<float> setter)
        {
            // Reuses AddNumeric with a small step — visual is the same.
            return AddNumeric(name, label, step, min, max, format, getter, setter);
        }

        // ── Color picker (cycle Palette colors) ─────
        /// <summary>Adds a color picker that cycles through Palette.PlayerColors.</summary>
        public TextMeshPro AddColorPicker(string name, string label, Func<int> getColorIndex, Action<int> setColorIndex)
        {
            var palette = Palette.PlayerColors;
            var names = palette != null
                ? new string[Math.Min(palette.Length, 18)]
                : new[] { "Red", "Blue", "Green", "Pink", "Orange", "Yellow", "Black", "White", "Purple", "Brown", "Cyan", "Lime" };

            if (palette != null)
                for (int i = 0; i < names.Length; i++)
                    names[i] = "Color " + i;

            var (target, valueText) = CloneRow(name);
            _row++;

            if (valueText != null)
            {
                int idx = Math.Max(0, Math.Min(getColorIndex(), names.Length - 1));
                valueText.text = names[idx];
                if (palette != null && idx < palette.Length)
                    valueText.color = palette[idx];
            }

            var buttons = new List<PassiveButton>();
            foreach (var b in target.GetComponentsInChildren<PassiveButton>())
                if (b != null) buttons.Add(b);
            buttons.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

            foreach (var pb in buttons) pb.gameObject.SetActive(_isHost);

            if (_isHost && buttons.Count >= 2)
            {
                var dec = buttons[0];
                var inc = buttons[buttons.Count - 1];
                var capturedText = valueText;

                dec.OnClick.RemoveAllListeners();
                dec.OnClick.AddListener(() =>
                {
                    int idx = (getColorIndex() - 1 + names.Length) % names.Length;
                    setColorIndex(idx);
                    if (capturedText != null)
                    {
                        capturedText.text = names[idx];
                        if (palette != null && idx < palette.Length)
                            capturedText.color = palette[idx];
                    }
                });

                inc.OnClick.RemoveAllListeners();
                inc.OnClick.AddListener(() =>
                {
                    int idx = (getColorIndex() + 1) % names.Length;
                    setColorIndex(idx);
                    if (capturedText != null)
                    {
                        capturedText.text = names[idx];
                        if (palette != null && idx < palette.Length)
                            capturedText.color = palette[idx];
                    }
                });
            }

            return valueText;
        }

        /// <summary>Expands the menu scroller to fit extra rows.</summary>
        public void ExpandScroller(float extraRows)
        {
            var scroller = _parent.GetComponentInParent<Scroller>();
            if (scroller == null || scroller.YBounds == null) return;
            var yb = scroller.YBounds;
            scroller.YBounds = new FloatRange(yb.min, yb.max + extraRows);
        }

        // ── internal helpers ────────────────────────

        private (Transform target, TextMeshPro valueText) CloneRow(string name)
        {
            var existing = _parent.Find(name);
            if (existing != null)
            {
                float yPos = _menu.YStart - (_baseItemCount + _row) * _menu.YOffset;
                existing.localPosition = new Vector3(existing.localPosition.x, yPos, existing.localPosition.z);
                Track(existing);
                return (existing, existing.GetComponentInChildren<TextMeshPro>());
            }

            var go = UnityEngine.Object.Instantiate(_template.gameObject, _parent);
            go.name = name;
            float y = _menu.YStart - (_baseItemCount + _row) * _menu.YOffset;
            go.transform.localPosition = new Vector3(_template.transform.localPosition.x, y, _template.transform.localPosition.z);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);

            var no = go.GetComponent<NumberOption>();
            var titleText = no != null ? no.TitleText : null;
            var valueText = no != null ? no.ValueText : null;
            if (no != null) UnityEngine.Object.Destroy(no);

            Track(go.transform);
            return (go.transform, valueText);
        }

        private void Track(Transform item)
        {
            if (item == null || _menu.AllItems == null) return;
            for (int i = 0; i < _menu.AllItems.Count; i++)
                if (_menu.AllItems.get_Item(i) == item) return;
            _menu.AllItems.Add(item);
        }
    }

    // ──────────────────────────────────────────────
    // Registry — what mods call in their Load() method
    // ──────────────────────────────────────────────

    public static class SettingsMenuAPI
    {
        private sealed class Registration
        {
            public int RowCount;
            public Action<SettingsMenuBuilder> Build;
        }

        private static readonly List<Registration> _registrations = new();

        /// <summary>Registers a block of settings rows to be built when the menu opens.</summary>
        public static void Register(int rowCount, Action<SettingsMenuBuilder> build)
        {
            _registrations.Add(new Registration { RowCount = rowCount, Build = build });
        }

        internal static void BuildAll(GameSettingMenu menu)
        {
            if (menu == null || menu.AllItems == null || menu.AllItems.Count == 0) return;
            var parent = menu.AllItems.get_Item(0).parent;
            if (parent == null) return;
            var template = menu.keyvaluePrefab?.TryCast<NumberOption>();
            if (template == null) return;

            for (int i = 0; i < _registrations.Count; i++)
            {
                var reg = _registrations[i];
                int start = ReactorAPI.ReserveSettingsRows(menu.GetInstanceID(), reg.RowCount);
                var builder = new SettingsMenuBuilder(menu, parent, template, start);
                try { reg.Build(builder); }
                catch (Exception e) { ManuAPIPlugin.Log.LogError("SettingsMenuAPI build failed: " + e); }
            }

            try { menu.RepositionChildren(); }
            catch (Exception e) { ManuAPIPlugin.Log.LogError("SettingsMenuAPI RepositionChildren: " + e); }
        }
    }

    // ──────────────────────────────────────────────
    // Host → Client settings sync over Reactor RPC
    // ──────────────────────────────────────────────

    /// <summary>
    /// Host-authoritative settings synchronisation. The host builds the settings
    /// payload once when the lobby menu opens, and every client applies it.
    /// Use <see cref="Register"/> with the same key on every client.
    ///
    /// Usage:
    /// <code>
    ///   // Host side (in SettingsMenuBuilder callback):
    ///   SettingsSync.PushFloat("myplugin.Cooldown", cooldown.GetValue());
    ///
    ///   // Client side (in plugin Load):
    ///   SettingsSync.RegisterFloat("myplugin.Cooldown", val => myConfig.Value = val);
    /// </code>
    /// </summary>
    public static class SettingsSync
    {
        private const string RpcKey = "classicus.manuapi.SettingsSync";
        private static readonly Dictionary<string, Action<float>> _floatHandlers = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Action<bool>> _boolHandlers = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Action<int>> _intHandlers = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Action<string>> _stringHandlers = new(StringComparer.Ordinal);
        private static System.IO.MemoryStream _pending;
        private static System.IO.BinaryWriter _writer;
        private static bool _registered;

        /// <summary>Registers a float setting handler for clients to receive.</summary>
        public static void RegisterFloat(string key, Action<float> apply) => _floatHandlers[key] = apply;
        /// <summary>Registers a bool setting handler for clients to receive.</summary>
        public static void RegisterBool(string key, Action<bool> apply) => _boolHandlers[key] = apply;
        /// <summary>Registers an int setting handler for clients to receive.</summary>
        public static void RegisterInt(string key, Action<int> apply) => _intHandlers[key] = apply;
        /// <summary>Registers a string setting handler for clients to receive.</summary>
        public static void RegisterString(string key, Action<string> apply) => _stringHandlers[key] = apply;

        /// <summary>Host: begins building the sync payload. Call before Push*.</summary>
        public static void BeginBuild()
        {
            _pending = new System.IO.MemoryStream();
            _writer = new System.IO.BinaryWriter(_pending, System.Text.Encoding.UTF8, true);
            _writer.Write((byte)1); // protocol version
        }

        /// <summary>Host: pushes a float value into the sync payload.</summary>
        public static void PushFloat(string key, float value)
        {
            if (_writer == null) return;
            _writer.Write((byte)0); // type tag
            _writer.Write(key);
            _writer.Write(value);
        }

        /// <summary>Host: pushes a bool value into the sync payload.</summary>
        public static void PushBool(string key, bool value)
        {
            if (_writer == null) return;
            _writer.Write((byte)1);
            _writer.Write(key);
            _writer.Write(value);
        }

        /// <summary>Host: pushes an int value into the sync payload.</summary>
        public static void PushInt(string key, int value)
        {
            if (_writer == null) return;
            _writer.Write((byte)2);
            _writer.Write(key);
            _writer.Write(value);
        }

        /// <summary>Host: pushes a string value into the sync payload.</summary>
        public static void PushString(string key, string value)
        {
            if (_writer == null) return;
            _writer.Write((byte)3);
            _writer.Write(key);
            _writer.Write(value ?? "");
        }

        /// <summary>Host: finalises and broadcasts the sync payload to all clients.</summary>
        public static void EndBuild()
        {
            if (_writer == null || _pending == null) return;
            _writer.Flush();
            var payload = Convert.ToBase64String(_pending.ToArray());
            _writer.Dispose();
            _pending.Dispose();
            _writer = null;
            _pending = null;

            EnsureRegistered();
            ReactorAPI.SendRpcMethod(RpcKey, payload);
        }

        /// <summary>
        /// Host: registers and broadcasts in one call. Call inside your
        /// SettingsMenuBuilder callback after all settings rows have been built.
        /// </summary>
        public static void Broadcast()
        {
            EnsureRegistered();
            BeginBuild();
        }

        private static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;
            ReactorAPI.RegisterRpcMethods(typeof(SettingsSync));
        }

        [ReactorRpc(RpcKey)]
        private static void OnSettingsSyncRpc(byte senderId, string payload)
        {
            var client = AmongUsClient.Instance;
            if (client != null && client.AmHost) return; // host doesn't apply its own sync

            var bytes = string.IsNullOrEmpty(payload) ? Array.Empty<byte>() : Convert.FromBase64String(payload);
            using var stream = new System.IO.MemoryStream(bytes, false);
            using var reader = new System.IO.BinaryReader(stream);

            if (stream.Length < 1) return;
            byte version = reader.ReadByte();
            if (version != 1) return;

            while (stream.Position < stream.Length)
            {
                byte type = reader.ReadByte();
                string key = reader.ReadString();

                switch (type)
                {
                    case 0: // float
                        if (_floatHandlers.TryGetValue(key, out var fh))
                        {
                            float val = reader.ReadSingle();
                            try { fh(val); }
                            catch (Exception e) { ManuAPIPlugin.Log.LogError("SettingsSync float " + key + ": " + e); }
                        }
                        else reader.ReadSingle(); // skip
                        break;
                    case 1: // bool
                        if (_boolHandlers.TryGetValue(key, out var bh))
                        {
                            bool val = reader.ReadBoolean();
                            try { bh(val); }
                            catch (Exception e) { ManuAPIPlugin.Log.LogError("SettingsSync bool " + key + ": " + e); }
                        }
                        else reader.ReadBoolean();
                        break;
                    case 2: // int
                        if (_intHandlers.TryGetValue(key, out var ih))
                        {
                            int val = reader.ReadInt32();
                            try { ih(val); }
                            catch (Exception e) { ManuAPIPlugin.Log.LogError("SettingsSync int " + key + ": " + e); }
                        }
                        else reader.ReadInt32();
                        break;
                    case 3: // string
                        if (_stringHandlers.TryGetValue(key, out var sh))
                        {
                            string val = reader.ReadString();
                            try { sh(val); }
                            catch (Exception e) { ManuAPIPlugin.Log.LogError("SettingsSync string " + key + ": " + e); }
                        }
                        else reader.ReadString();
                        break;
                }
            }

            ManuAPIPlugin.Log.LogInfo("SettingsSync: applied host settings for " +
                (_floatHandlers.Count + _boolHandlers.Count + _intHandlers.Count + _stringHandlers.Count) + " keys.");
        }
    }

    // ──────────────────────────────────────────────
    // Harmony patch — injects settings on menu open
    // ──────────────────────────────────────────────

    [HarmonyPatch(typeof(SettingMenu), nameof(SettingMenu.OnEnable))]
    internal static class SettingMenu_OnEnable_Patch
    {
        private static void Postfix(SettingMenu __instance)
        {
            var gameMenu = __instance.TryCast<GameSettingMenu>();
            if (gameMenu == null) return;
            try { SettingsMenuAPI.BuildAll(gameMenu); }
            catch (Exception e) { ManuAPIPlugin.Log.LogError("SettingsMenuAPI.BuildAll: " + e); }
        }
    }
}