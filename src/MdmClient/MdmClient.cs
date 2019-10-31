using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Cloud.Metrics.Client;
using Microsoft.Cloud.Metrics.Client.Metrics;
using Microsoft.Cloud.Metrics.Client.Query;
using Microsoft.Online.Metrics.Serialization.Configuration;

namespace MetricsMonitor
{
    public class MdmClient
    {
        public static readonly string MessageCount = "MessageCount";
        public static readonly string PodConnectionCount = "PodConnectionCount";

        private readonly X509Certificate2 _certificate;
        private readonly MdmEnvironment _mdmEnvironment = MdmEnvironment.Production;
        private readonly string _namespace = "Shoebox";
        private readonly string _namespace2 = "ShoeboxInternal";
        private readonly string _accountPrefix = "MicrosoftSignalRServiceShoebox";
        private string _account;

        public MdmClient(string certPath, string passwd, string region)
        {
            _certificate = new X509Certificate2(certPath, passwd);
            _account = $"{_accountPrefix}{region}";
        }

        public async Task<IDictionary<string, double>> GetDimensionCountMetricsAsync(string metricName, TimeSpan backTime, string dimension)
        {
            if (MessageCount.Equals(metricName))
            {
                return await GetDimensionMessageCountMetricsAsync(backTime, dimension);
            }
            else
            {
                if (PodConnectionCount.Equals(metricName))
                {
                    return await GetDimensionPodConnectionCountMetricsAsync(backTime, dimension);
                }
            }
            return null;
        }

        public async Task<IDictionary<string, double>> GetDimensionPodConnectionCountMetricsAsync(TimeSpan backTime, string dimension)
        {
            return await GetDimensionMetricsCountCoreAsync(PodConnectionCount, backTime, dimension);
        }

        public async Task<IDictionary<string, double>> GetDimensionMessageCountMetricsAsync(TimeSpan backTime, string dimension)
        {
            return await GetDimensionMetricsCountCoreAsync(MessageCount, backTime, dimension);
        }

        public static bool isShoeboxNamespace(string metricName)
        {
            switch (metricName)
            {
                case "MessageCount":
                case "ConnectionCount":
                case "InboundTraffic":
                case "OutboundTraffic":
                    return true;
                case "PodConnectionCount":
                case "RedisPubCount":
                case "TotalDelta":
                case "ConnectionCountRaw":
                    return false;
            }
            return false;
        }

        public MetricIdentifier GenMetricId(string metricName)
        {
            if (isShoeboxNamespace(metricName))
            {
                return new MetricIdentifier(_account, _namespace, metricName);
            }
            else
            {
                return new MetricIdentifier(_account, _namespace2, metricName);
            }
        }

        public async Task<IDictionary<string, double>> GetDimensionMetricsCountCoreAsync(string metricsName, TimeSpan backTime, string dimension)
        {
            IDictionary<string, double> result = new Dictionary<string, double>();
            var connectionInfo = new ConnectionInfo(_certificate, _mdmEnvironment);
            var metricReader = new MetricReader(connectionInfo);
            var metricId = GenMetricId(metricsName);
            var dimensionFilters = new List<DimensionFilter>
            {
                DimensionFilter.CreateIncludeFilter(dimension)
            };

            try
            {
                var now = DateTime.UtcNow;
                var results = await metricReader.GetTimeSeriesAsync(
                    metricId,
                    dimensionFilters,
                    now - backTime,
                    now,
                    new[] { SamplingType.Count },
                    new SelectionClauseV3(new PropertyDefinition(PropertyAggregationType.Sum, SamplingType.Count), 100_000, OrderBy.Descending),
                    outputDimensionNames: new List<string> { dimension }
                );
                return results.Results.ToDictionary(r => r.DimensionList.First().Value, r => r.EvaluatedResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to get metrics data from Geneva", ex);
                return null;
            }
        }
    }
}
