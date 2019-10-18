using System.Collections.Generic;

namespace AzSignalR.Monitor.JobRegistry
{
    public class SignalRResourceMeta
    {
        public string Subscription { get; set; }
        public string ResourceGroup { get; set; }
        public string ResourceName { get; set; }
        public string KubeId { get; set; }
        public string RowGuid { get; set; }

        public static SignalRResourceMeta FromDict(IDictionary<string, string> labels)
        {
            labels.TryGetValue("subscription", out var subscription);
            labels.TryGetValue("resourceGroup", out var resourceGroup);
            labels.TryGetValue("resourceName", out var resourceName);
            labels.TryGetValue("resourceKubeId", out var kubeId);
            labels.TryGetValue("resourceRowGuid", out var rowGuid);

            if (string.IsNullOrEmpty(subscription)
                && string.IsNullOrEmpty(resourceGroup)
                && string.IsNullOrEmpty(resourceName)
                && string.IsNullOrEmpty(kubeId)
                && string.IsNullOrEmpty(rowGuid))
            {
                return null;
            }

            var meta = new SignalRResourceMeta
            {
                Subscription = subscription,
                ResourceGroup = resourceGroup,
                ResourceName = resourceName,
                KubeId = kubeId,
                RowGuid = rowGuid,
            };
            return meta;
        }
    }
}
