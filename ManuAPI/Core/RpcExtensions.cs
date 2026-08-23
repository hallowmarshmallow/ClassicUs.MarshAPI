using System;
using System.IO;
using ClassicUs.Reactor;
using UnityEngine;

namespace ClassicUs.ManuAPI
{
    /// <summary>
    /// Extended RPC serialization for argument types beyond Reactor's native set
    /// (bool/byte/int/float/string). Packs all arguments into a base64 string,
    /// which is sent as a single <c>[ReactorRpc]</c> string argument.
    ///
    /// Sending:
    /// <code>
    ///   RpcExtensions.SendPacked("myplugin.MyRpc", killerId, targetId, position);
    /// </code>
    ///
    /// Receiving (declare in your system class):
    /// <code>
    ///   [ReactorRpc("myplugin.MyRpc")]
    ///   static void OnMyRpc(byte senderId, string payload)
    ///   {
    ///       var bytes = Convert.FromBase64String(payload);
    ///       using var reader = new BinaryReader(new MemoryStream(bytes));
    ///       byte killerId = reader.ReadByte();
    ///       byte targetId = reader.ReadByte();
    ///       var pos = new Vector2(reader.ReadSingle(), reader.ReadSingle());
    ///       // ... handle ...
    ///   }
    /// </code>
    ///
    /// Or use the convenience deserializer:
    /// <code>
    ///   [ReactorRpc("myplugin.MyRpc")]
    ///   static void OnMyRpc(byte senderId, string payload)
    ///   {
    ///       var bytes = Convert.FromBase64String(payload);
    ///       var args = RpcExtensions.DeserializeArgs(bytes, typeof(byte), typeof(byte), typeof(Vector2));
    ///       byte killerId = (byte)args[0];
    ///       byte targetId = (byte)args[1];
    ///       var pos = (Vector2)args[2];
    ///   }
    /// </code>
    /// </summary>
    public static class RpcExtensions
    {
        /// <summary>
        /// Serializes all arguments to a base64 string and sends them through
        /// Reactor's string-keyed RPC. Supported types: bool, byte, sbyte, short,
        /// ushort, int, uint, long, ulong, float, double, string, byte[],
        /// Vector2, Vector3.
        /// </summary>
        public static void SendPacked(string key, params object[] args)
        {
            try
            {
                var payload = Convert.ToBase64String(SerializeArgs(args));
                ReactorAPI.SendRpcMethod(key, payload);
            }
            catch (Exception e)
            {
                ManuAPIPlugin.Log.LogError("SendPacked (" + key + "): " + e);
            }
        }

        /// <summary>
        /// Serializes arguments into a byte array for transport.
        /// </summary>
        public static byte[] SerializeArgs(object[] args)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);
            foreach (var value in args ?? Array.Empty<object>())
                SerializeValue(writer, value);
            return stream.ToArray();
        }

        /// <summary>
        /// Deserializes a base64 payload into an array of objects.
        /// Each type in <paramref name="types"/> specifies the expected deserialization format.
        /// </summary>
        public static object[] DeserializeArgs(string base64Payload, params Type[] types)
        {
            var bytes = string.IsNullOrEmpty(base64Payload) ? Array.Empty<byte>() : Convert.FromBase64String(base64Payload);
            return DeserializeArgs(bytes, types);
        }

        /// <summary>
        /// Deserializes a byte array payload into an array of objects.
        /// </summary>
        public static object[] DeserializeArgs(byte[] bytes, params Type[] types)
        {
            using var stream = new MemoryStream(bytes ?? Array.Empty<byte>(), false);
            using var reader = new BinaryReader(stream);
            return DeserializeArgs(reader, types);
        }

        /// <summary>
        /// Reads values from a BinaryReader matching the given types.
        /// </summary>
        public static object[] DeserializeArgs(BinaryReader reader, Type[] types)
        {
            var result = new object[types.Length];
            for (int i = 0; i < types.Length; i++)
                result[i] = DeserializeValue(reader, types[i]);
            return result;
        }

        private static void SerializeValue(BinaryWriter writer, object value)
        {
            switch (value)
            {
                case bool v: writer.Write(v); break;
                case byte v: writer.Write(v); break;
                case sbyte v: writer.Write(v); break;
                case short v: writer.Write(v); break;
                case ushort v: writer.Write(v); break;
                case int v: writer.Write(v); break;
                case uint v: writer.Write(v); break;
                case long v: writer.Write(v); break;
                case ulong v: writer.Write(v); break;
                case float v: writer.Write(v); break;
                case double v: writer.Write(v); break;
                case string v: writer.Write(v ?? string.Empty); break;
                case byte[] v:
                    writer.Write(v?.Length ?? -1);
                    if (v != null) writer.Write(v);
                    break;
                case Vector2 v: writer.Write(v.x); writer.Write(v.y); break;
                case Vector3 v: writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); break;
                default:
                    throw new NotSupportedException("RpcExtensions: unsupported argument type " + value?.GetType());
            }
        }

        private static object DeserializeValue(BinaryReader reader, Type type)
        {
            if (type == typeof(bool)) return reader.ReadBoolean();
            if (type == typeof(byte)) return reader.ReadByte();
            if (type == typeof(sbyte)) return reader.ReadSByte();
            if (type == typeof(short)) return reader.ReadInt16();
            if (type == typeof(ushort)) return reader.ReadUInt16();
            if (type == typeof(int)) return reader.ReadInt32();
            if (type == typeof(uint)) return reader.ReadUInt32();
            if (type == typeof(long)) return reader.ReadInt64();
            if (type == typeof(ulong)) return reader.ReadUInt64();
            if (type == typeof(float)) return reader.ReadSingle();
            if (type == typeof(double)) return reader.ReadDouble();
            if (type == typeof(string)) return reader.ReadString();
            if (type == typeof(byte[])) { var count = reader.ReadInt32(); return count < 0 ? null : reader.ReadBytes(count); }
            if (type == typeof(Vector2)) return new Vector2(reader.ReadSingle(), reader.ReadSingle());
            if (type == typeof(Vector3)) return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            throw new NotSupportedException("RpcExtensions: unsupported parameter type " + type);
        }
    }
}