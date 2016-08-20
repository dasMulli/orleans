using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Orleans.Runtime
{
    public static partial class TypeUtils
    {
        private static readonly ConcurrentDictionary<Tuple<Type, TypeFormattingOptions>, string> ParseableNameCache = new ConcurrentDictionary<Tuple<Type, TypeFormattingOptions>, string>();

        /// <summary>
        /// Returns the non-generic type name without any special characters.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The non-generic type name without any special characters.
        /// </returns>
        public static string GetUnadornedTypeName(this Type type)
        {
            var index = type.Name.IndexOf('`');

            // An ampersand can appear as a suffix to a by-ref type.
            return (index > 0 ? type.Name.Substring(0, index) : type.Name).TrimEnd('&');
        }

        /// <summary>
        /// Returns a string representation of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="includeNamespace">
        /// A value indicating whether or not to include the namespace name.
        /// </param>
        /// <returns>
        /// A string representation of the <paramref name="type"/>.
        /// </returns>
        public static string GetParseableName(this Type type, TypeFormattingOptions options = null)
        {
            options = options ?? new TypeFormattingOptions();
            return ParseableNameCache.GetOrAdd(
                Tuple.Create(type, options),
                _ =>
                {
                    var builder = new StringBuilder();
                    var typeInfo = type.GetTypeInfo();
                    GetParseableName(
                        type,
                        builder,
                        new Queue<Type>(
                            typeInfo.IsGenericTypeDefinition
                                ? typeInfo.GetGenericArguments()
                                : typeInfo.GenericTypeArguments),
                        options);
                    return builder.ToString();
                });
        }

        /// <summary>
        /// Returns a string representation of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="builder">
        /// The <see cref="StringBuilder"/> to append results to.
        /// </param>
        /// <param name="typeArguments">
        /// The type arguments of <paramref name="type"/>.
        /// </param>
        /// <param name="options">
        /// The type formatting options.
        /// </param>
        private static void GetParseableName(
            Type type,
            StringBuilder builder,
            Queue<Type> typeArguments,
            TypeFormattingOptions options)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsArray)
            {
                builder.AppendFormat(
                    "{0}[{1}]",
                    typeInfo.GetElementType().GetParseableName(options),
                    string.Concat(Enumerable.Range(0, type.GetArrayRank() - 1).Select(_ => ',')));
                return;
            }

            if (typeInfo.IsGenericParameter)
            {
                if (options.IncludeGenericTypeParameters)
                {
                    builder.Append(type.GetUnadornedTypeName());
                }

                return;
            }

            if (typeInfo.DeclaringType != null)
            {
                // This is not the root type.
                GetParseableName(typeInfo.DeclaringType, builder, typeArguments, options);
                builder.Append(options.NestedTypeSeparator);
            }
            else if (!string.IsNullOrWhiteSpace(type.Namespace) && options.IncludeNamespace)
            {
                // This is the root type, so include the namespace.
                var namespaceName = type.Namespace;
                if (options.NestedTypeSeparator != '.')
                {
                    namespaceName = namespaceName.Replace('.', options.NestedTypeSeparator);
                }

                if (options.IncludeGlobal)
                {
                    builder.AppendFormat("global::");
                }

                builder.AppendFormat("{0}{1}", namespaceName, options.NestedTypeSeparator);
            }

            if (type.IsConstructedGenericType)
            {
                // Get the unadorned name, the generic parameters, and add them together.
                var unadornedTypeName = type.GetUnadornedTypeName() + options.NameSuffix;
                builder.Append(EscapeIdentifier(unadornedTypeName));
                var generics =
                    Enumerable.Range(0, Math.Min(typeInfo.GetGenericArguments().Count(), typeArguments.Count))
                        .Select(_ => typeArguments.Dequeue())
                        .ToList();
                if (generics.Count > 0 && options.IncludeTypeParameters)
                {
                    var genericParameters = string.Join(
                        ",",
                        generics.Select(generic => GetParseableName(generic, options)));
                    builder.AppendFormat("<{0}>", genericParameters);
                }
            }
            else if (typeInfo.IsGenericTypeDefinition)
            {
                // Get the unadorned name, the generic parameters, and add them together.
                var unadornedTypeName = type.GetUnadornedTypeName() + options.NameSuffix;
                builder.Append(EscapeIdentifier(unadornedTypeName));
                var generics =
                    Enumerable.Range(0, Math.Min(type.GetGenericArguments().Count(), typeArguments.Count))
                        .Select(_ => typeArguments.Dequeue())
                        .ToList();
                if (generics.Count > 0 && options.IncludeTypeParameters)
                {
                    var genericParameters = string.Join(
                        ",",
                        generics.Select(_ => options.IncludeGenericTypeParameters ? _.ToString() : string.Empty));
                    builder.AppendFormat("<{0}>", genericParameters);
                }
            }
            else
            {
                builder.Append(EscapeIdentifier(type.GetUnadornedTypeName() + options.NameSuffix));
            }
        }

        private static string EscapeIdentifier(string identifier)
        {
            switch (identifier)
            {
                case "abstract":
                case "add":
                case "base":
                case "bool":
                case "break":
                case "byte":
                case "case":
                case "catch":
                case "char":
                case "checked":
                case "class":
                case "const":
                case "continue":
                case "decimal":
                case "default":
                case "delegate":
                case "do":
                case "double":
                case "else":
                case "enum":
                case "event":
                case "explicit":
                case "extern":
                case "false":
                case "finally":
                case "fixed":
                case "float":
                case "for":
                case "foreach":
                case "get":
                case "goto":
                case "if":
                case "implicit":
                case "in":
                case "int":
                case "interface":
                case "internal":
                case "lock":
                case "long":
                case "namespace":
                case "new":
                case "null":
                case "object":
                case "operator":
                case "out":
                case "override":
                case "params":
                case "partial":
                case "private":
                case "protected":
                case "public":
                case "readonly":
                case "ref":
                case "remove":
                case "return":
                case "sbyte":
                case "sealed":
                case "set":
                case "short":
                case "sizeof":
                case "static":
                case "string":
                case "struct":
                case "switch":
                case "this":
                case "throw":
                case "true":
                case "try":
                case "typeof":
                case "uint":
                case "ulong":
                case "unsafe":
                case "ushort":
                case "using":
                case "virtual":
                case "where":
                case "while":
                    return "@" + identifier;
                default:
                    return identifier;
            }
        }
    }
}
