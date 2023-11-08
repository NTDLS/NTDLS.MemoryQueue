using Newtonsoft.Json;
using NTDLS.Semaphore;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NTDLS.MemoryQueue
{
    internal static class Utility
    {
        private static readonly CriticalResource<Dictionary<string, MethodInfo>> _reflectioncache = new();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            return (new StackTrace())?.GetFrame(1)?.GetMethod()?.Name ?? "{unknown frame}";
        }

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

        public static T? JsonDeserializeToObject<T>(string json)
            => JsonConvert.DeserializeObject<T>(json);

        internal static T ExtractGenericType<T>(string payload, string typeName)
        {
            var genericToObjectMethod = _reflectioncache.Use((o) =>
            {
                if (o.TryGetValue(typeName, out var method))
                {
                    return method;
                }
                return null;
            });

            if (genericToObjectMethod != null)
            {
                return (T?)genericToObjectMethod.Invoke(null, new object[] { payload })
                    ?? throw new Exception($"ExtractGenericType: Payload can not be null.");
            }

            var genericType = Type.GetType(typeName)
                ?? throw new Exception($"ExtractGenericType: Unknown payload type {typeName}.");

            var toObjectMethod = typeof(Utility).GetMethod("JsonDeserializeToObject")
                ?? throw new Exception($"ExtractGenericType: Could not find JsonDeserializeToObject().");

            genericToObjectMethod = toObjectMethod.MakeGenericMethod(genericType);

            _reflectioncache.Use((o) => o.TryAdd(typeName, genericToObjectMethod));

            return (T?)genericToObjectMethod.Invoke(null, new object[] { payload })
                ?? throw new Exception($"ExtractGenericType: Payload can not be null.");
        }
    }
}
