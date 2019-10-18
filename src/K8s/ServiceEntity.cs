using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;

namespace AzSignalR.Monitor.Storage.Entities
{
    public class ServiceEntity : CompoundTableEntity, IPackableEntity
    {
        public const string Connectivity = "connectivity";

        public ServiceEntity() { }
        public ServiceEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) { }

        public string SubscriptionId { get; set; }
        public string Region { get; set; }
        public string ResourceGroup { get; set; }
        public string ResourceName { get; set; }
        public string ResourceRowGuid { get; set; }
        public string ResourceKubeId { get; set; }
        // from Metadata
        public string Name { get; set; }
        public string NamespaceProperty { get; set; }
        public string ClusterName { get; set; }
        public string LoadBalancer { get; set; }
        public string ResourceVersion { get; set; }
        public string Labels { get; set; } // from IDictionary<string, string>
        public string DeletionTimestamp { get; set; }
        public long DeletionGracePeriodSeconds { get; set; }
        public string CreationTimestamp { get; set; }
        public string Annotations { get; set; } // from IDictionary<string, string>
        public string SelfLink { get; set; }
        public string Uid { get; set; }

        // Spec
        public string ClusterIP { get; set; }
        public string ExternalName { get; set; }
        public string ExternalTrafficPolicy { get; set; }
        public string LoadBalancerIP { get; set; }

        public int Replicas { get; set; }
        // Status
        public string IngressString { get; set; }
        public string ExternalIP { get; set; }
        /*
        [ConvertableEntityProperty]
        public IDictionary<string, Property> HealthItems { get; set; }
        */
        public string Version { get; set; }

        #region IPackableEntity
        [IgnoreProperty]
        ResourceType IPackableEntity.Type => ResourceType.Service;
        [IgnoreProperty]
        string IPackableEntity.ResourceIdentifier => PackableEntityUtil.ResourceIdentifierForACS(Name, ClusterName);
        [IgnoreProperty]
        string IPackableEntity.Name => Name;
        [IgnoreProperty]
        List<string> IPackableEntity.AlternaltiveNames => null;
        [IgnoreProperty]
        string IPackableEntity.NameRegion => Region;
        public DateTime CreatedTime { get; set; }
        public DateTime ResourceVersionTime { get; set; }
        [IgnoreProperty]
        DateTime IPackableEntity.CheckTime => ResourceVersionTime;
        [IgnoreProperty]
        bool IPackableEntity.IsDeleted => false;
        #endregion

    }
}
