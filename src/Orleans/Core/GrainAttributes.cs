using System;
using System.Linq;
using System.Reflection;
using Orleans.GrainDirectory;
namespace Orleans
{
    namespace MultiCluster
    {
        /// <summary>
        /// base class for multi cluster registration strategies.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public abstract class RegistrationAttribute : Attribute
        {
            internal MultiClusterRegistrationStrategy RegistrationStrategy { get; private set; }

            internal RegistrationAttribute(MultiClusterRegistrationStrategy strategy)
            {
                RegistrationStrategy = strategy ?? MultiClusterRegistrationStrategy.GetDefault();
            }
        }

        /// <summary>
        /// This attribute indicates that instances of the marked grain class
        /// will have an independent instance for each cluster with 
        /// no coordination. 
        /// </summary>
        public class OneInstancePerClusterAttribute : RegistrationAttribute
        {
            public OneInstancePerClusterAttribute()
                : base(ClusterLocalRegistration.Singleton)
            {
            }
        }
    }

    namespace Placement
    {
        using Orleans.Runtime;

        /// <summary>
        /// Base for all placement policy marker attributes.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public abstract class PlacementAttribute : Attribute
        {
            internal PlacementStrategy PlacementStrategy { get; private set; }

            internal PlacementAttribute(PlacementStrategy placement)
            {
                PlacementStrategy = placement ?? PlacementStrategy.GetDefault();
            }
        }

        /// <summary>
        /// Marks a grain class as using the <c>RandomPlacement</c> policy.
        /// </summary>
        /// <remarks>
        /// This is the default placement policy, so this attribute does not need to be used for normal grains.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public sealed class RandomPlacementAttribute : PlacementAttribute
        {
            public RandomPlacementAttribute() :
                base(RandomPlacement.Singleton)
            { }
        }

        /// <summary>
        /// Marks a grain class as using the <c>PreferLocalPlacement</c> policy.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false) ]
        public sealed class PreferLocalPlacementAttribute : PlacementAttribute
        {
            public PreferLocalPlacementAttribute() :
                base(PreferLocalPlacement.Singleton)
            { }
        }

        /// <summary>
        /// Marks a grain class as using the <c>ActivationCountBasedPlacement</c> policy.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public sealed class ActivationCountBasedPlacementAttribute : PlacementAttribute
        {
            public ActivationCountBasedPlacementAttribute() :
                base(ActivationCountBasedPlacement.Singleton)
            { }
        }
    }

    namespace Providers
    {
        /// <summary>
        /// The [Orleans.Providers.StorageProvider] attribute is used to define which storage provider to use for persistence of grain state.
        /// <para>
        /// Specifying [Orleans.Providers.StorageProvider] property is recommended for all grains which extend Grain&lt;T&gt;.
        /// If no [Orleans.Providers.StorageProvider] attribute is  specified, then a "Default" strorage provider will be used.
        /// If a suitable storage provider cannot be located for this grain, then the grain will fail to load into the Silo.
        /// </para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class StorageProviderAttribute : Attribute
        {
            public StorageProviderAttribute()
            {
                    ProviderName = Runtime.Constants.DEFAULT_STORAGE_PROVIDER_NAME;
            }
            /// <summary>
            /// The name of the storage provider to ne used for persisting state for this grain.
            /// </summary>
            public string ProviderName { get; set; }
        }
    }

    [AttributeUsage(AttributeTargets.Interface)]
    internal sealed class FactoryAttribute : Attribute
    {
        public enum FactoryTypes
        {
            Grain,
            ClientObject,
            Both
        };

        private readonly FactoryTypes factoryType;

        public FactoryAttribute(FactoryTypes factoryType)
        {
            this.factoryType = factoryType;
        }

        internal static FactoryTypes CollectFactoryTypesSpecified(Type type)
        {
            var attribs = type.GetTypeInfo().GetCustomAttributes(typeof(FactoryAttribute), inherit: true).ToArray();

            // if no attributes are specified, we default to FactoryTypes.Grain.
            if (0 == attribs.Length)
                return FactoryTypes.Grain;
            
            // otherwise, we'll consider all of them and aggregate the specifications
            // like flags.
            FactoryTypes? result = null;
            foreach (var i in attribs)
            {
                var a = (FactoryAttribute)i;
                if (result.HasValue)
                {
                    if (a.factoryType == FactoryTypes.Both)
                        result = a.factoryType;
                    else if (a.factoryType != result.Value)
                        result = FactoryTypes.Both;
                }
                else
                    result = a.factoryType;
            }

            if (result.Value == FactoryTypes.Both)
            {
                throw 
                    new NotSupportedException(
                        "Orleans doesn't currently support generating both a grain and a client object factory but we really want to!");
            }
            
            return result.Value;
        }

        public static FactoryTypes CollectFactoryTypesSpecified<T>()
        {
            return CollectFactoryTypesSpecified(typeof(T));
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public sealed class ImplicitStreamSubscriptionAttribute : Attribute
    {
        public string Namespace { get; private set; }
        
        // We have not yet come to an agreement whether the provider should be specified as well.
        public ImplicitStreamSubscriptionAttribute(string streamNamespace)
        {
            Namespace = streamNamespace;
        }
    }
}
