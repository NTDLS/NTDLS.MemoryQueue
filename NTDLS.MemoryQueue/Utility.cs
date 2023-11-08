using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NTDLS.MemoryQueue
{
    internal static class Utility
    {
        private static readonly MemoryCache _reflectioncache = new(new MemoryCacheOptions());

        public delegate void TryAndIgnoreProc();
        public delegate T TryAndIgnoreProc<T>();

        /// <summary>
        /// We didnt need that exception! Did we?... DID WE?!
        /// </summary>
        public static void TryAndIgnore(TryAndIgnoreProc func)
        {
            try { func(); } catch { }
        }

        /// <summary>
        /// We didnt need that exception! Did we?... DID WE?!
        /// </summary>
        public static T? TryAndIgnore<T>(TryAndIgnoreProc<T> func)
        {
            try { return func(); } catch { }
            return default;
        }

        public static void EnsureNotNull<T>([NotNull] T? value, string? message = null, [CallerArgumentExpression(nameof(value))] string strName = "")
        {
            if (value == null)
            {
                if (message == null)
                {
                    throw new Exception($"Value should not be null: '{strName}'.");
                }
                else
                {
                    throw new Exception(message);
                }
            }
        }

        /// <summary>
        /// This is used by ExtractGenericType() and is accessed via reflection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T? JsonDeserializeToObject<T>(string json)
            => JsonConvert.DeserializeObject<T>(json);

        internal static T ExtractGenericType<T>(string payload, string typeName)
        {
            var genericDeserializationMethod = (MethodInfo?)_reflectioncache.Get(typeName);
            if (genericDeserializationMethod != null)
            {
                return (T?)genericDeserializationMethod.Invoke(null, new object[] { payload })
                    ?? throw new Exception($"ExtractGenericType: Payload can not be null.");
            }

            var genericType = Type.GetType(typeName)
                ?? throw new Exception($"ExtractGenericType: Unknown payload type {typeName}.");

            var jsonDeserializeToObject = typeof(Utility).GetMethod("JsonDeserializeToObject")
                ?? throw new Exception($"ExtractGenericType: Could not find JsonDeserializeToObject().");

            genericDeserializationMethod = jsonDeserializeToObject.MakeGenericMethod(genericType);
            _reflectioncache.Set(typeName, genericDeserializationMethod, TimeSpan.FromSeconds(600));

            return (T?)genericDeserializationMethod.Invoke(null, new object[] { payload })
                ?? throw new Exception($"ExtractGenericType: Payload can not be null.");
        }
    }
}
