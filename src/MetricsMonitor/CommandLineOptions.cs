using AzSignalR.Monitor.JobRegistry;
using k8s;
using K8SClient;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MetricsMonitor
{
    [VersionOptionFromMember(MemberName = nameof(GetVersion))]
    [HelpOption("--help")]
    [Subcommand(
        typeof(ScanPodMetricsOption))]
    internal class CommandLineOptions : BaseOption
    {
        public string GetVersion()
            => typeof(CommandLineOptions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        protected override Task OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.CompletedTask;
        }
    }

    
    [Command(Name = "podmetrics", FullName = "podmetrics", Description = "Scan the metrics for all pods in a specific region")]
    internal class ScanPodMetricsOption : BaseOption
    {
        [Option("-k|--k8sConfig", Description = "Specify the k8s configuration")]
        public string K8sConfig { get; set; }

        [Option("-r|--region", Description = "Specify the region where you want to get metrics from")]
        public string Region { get; set; }

        [Option("-c|--certPath", Description = "Specify the mdm client certificate path. Default is mdm.pfx")]
        public string MdmClientCertPath { get; set; } = "mdm.pfx";

        [Option("-p|--certPasswd", Description = "Specify the certificate password")]
        public string MdmClientCertPasswd { get; set; }

        [Option("-m|--metricsName", Description = "Specify the metrics name: <PodConnectionCount>|<MessageCount>, default is PodConnectionCount")]
        public string MetricsName { get; set; } = MdmClient.PodConnectionCount;

        private async Task<string[]> GetFreeTierDeployments(IKubernetes client, CancellationToken cancellationToken = default)
        {
            var deployments = await K8sOperations.ListAllDeployments(client, "default");
            var freeDeployments = deployments.Where(d => d.Spec.Replicas == 1).Select(d => d.Metadata.Name).ToArray();
            return freeDeployments;
        }

        private bool isFreeTierPod(string[] freeTierList, string podName)
        {
            if (freeTierList != null && freeTierList.Length > 0)
            {
                foreach (var free in freeTierList)
                {
                    if (podName.StartsWith(free))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            if (!ValidateParameters())
            {
                return;
            }
            var kubeClientCache = new KubernetesClientCache();
            var kubeClient = kubeClientCache.Get("CheckPods", File.ReadAllText(this.K8sConfig));
            var pods = await K8sOperations.ListPodsAsync(kubeClient, "default");
            var freeTierDeployArray = await GetFreeTierDeployments(kubeClient);
            var mdmClient = new MdmClient(MdmClientCertPath, MdmClientCertPasswd, Region);
            var podMetrics = await mdmClient.GetDimensionCountMetricsAsync(MetricsName, TimeSpan.FromHours(1), "InstanceId");
            if (podMetrics == null)
            {
                Console.WriteLine($"Fail to get metrics for {Region}");
                return;
            }
            bool foundFreeTier = false;
            Parallel.ForEach(pods, pod =>
            {
                bool isFreeTier = isFreeTierPod(freeTierDeployArray, pod.Metadata.Name);
                if (isFreeTier)
                {
                    foundFreeTier = true;
                }
                var hasMetrics = podMetrics.TryGetValue(pod.Metadata.Name, out double count);
                if (!hasMetrics)
                {
                    if (isFreeTier)
                    {
                        Console.WriteLine($"Free pod {pod.Metadata.Name} miss metrics");
                    }
                    else
                    {
                        Console.WriteLine($"{pod.Metadata.Name} miss metrics");
                    }
                }
            });
            if (!foundFreeTier)
            {
                Console.WriteLine("Does not see free tier");
            }
        }

        private bool ValidateParameters()
        {
            if (String.IsNullOrEmpty(this.K8sConfig))
            {
                Console.WriteLine("Missing k8s configuration, please specify it by -k");
                return false;
            }
            if (String.IsNullOrEmpty(this.Region))
            {
                Console.WriteLine("Missing region, please specify it by -r");
                return false;
            }
            if (String.IsNullOrEmpty(this.MdmClientCertPath))
            {
                Console.WriteLine("Missing Mdm client certification, please specify it by -c");
                return false;
            }
            if (String.IsNullOrEmpty(this.MdmClientCertPasswd))
            {
                Console.WriteLine("Missing Mdm client certification password, please specify it by -p");
                return false;
            }
            return true;
        }
    }

    [HelpOption("--help")]
    internal abstract class BaseOption
    {
        protected virtual Task OnExecuteAsync(CommandLineApplication app)
        {
            return Task.CompletedTask;
        }

        protected static void ReportError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Unexpected error: {ex}");
            Console.ResetColor();
        }
    }
}
