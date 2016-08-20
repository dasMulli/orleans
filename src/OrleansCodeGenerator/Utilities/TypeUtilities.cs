using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Orleans.CodeGenerator.Utilities
{
    internal static class TypeUtilities
    {
        internal static bool IsOrleansPrimitive(this TypeInfo typeInfo)
        {
            var t = typeInfo.AsType();
            return typeInfo.IsPrimitive || typeInfo.IsEnum || t == typeof(string) || t == typeof(DateTime) || t == typeof(Decimal) || (typeInfo.IsArray && typeInfo.GetElementType().GetTypeInfo().IsOrleansPrimitive());
        }

        public static bool IsTypeIsInaccessibleForSerialization(Type type, Module fromModule, string serializationAssemblyName)
        {
            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsGenericTypeDefinition)
            {
                // Guard against invalid type constraints, which appear when generating code for some languages.
                foreach (var parameter in typeInfo.GenericTypeParameters)
                {
                    if (parameter.GetTypeInfo().GetGenericParameterConstraints().Any(IsSpecialClass))
                    {
                        return true;
                    }
                }
            }

            if (!typeInfo.IsVisible && type.IsConstructedGenericType)
            {
                foreach (var inner in typeInfo.GetGenericArguments())
                {
                    if (IsTypeIsInaccessibleForSerialization(inner, fromModule, serializationAssemblyName))
                    {
                        return true;
                    }
                }

                if (IsTypeIsInaccessibleForSerialization(typeInfo.GetGenericTypeDefinition(), fromModule, serializationAssemblyName))
                {
                    return true;
                }
            }

            if ((typeInfo.IsNotPublic || !typeInfo.IsVisible) && !AreInternalsVisibleTo(typeInfo, serializationAssemblyName))
            {
                // subtype is defined in a different assembly from the outer type
                if (!typeInfo.Module.Equals(fromModule))
                {
                    return true;
                }

                // subtype defined in a different assembly from the one we are generating serializers for.
                if (!typeInfo.Assembly.FullName.Equals(serializationAssemblyName))
                {
                    return true;
                }
            }

            // For arrays, check the element type.
            if (typeInfo.IsArray)
            {
                if (IsTypeIsInaccessibleForSerialization(typeInfo.GetElementType(), fromModule, serializationAssemblyName))
                {
                    return true;
                }
            }

            // For nested types, check that the declaring type is accessible.
            if (typeInfo.IsNested)
            {
                if (IsTypeIsInaccessibleForSerialization(typeInfo.DeclaringType, fromModule, serializationAssemblyName))
                {
                    return true;
                }
            }

            return typeInfo.IsNestedPrivate || typeInfo.IsNestedFamily || type.IsPointer;
        }

        private static bool IsSpecialClass(Type type)
        {
            return type == typeof(object) || type == typeof(Array) || type == typeof(Delegate) ||
                   type == typeof(Enum) || type == typeof(ValueType);
        }

        /// <summary>
        /// Returns true if <paramref name="type"/> has is visible to <paramref name="serializationAssemblyName"/>, false otherwise.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="serializationAssemblyName">The full name of the assembly requiring access to internal types.</param>
        /// <returns>
        /// true if <paramref name="type"/> is visible to <paramref name="serializationAssemblyName"/>, false otherwise
        /// </returns>
        private static bool AreInternalsVisibleTo(TypeInfo type, string serializationAssemblyName)
        {
            if (type.IsVisible) return true;
            if (type.IsConstructedGenericType)
            {
                if (!AreInternalsVisibleTo(type.GetGenericTypeDefinition().GetTypeInfo(), serializationAssemblyName)) return false;
                return type.GetGenericArguments().All(innerType => AreInternalsVisibleTo(innerType.GetTypeInfo(), serializationAssemblyName));
            }

            // If the to-assembly is null, it cannot have internals visible to it.
            if (string.IsNullOrWhiteSpace(serializationAssemblyName))
            {
                return false;
            }

            // Check InternalsVisibleTo attributes on the from-assembly, pointing to the to-assembly.
            var internalsVisibleTo = type.Assembly.GetCustomAttributes<InternalsVisibleToAttribute>();
            return internalsVisibleTo.Any(_ => _.AssemblyName == serializationAssemblyName);
        }
    }
}
