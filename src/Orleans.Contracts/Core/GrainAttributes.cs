﻿using System;

namespace Orleans
{
    namespace Concurrency
    {
        /// <summary>
        /// The ReadOnly attribute is used to mark methods that do not modify the state of a grain.
        /// <para>
        /// Marking methods as ReadOnly allows the run-time system to perform a number of optimizations
        /// that may significantly improve the performance of your application.
        /// </para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        internal sealed class ReadOnlyAttribute : Attribute
        {
        }

        /// <summary>
        /// The Reentrant attribute is used to mark grain implementation classes that allow request interleaving within a task.
        /// <para>
        /// This is an advanced feature and should not be used unless the implications are fully understood.
        /// That said, allowing request interleaving allows the run-time system to perform a number of optimizations
        /// that may significantly improve the performance of your application. 
        /// </para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class ReentrantAttribute : Attribute
        {
        }

        /// <summary>
        /// The Unordered attribute is used to mark grain interface in which the delivery order of
        /// messages is not significant.
        /// </summary>
        [AttributeUsage(AttributeTargets.Interface)]
        public sealed class UnorderedAttribute : Attribute
        {
        }

        /// <summary>
        /// The StatelessWorker attribute is used to mark grain class in which there is no expectation
        /// of preservation of grain state between requests and where multiple activations of the same grain are allowed to be created by the runtime. 
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class StatelessWorkerAttribute : Attribute
        {
            /// <summary>
            /// Maximal number of local StatelessWorkers in a single silo.
            /// </summary>
            public int MaxLocalWorkers { get; private set; }

            public StatelessWorkerAttribute(int maxLocalWorkers)
            {
                MaxLocalWorkers = maxLocalWorkers;
            }

            public StatelessWorkerAttribute()
            {
                MaxLocalWorkers = -1;
            }
        }

        /// <summary>
        /// The AlwaysInterleaveAttribute attribute is used to mark methods that can interleave with any other method type, including write (non ReadOnly) requests.
        /// </summary>
        /// <remarks>
        /// Note that this attribute is applied to method declaration in the grain interface, 
        /// and not to the method in the implementation class itself.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class AlwaysInterleaveAttribute : Attribute
        {
        }

        /// <summary>
        /// The Immutable attribute indicates that instances of the marked class or struct are never modified
        /// after they are created.
        /// </summary>
        /// <remarks>
        /// Note that this implies that sub-objects are also not modified after the instance is created.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
        public sealed class ImmutableAttribute : Attribute
        {
        }
    }

    namespace CodeGeneration
    {
        /// <summary>
        /// The TypeCodeOverrideAttribute attribute allows to specify the grain interface ID or the grain class type code
        /// to override the default ones to avoid hash collisions
        /// </summary>
        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
        public sealed class TypeCodeOverrideAttribute : Attribute
        {
            /// <summary>
            /// Use a specific grain interface ID or grain class type code (e.g. to avoid hash collisions)
            /// </summary>
            public int TypeCode { get; private set; }

            public TypeCodeOverrideAttribute(int typeCode)
            {
                TypeCode = typeCode;
            }
        }

        /// <summary>
        /// Used to mark a method as providing a copier function for that type.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class CopierMethodAttribute : Attribute
        {
        }

        /// <summary>
        /// Used to mark a method as providinga serializer function for that type.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class SerializerMethodAttribute : Attribute
        {
        }

        /// <summary>
        /// Used to mark a method as providing a deserializer function for that type.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class DeserializerMethodAttribute : Attribute
        {
        }

        /// <summary>
        /// Used to make a class for auto-registration as a serialization helper.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class RegisterSerializerAttribute : Attribute
        {
        }
    }
}
