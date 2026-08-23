using System;
using System.Reflection;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Provides compile-safe access to Il2CppObjectBase.GetIl2CppType() which
    /// exists at runtime but may be absent from decompiled interop stub DLLs.
    /// Uses reflection to avoid compile-time binding to Il2CppInterop APIs.
    /// </summary>
    internal static class Il2CppTypeHelper
    {
        private static readonly MethodInfo _getIl2CppTypeMethod;
        private static readonly PropertyInfo _il2CppTypeNameProp;

        static Il2CppTypeHelper()
        {
            // Il2CppObjectBase is the base class that provides GetIl2CppType()
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var baseType = asm.GetType("Il2CppInterop.Runtime.Il2CppObjectBase");
                if (baseType != null)
                {
                    _getIl2CppTypeMethod = baseType.GetMethod("GetIl2CppType",
                        BindingFlags.Public | BindingFlags.Instance);
                    break;
                }
            }

            // Il2CppSystem.Type.Name property
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var il2cppType = asm.GetType("Il2CppSystem.Type");
                if (il2cppType != null)
                {
                    _il2CppTypeNameProp = il2cppType.GetProperty("Name");
                    break;
                }
            }
        }

        /// <summary>
        /// Returns the IL2CPP type name for a role (e.g., "ImpostorRole", "CrewmateRole").
        /// Falls back to System.Type.Name if Il2CppInterop is unavailable.
        /// </summary>
        public static string GetIl2CppTypeName(object obj)
        {
            if (obj == null) return "<null>";

            if (_getIl2CppTypeMethod != null)
            {
                try
                {
                    var il2cppType = _getIl2CppTypeMethod.Invoke(obj, null);
                    if (il2cppType != null && _il2CppTypeNameProp != null)
                        return _il2CppTypeNameProp.GetValue(il2cppType) as string ?? il2cppType.ToString();
                }
                catch { /* fall through to fallback */ }
            }

            // Fallback: use .NET reflection type name
            return obj.GetType().Name;
        }

        /// <summary>
        /// Returns the IL2CPP System.Type for a role. Falls back to System.Type if unavailable.
        /// </summary>
        public static object GetIl2CppType(object obj)
        {
            if (obj == null) return null;

            if (_getIl2CppTypeMethod != null)
            {
                try
                {
                    return _getIl2CppTypeMethod.Invoke(obj, null);
                }
                catch { /* fall through */ }
            }

            return obj.GetType();
        }

        /// <summary>
        /// Checks whether the IL2CPP type of <paramref name="obj"/> has the given name.
        /// </summary>
        public static bool HasIl2CppTypeName(object obj, string name)
        {
            return string.Equals(GetIl2CppTypeName(obj), name, StringComparison.Ordinal);
        }
    }
}