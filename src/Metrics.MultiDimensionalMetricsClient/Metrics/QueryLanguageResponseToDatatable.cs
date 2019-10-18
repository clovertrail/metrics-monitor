// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueryLanguageResponseToDatatable.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Class which helps in converting query language response from FE to Datatable.
    /// </summary>
    internal static class QueryLanguageResponseToDatatable
    {
        /// <summary>
        /// Converts the query language response to datatable.
        /// Input looks like as follows :
        /// {
        ///  "queryRequest": {
        ///    "metricIdentifier": {
        ///      "monitoringAccount": "MetricTeamInternalMetrics",
        ///      "metricNamespace": "Metrics.Server",
        ///      "metricName": "ClientAggregatedMetricCount"
        ///    },
        ///    "startTimeUtc": "0001-01-01T00:00:00.0000000Z",
        ///    "endTimeUtc": "0001-01-01T00:00:00.0000000Z",
        ///    "seriesResolutionInMinutes": 0,
        ///    "aggregationType": 0,
        ///    "numberOfResultsToReturn": 0,
        ///    "orderBy": 0,
        ///    "zeroAsNoValueSentinel": false,
        ///    "aggregateAcrossAccounts": false,
        ///    "lastValueMode": false
        ///  },
        ///  "endTimeUtc": "2019-01-29T06:48:00.0000000Z",
        ///  "startTimeUtc": "2019-01-29T05:48:00.0000000Z",
        ///  "timeResolutionInMinutes": 1,
        ///  "filteredTimeSeriesList": {
        ///    "$type": "Microsoft.Cloud.Metrics.Client.Query.FilteredTimeSeries[], Microsoft.Cloud.Metrics.Client",
        ///    "$values": [
        ///      {
        ///        "metricIdentifier": {
        ///          "monitoringAccount": "MetricTeamInternalMetrics",
        ///          "metricNamespace": "Metrics.Server",
        ///          "metricName": "ClientAggregatedMetricCount"
        ///        },
        ///        "dimensionList": {
        ///          "$type": "System.Collections.Generic.KeyValuePair`2[[System.String, mscorlib],[System.String, mscorlib]][], mscorlib",
        ///          "$values": [{"key":"datacenter","value":"mdmtest1-black"}]
        /// },
        ///        "evaluatedResult": "NaN",
        ///        "timeSeriesValues": {
        ///          "$type": "System.Collections.Generic.KeyValuePair`2[[Microsoft.Cloud.Metrics.Client.Metrics.SamplingType, Microsoft.Cloud.Metrics.Client],[System.Double[], mscorlib]][], mscorlib",
        ///          "$values": [
        ///            {
        ///              "key": {
        ///                "name": "Average"
        ///              },
        ///              "value": [
        ///                265.10236098167132,
        ///              ]
        ///            }
        ///          ]
        ///        }
        ///      }
        ///    ]
        ///  },
        ///  "errorCode": 0,
        ///  "diagnosticInfo": {}
        /// }
        ///
        /// Output looks like as follows:
        /// [
        ///   {
        ///         "TimestampUtc": "02/04/2019 07:11:00",
        ///         "AccountName": "MetricTeamInternalMetrics",
        ///         "MetricNamespace": "Metrics.Server",
        ///         "MetricName": "ClientAggregatedMetricCount",
        ///         "Datacenter": "EastUS2",
        ///         "Average": 70.083851254134714
        ///    },
        ///    {
        ///         "TimestampUtc": "02/04/2019 07:12:00",
        ///         "AccountName": "MetricTeamInternalMetrics",
        ///         "MetricNamespace": "Metrics.Server",
        ///         "MetricName": "ClientAggregatedMetricCount",
        ///         "Datacenter": "EastUS2",
        ///         "Average": 67.305346411549351
        ///     }
        /// ]
        /// </summary>
        /// <param name="responseFromMetrics">Input data stream to convert to datatable.</param>
        /// <returns>
        /// Input data stream converted to datatable.
        /// </returns>
        public static JArray GetResponseAsTable(Stream responseFromMetrics)
        {
            using (var reader = new StreamReader(responseFromMetrics, Encoding.UTF8))
            {
                var jObject = (JObject)JsonSerializer.Create().Deserialize(reader, typeof(JObject));

                JArray returnObject = JArray.Parse("[]");
                var startTimeUtc = DateTime.Parse(jObject["startTimeUtc"].Value<string>());
                var timeResolution = TimeSpan.FromMinutes(jObject["timeResolutionInMinutes"].Value<int>());
                var outerValues = jObject["filteredTimeSeriesList"]["$values"] as JArray;
                Dictionary<DateTime, JObject> rowMap = new Dictionary<DateTime, JObject>();

                foreach (var outerValue in outerValues)
                {
                    var innerValues = outerValue["timeSeriesValues"]["$values"] as JArray;
                    string accountName = outerValue["metricIdentifier"]["monitoringAccount"].Value<string>();
                    string metricNamespace = outerValue["metricIdentifier"]["metricNamespace"].Value<string>();
                    string metricName = outerValue["metricIdentifier"]["metricName"].Value<string>();
                    var dimensions = outerValue["dimensionList"]["$values"] as JArray;

                    rowMap.Clear();

                    foreach (var actualValue in innerValues)
                    {
                        var samplingType = actualValue["key"]["name"].Value<string>();
                        var dataPoints = actualValue["value"] as JArray;

                        var currentTimeStamp = startTimeUtc;
                        foreach (var val in dataPoints)
                        {
                            JObject row;
                            if (rowMap.ContainsKey(currentTimeStamp))
                            {
                                row = rowMap[currentTimeStamp];
                            }
                            else
                            {
                                row = JObject.Parse("{}");
                                row.Add("TimestampUtc", currentTimeStamp.ToString(DateTimeFormatInfo.InvariantInfo));
                                row.Add("i_AccountName", accountName);
                                row.Add("i_MetricNamespace", metricNamespace);
                                row.Add("i_MetricName", metricName);

                                foreach (var dim in dimensions)
                                {
                                    row.Add(dim["key"].Value<string>(), dim["value"].Value<string>());
                                }

                                rowMap[currentTimeStamp] = row;
                                returnObject.Add(row);
                            }

                            var rowDimValue = val.Value<double>();
                            if (double.IsNaN(rowDimValue))
                            {
                                rowDimValue = 0;
                            }

                            row.Add(samplingType, rowDimValue);
                            currentTimeStamp = currentTimeStamp.Add(timeResolution);
                        }
                    }
                }

                return returnObject;
            }
        }
    }
}