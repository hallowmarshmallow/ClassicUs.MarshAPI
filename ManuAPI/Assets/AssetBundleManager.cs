using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    public static class AssetBundleManager
    {
        private static readonly Dictionary<string, AssetBundle> _bundles = new();

        public static AssetBundle LoadFromEmbeddedResource(Assembly assembly, string resourceName, string bundleKey = null)
        {
            bundleKey ??= resourceName;
            if (_bundles.TryGetValue(bundleKey, out var existing) && existing != null) return existing;

            try
            {
                var bytes = AssetUtils.ReadEmbeddedResource(assembly, resourceName);
                if (bytes == null) return null;

                var array = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) array[i] = bytes[i];

                var bundle = AssetBundle.LoadFromMemory(array);
                if (bundle == null)
                {
                    ManuAPIPlugin.Log.LogError("AssetBundleManager: LoadFromMemory returned null for " + resourceName);
                    return null;
                }

                _bundles[bundleKey] = bundle;
                return bundle;
            }
            catch (Exception e)
            {
                ManuAPIPlugin.Log.LogError("AssetBundleManager.LoadFromEmbeddedResource (" + resourceName + "): " + e);
                return null;
            }
        }

        public static AssetBundle LoadFromFile(string filePath, string bundleKey = null)
        {
            bundleKey ??= filePath;
            if (_bundles.TryGetValue(bundleKey, out var existing) && existing != null) return existing;

            try
            {
                var bundle = AssetBundle.LoadFromFileAsync(filePath).assetBundle;
                if (bundle == null)
                {
                    ManuAPIPlugin.Log.LogError("AssetBundleManager: LoadFromFile returned null for " + filePath);
                    return null;
                }

                _bundles[bundleKey] = bundle;
                return bundle;
            }
            catch (Exception e)
            {
                ManuAPIPlugin.Log.LogError("AssetBundleManager.LoadFromFile (" + filePath + "): " + e);
                return null;
            }
        }

        public static T LoadAsset<T>(string bundleKey, string assetName) where T : UnityEngine.Object
        {
            if (!_bundles.TryGetValue(bundleKey, out var bundle) || bundle == null)
            {
                ManuAPIPlugin.Log.LogError("AssetBundleManager.LoadAsset: bundle '" + bundleKey + "' not loaded.");
                return null;
            }
            try
            {
                // AssetBundle APIs vary; use LoadAssetAsync synchronously
                var req = bundle.LoadAssetAsync(assetName, typeof(T));
                if (req == null) return null;
                while (!req.isDone) { /* busy-wait */ }
                return req.asset as T;
            }
            catch (Exception e)
            {
                ManuAPIPlugin.Log.LogError("AssetBundleManager.LoadAsset (" + bundleKey + "/" + assetName + "): " + e);
                return null;
            }
        }

        public static void Unload(string bundleKey, bool unloadAllLoadedObjects = false)
        {
            if (!_bundles.TryGetValue(bundleKey, out var bundle) || bundle == null) return;
            bundle.Unload(unloadAllLoadedObjects);
            _bundles.Remove(bundleKey);
        }
    }
}
