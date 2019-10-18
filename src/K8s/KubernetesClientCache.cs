using k8s;
using System.Collections.Generic;

namespace AzSignalR.Monitor.JobRegistry
{
    /// <summary>
    /// A workaround for the connection leak issue in ServiceClient (base of Kubernetes).
    ///
    /// see: https://github.com/Azure/azure-sdk-for-net/issues/5977
    /// </summary>
    class FixedKubernetes : Kubernetes
    {
        public FixedKubernetes(KubernetesClientConfiguration config) : base(config) { }

        protected override void Dispose(bool disposing)
        {
            HttpClientHandler?.Dispose();

            // base.Dispose will set the HttpClientHandler to null.
            base.Dispose(disposing);
        }
    }

    public class KubernetesClientCache
    {
        private readonly IDictionary<string, KubernetesClientWrapper> _instances = new Dictionary<string, KubernetesClientWrapper>();

        public Kubernetes Get(string cluster, string kubeConfig)
        {
            if (_instances.TryGetValue(cluster, out var wrapper))
            {
                if (wrapper.KubeConfig == kubeConfig)
                {
                    return wrapper.Client;
                }
            }
            lock (_instances)
            {
                if (_instances.TryGetValue(cluster, out wrapper))
                {
                    if (wrapper.KubeConfig == kubeConfig)
                    {
                        return wrapper.Client;
                    }
                }
                try
                {
                    using (var kubeConfigStream = Utils.GenerateStreamFromString(kubeConfig))
                    {
                        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigStream);
                        var client = new FixedKubernetes(config);
                        _instances.Add(cluster, new KubernetesClientWrapper(kubeConfig, client));
                        return client;
                    }
                }
                finally
                {
                    if (wrapper != null)
                    {
                        // this won't clean all the resources, that's why we need the Cache impl here
                        wrapper.Client?.Dispose();
                    }
                }
            }
        }

        class KubernetesClientWrapper
        {
            public string KubeConfig { get; }
            public Kubernetes Client { get; }

            public KubernetesClientWrapper(string config, Kubernetes client)
            {
                KubeConfig = config;
                Client = client;
            }
        }
    }
}
