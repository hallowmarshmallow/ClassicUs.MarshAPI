using System.Collections.Generic;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Sound playback API — extends the existing <see cref="SpatialAudio"/> with
    /// 2D (non-spatial) sounds, looping playback, volume control, and global
    /// sound management that works on every client (not just the host).
    /// </summary>
    public static class SoundAPI
    {
        private static readonly List<ManagedSound> _activeSounds = new();

        /// <summary>
        /// Plays a 2D sound (non-spatial, heard by everyone equally regardless of
        /// distance). Uses the game's <see cref="SoundManager"/> for native audio
        /// clips, or creates a temporary <see cref="AudioSource"/> for custom clips.
        /// </summary>
        public static void PlayGlobal(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound(clip, false, volume);
                return;
            }

            // Fallback: play through a temporary GameObject.
            var go = new GameObject("ManuAPI_GlobalSound");
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.Play();
            Object.Destroy(go, clip.length + 0.1f);
        }

        /// <summary>
        /// Plays a sound that only the local player hears (2D, bypasses
        /// SoundManager positional logic).
        /// </summary>
        public static void PlayLocal(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            var go = new GameObject("ManuAPI_LocalSound");
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.Play();
            Object.Destroy(go, clip.length + 0.1f);
        }

        /// <summary>
        /// Plays a looping 2D sound and returns a handle that can be stopped later.
        /// The sound persists until <see cref="StopLooping"/> is called.
        /// </summary>
        public static ManagedSound PlayLooping(AudioClip clip, float volume = 1f, bool spatial = false)
        {
            if (clip == null) return null;
            var go = new GameObject("ManuAPI_LoopSound_" + clip.name);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = spatial ? 1f : 0f;
            source.volume = volume;
            source.Play();

            var managed = new ManagedSound(go, source);
            _activeSounds.Add(managed);
            return managed;
        }

        /// <summary>
        /// Stops and cleans up a looping sound previously started with <see cref="PlayLooping"/>.
        /// </summary>
        public static void StopLooping(ManagedSound sound)
        {
            if (sound == null) return;
            sound.Stop();
            _activeSounds.Remove(sound);
        }

        /// <summary>
        /// Stops all active managed sounds.
        /// </summary>
        public static void StopAll()
        {
            foreach (var s in _activeSounds) s.Stop();
            _activeSounds.Clear();
        }

        /// <summary>
        /// Plays the game's native kill sound (KillSfx from the local player).
        /// Useful for custom kills that want the vanilla audio cue.
        /// </summary>
        public static void PlayKillSound()
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null || local.KillSfx == null) return;
            SoundManager.Instance?.PlaySound(local.KillSfx, false, 0.8f);
        }

        /// <summary>
        /// Plays any sound from SoundManager's internal audio clips by name substring.
        /// Returns true if a matching clip was found and played.
        /// </summary>
        public static bool PlaySoundByName(string partialName, float volume = 1f)
        {
            if (SoundManager.Instance == null || string.IsNullOrEmpty(partialName)) return false;

            // Try common sound fields via reflection on the game's SoundManager.
            var type = SoundManager.Instance.GetIl2CppType();
            if (type == null) return false;

            var query = partialName.ToLowerInvariant();
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!field.FieldType.IsAssignableFrom(typeof(AudioClip))) continue;
                if (field.Name.ToLowerInvariant().Contains(query))
                {
                    var clip = field.GetValue(SoundManager.Instance) as AudioClip;
                    if (clip != null)
                    {
                        SoundManager.Instance.PlaySound(clip, false, volume);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Handle for a looping sound created by <see cref="SoundAPI.PlayLooping"/>.
    /// </summary>
    public sealed class ManagedSound
    {
        private readonly GameObject _go;
        private readonly AudioSource _source;
        private bool _stopped;

        internal ManagedSound(GameObject go, AudioSource source)
        {
            _go = go;
            _source = source;
        }

        /// <summary>The AudioSource. Tweak volume, pitch, etc. at runtime.</summary>
        public AudioSource Source => _source;

        /// <summary>Current volume. Setting this updates the AudioSource immediately.</summary>
        public float Volume
        {
            get => _source != null ? _source.volume : 0f;
            set { if (_source != null) _source.volume = value; }
        }

        /// <summary>True after Stop() has been called.</summary>
        public bool IsStopped => _stopped;

        internal void Stop()
        {
            if (_stopped) return;
            _stopped = true;
            if (_source != null) _source.Stop();
            if (_go != null) Object.Destroy(_go);
        }
    }
}