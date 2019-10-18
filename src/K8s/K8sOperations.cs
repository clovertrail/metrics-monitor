using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace K8SClient
{
    public class K8sOperations
    {
        public static async Task<List<Extensionsv1beta1Ingress>> ListAllIngressAsync(IKubernetes client, string ns, CancellationToken cancellationToken = default)
        {
            var results = new List<Extensionsv1beta1Ingress>();
            string continueParameter = null;
            do
            {
                var resp = await client.ListNamespacedIngressAsync(ns, continueParameter: continueParameter, limit: 100, cancellationToken: cancellationToken);
                continueParameter = resp.Metadata.ContinueProperty;
                results.AddRange(resp.Items);
            } while (continueParameter != null);
            return results;
        }

        public static async Task<List<V1Service>> ListAllServiceAsync(IKubernetes client, string ns, CancellationToken cancellationToken = default)
        {
            var results = new List<V1Service>();
            string continueParameter = null;
            do
            {
                var resp = await client.ListNamespacedServiceAsync(ns, continueParameter: continueParameter, limit: 100, cancellationToken: cancellationToken);
                continueParameter = resp.Metadata.ContinueProperty;
                results.AddRange(resp.Items);
            } while (continueParameter != null);
            return results;
        }

        public static async Task<List<V1Deployment>> ListAllDeployments(IKubernetes client, string ns, CancellationToken cancellationToken = default)
        {
            var results = new List<V1Deployment>();
            string continueParameter = null;
            do
            {
                var resp = await client.ListNamespacedDeploymentAsync(ns, continueParameter: continueParameter, limit: 100, cancellationToken: cancellationToken);
                continueParameter = resp.Metadata.ContinueProperty;
                results.AddRange(resp.Items);
            } while (continueParameter != null);
            return results;
        }

        public static async Task<List<V1ReplicaSet>> ListReplicaSet(IKubernetes client, string ns, CancellationToken cancellationToken = default)
        {
            var results = new List<V1ReplicaSet>();
            string continueParameter = null;
            do
            {
                var resp = await client.ListNamespacedReplicaSetAsync(ns, continueParameter: continueParameter, limit: 100, cancellationToken: cancellationToken);
                continueParameter = resp.Metadata.ContinueProperty;
                results.AddRange(resp.Items);
            } while (continueParameter != null);
            return results;
        }

        public static async Task<List<V1Pod>> ListPodsAsync(IKubernetes client, string ns, CancellationToken cancellationToken = default)
        {
            var list = new List<V1Pod>();
            string continueParameter = null;
            do
            {
                var resp = await client.ListNamespacedPodAsync(ns, continueParameter: continueParameter, limit: 100, cancellationToken: cancellationToken);
                continueParameter = resp.Metadata.ContinueProperty;
                list.AddRange(resp.Items);
            } while (continueParameter != null);
            return list;
        }
    }
}
