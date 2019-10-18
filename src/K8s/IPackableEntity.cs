using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace AzSignalR.Monitor.Storage.Entities
{
    public interface IPackableEntity
    {
        [JsonConverter(typeof(StringEnumConverter))]
        ResourceType Type { get; }

        /// <summary>
        /// A stable identifier which may uniquely identify a resource at a given time.
        ///
        /// If given a (identifier, time) tuple, we should be able to locate the only one resource version, or null,
        /// i.e. there will not be two different valid resource at a given time for the same identifier.
        ///
        /// However, as time changes, the underlying resource may be changed.
        ///
        /// For example, this may be
        /// * resource name + cluster name for ACS related resource
        /// * SignalR instance row key for the SignalR instances
        /// </summary>
        string ResourceIdentifier { get; }

        /// <summary>
        /// The resource name.
        ///
        /// The uniqueness of the name doesn't matter. At a given time, two or more resources may share the same name.
        /// </summary>
        string Name { get; }

        List<string> AlternaltiveNames { get; }

        /// <summary>
        /// The region of the resource if the resource name is bounded to a given region, like the ACS related resources.
        ///
        /// Or null if the resource name is not bounded, i.e., global, like the SignalR instance names.
        /// </summary>
        string NameRegion { get; }

        /// <summary>
        /// The time when the resource is created. If not available, fallback to ResourceVersionTime.
        /// </summary>
        DateTime CreatedTime { get; }

        /// <summary>
        /// The version time of this resource.
        ///
        /// For ACS related resource, this should be the same as the CheckVersionTime.
        /// For SignalR instance rows, this should be the Timestamp.
        /// </summary>
        DateTime ResourceVersionTime { get; }

        /// <summary>
        /// The version when the resource was being checked.
        /// </summary>
        DateTime CheckTime { get; }

        /// <summary>
        /// Whether this resource is deleted at this given version.
        ///
        /// A deletion marks the end of the current name validity scope. Later if we see the resource gets revealed,
        /// we should start a new validity scope.
        /// </summary>
        bool IsDeleted { get; }
    }

    public static class IPackableEntityExtensions
    {
        public static string PackPartitionKey(this IPackableEntity entity)
        {
            return PackableEntityUtil.PackPartitionKey(entity.Type, entity.ResourceIdentifier);
        }

        public static string PackRowKey(this IPackableEntity entity)
        {
            return entity.PackVersion();
        }

        public static string PackVersion(this IPackableEntity entity)
        {
            return Utils.InversedTimeKey(entity.ResourceVersionTime);
        }
        /*
        public static ResourcePackEntity Pack(this IPackableEntity entity)
        {
            return new ResourcePackEntity(entity.PackPartitionKey(), entity.PackRowKey())
            {
                Type = entity.Type,
                JsonPayload = JsonConvert.SerializeObject(entity),
            };
        }
        */
        public static DateTime ValidTime(this IPackableEntity entity)
        {
            return entity.IsDeleted ? entity.ResourceVersionTime : entity.CheckTime;
        }

        public static string PrettyString(this IPackableEntity entity)
        {
            return $"{entity.Type}/{entity.NameRegion}/{entity.Name}/{entity.PackPartitionKey()}/{entity.ValidTime()}";
        }
    }

    public static class PackableEntityUtil
    {
        public static string PackPartitionKey(ResourceType type, string resourceIdentifier)
        {
            return $"{type}|{resourceIdentifier}";
        }
        public static string PackPartitionKeyForACSResource(ResourceType type, string name, string resourceGroup)
        {
            if (!type.IsPerACS())
            {
                throw new ArgumentException($"Type {type} is not specified to ACS");
            }
            return PackPartitionKey(type, ResourceIdentifierForACS(name, resourceGroup));
        }

        public static string ResourceIdentifierForACS(string name, string resourceGroup)
        {
            return $"{name}|{resourceGroup}";
        }
    }
}
