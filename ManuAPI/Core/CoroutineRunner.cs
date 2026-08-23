using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Safe coroutine runner for Classic Us mods. Harmony patches on IEnumerator
    /// (coroutine) entry points can segfault during detour installation on Linux
    /// IL2CPP. This runner lets you start Unity coroutines from within safe
    /// MonoBehaviour callbacks (Start, FixedUpdate, etc.) without patching any
    /// coroutine methods.
    ///
    /// Usage:
    /// <code>
    ///   CoroutineRunner.Start(MyCoroutine());  // fire-and-forget
    ///   var handle = CoroutineRunner.Start(MyCoroutine(5f));  // stoppable
    ///   handle.Stop();
    /// </code>
    ///
    /// All coroutines are automatically stopped on game end/restart via the
    /// GameEvents.GameEnded hook, so you don't need to manually clean up.
    /// </summary>
    public static class CoroutineRunner
    {
        private static RunnerBehaviour _runner;
        private static readonly HashSet<CoroutineHandle> _active = new();

        /// <summary>
        /// Starts a coroutine. Returns a handle that can be used to stop it early.
        /// All active coroutines are automatically stopped when GameEvents.GameEnded fires.
        /// </summary>
        public static CoroutineHandle Start(IEnumerator routine)
        {
            if (routine == null) return CoroutineHandle.Invalid;
            EnsureRunner();

            var handle = new CoroutineHandle(routine);
            _active.Add(handle);
            handle.Coroutine = _runner.StartCoroutine(Wrap(handle));
            return handle;
        }

        /// <summary>
        /// Stops a specific coroutine by its handle.
        /// </summary>
        public static void Stop(CoroutineHandle handle)
        {
            if (handle == null || !_active.Remove(handle)) return;
            handle.MarkStopped();
            if (_runner != null && handle.Coroutine != null)
                _runner.StopCoroutine(handle.Coroutine);
        }

        /// <summary>
        /// Stops all running coroutines immediately.
        /// </summary>
        public static void StopAll()
        {
            foreach (var handle in _active)
            {
                handle.MarkStopped();
                if (_runner != null && handle.Coroutine != null)
                    _runner.StopCoroutine(handle.Coroutine);
            }
            _active.Clear();
        }

        /// <summary>
        /// Returns the number of currently active coroutines.
        /// </summary>
        public static int ActiveCount => _active.Count;

        private static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("ManuAPI_CoroutineRunner");
            _runner = go.AddComponent<RunnerBehaviour>();
            UnityEngine.Object.DontDestroyOnLoad(go);

            // Auto-cleanup on game end.
            GameEvents.GameEnded += _ => StopAll();
        }

        private static IEnumerator Wrap(CoroutineHandle handle)
        {
            yield return handle.Routine;
            _active.Remove(handle);
            handle.MarkStopped();
        }

        private sealed class RunnerBehaviour : MonoBehaviour
        {
            private void OnDestroy() { CoroutineRunner._runner = null; }
        }
    }

    /// <summary>
    /// Handle returned by <see cref="CoroutineRunner.Start"/>. Can be used to
    /// stop the coroutine early or check its status.
    /// </summary>
    public sealed class CoroutineHandle
    {
        internal static readonly CoroutineHandle Invalid = new(null);

        internal Coroutine Routine { get; }
        internal UnityEngine.Coroutine Coroutine { get; set; }

        /// <summary>True after Stop() was called or the coroutine completed naturally.</summary>
        public bool IsStopped { get; private set; }

        internal CoroutineHandle(IEnumerator routine) { Routine = routine; }
        internal void MarkStopped() => IsStopped = true;

        /// <summary>Stops this coroutine. Safe to call multiple times.</summary>
        public void Stop() => CoroutineRunner.Stop(this);
    }
}