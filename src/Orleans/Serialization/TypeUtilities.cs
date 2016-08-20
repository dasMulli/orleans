using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Serialization
{
    using System.Runtime.CompilerServices;

    internal static class TypeUtilities
    {
        static readonly ConcurrentDictionary<Type, bool> shallowCopyableTypes = new ConcurrentDictionary<Type, bool>();
        static readonly ConcurrentDictionary<Type, string> typeNameCache = new ConcurrentDictionary<Type, string>();
        static readonly ConcurrentDictionary<Type, string> typeKeyStringCache = new ConcurrentDictionary<Type, string>();
        static readonly ConcurrentDictionary<Type, byte[]> typeKeyCache = new ConcurrentDictionary<Type, byte[]>();

        static TypeUtilities()
        {
            shallowCopyableTypes[typeof(Decimal)] = true;
            shallowCopyableTypes[typeof(DateTime)] = true;
            shallowCopyableTypes[typeof(TimeSpan)] = true;
            shallowCopyableTypes[typeof(IPAddress)] = true;
            shallowCopyableTypes[typeof(IPEndPoint)] = true;
            shallowCopyableTypes[typeof(SiloAddress)] = true;
            shallowCopyableTypes[typeof(GrainId)] = true;
            shallowCopyableTypes[typeof(ActivationId)] = true;
            shallowCopyableTypes[typeof(ActivationAddress)] = true;
            shallowCopyableTypes[typeof(CorrelationId)] = true;
            shallowCopyableTypes[typeof(string)] = true;
            shallowCopyableTypes[typeof(Immutable<>)] = true;
            shallowCopyableTypes[typeof(CancellationToken)] = true;
        }

        internal static bool IsOrleansShallowCopyable(this Type t)
        {
            bool result;
            if (shallowCopyableTypes.TryGetValue(t, out result))
            {
                return result;
            }

            var typeInfo = t.GetTypeInfo();
            if (typeInfo.IsPrimitive || typeInfo.IsEnum)
            {
                shallowCopyableTypes[t] = true;
                return true;
            }

            if (typeInfo.GetCustomAttributes(typeof(ImmutableAttribute), false).Any())
            {
                shallowCopyableTypes[t] = true;
                return true;
            }

            if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Immutable<>))
            {
                shallowCopyableTypes[t] = true;
                return true;
            }

            if (typeInfo.IsValueType && !typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition)
            {
                result = IsTypeFieldsShallowCopyable(typeInfo);
                shallowCopyableTypes[t] = result;
                return result;
            }

            shallowCopyableTypes[t] = false;
            return false;
        }

        private static bool IsTypeFieldsShallowCopyable(TypeInfo typeInfo)
        {
            return typeInfo.GetFields().All(f => !(f.FieldType.IsEquivalentTo(typeInfo)) && IsOrleansShallowCopyable(f.FieldType));
        }

        internal static bool IsSpecializationOf(this Type t, Type match)
        {
            var typeInfo = t.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == match;
        }

        internal static string OrleansTypeName(this Type t)
        {
            string name;
            if (typeNameCache.TryGetValue(t, out name))
                return name;

            name = TypeUtils.GetTemplatedName(t, _ => !_.IsGenericParameter);
            typeNameCache[t] = name;
            return name;
        }

        public static byte[] OrleansTypeKey(this Type t)
        {
            byte[] key;
            if (typeKeyCache.TryGetValue(t, out key))
                return key;

            key = Encoding.UTF8.GetBytes(t.OrleansTypeKeyString());
            typeKeyCache[t] = key;
            return key;
        }

        public static string OrleansTypeKeyString(this Type t)
        {
            string key;
            if (typeKeyStringCache.TryGetValue(t, out key))
                return key;

            var typeInfo = t.GetTypeInfo();
            var sb = new StringBuilder();
            if (typeInfo.IsGenericTypeDefinition)
            {
                sb.Append(GetBaseTypeKey(t));
                sb.Append('\'');
                sb.Append(typeInfo.GetGenericArguments().Length);
            }
            else if (typeInfo.IsGenericType)
            {
                sb.Append(GetBaseTypeKey(t));
                sb.Append('<');
                var first = true;
                foreach (var genericArgument in t.GetGenericArguments())
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }
                    first = false;
                    sb.Append(OrleansTypeKeyString(genericArgument));
                }
                sb.Append('>');
            }
            else if (t.IsArray)
            {
                sb.Append(OrleansTypeKeyString(t.GetElementType()));
                sb.Append('[');
                if (t.GetArrayRank() > 1)
                {
                    sb.Append(',', t.GetArrayRank() - 1);
                }
                sb.Append(']');
            }
            else
            {
                sb.Append(GetBaseTypeKey(t));
            }

            key = sb.ToString();
            typeKeyStringCache[t] = key;

            return key;
        }

        private static string GetBaseTypeKey(Type t)
        {
            var typeInfo = t.GetTypeInfo();

            string namespacePrefix = "";
            if ((typeInfo.Namespace != null) && !typeInfo.Namespace.StartsWith("System.") && !typeInfo.Namespace.Equals("System"))
            {
                namespacePrefix = typeInfo.Namespace + '.';
            }

            if (typeInfo.IsNestedPublic)
            {
                return namespacePrefix + OrleansTypeKeyString(typeInfo.DeclaringType) + "." + typeInfo.Name;
            }

            return namespacePrefix + typeInfo.Name;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static string GetLocationSafe(this Assembly a)
        {
            if (a.IsDynamic)
            {
                return "dynamic";
            }

            try
            {
                return a.Location;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }
    }
}
