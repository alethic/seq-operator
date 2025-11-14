using System.Collections.Generic;
using System.Text.Json.Serialization;

using k8s.Models;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes how to deploy a new instance of Seq.
    /// </summary>
    public class InstanceDeploymentServiceSpec
    {

        [JsonPropertyName("annotations")]
        public IDictionary<string, string>? Annotations { get; set; }

        [JsonPropertyName("labels")]
        public IDictionary<string, string>? Labels { get; set; }

        [JsonPropertyName("clusterIP")]
        public string? ClusterIP { get; set; }

        [JsonPropertyName("clusterIPs")]
        public IList<string>? ClusterIPs { get; set; }

        [JsonPropertyName("externalIPs")]
        public IList<string>? ExternalIPs { get; set; }

        [JsonPropertyName("externalName")]
        public string? ExternalName { get; set; }

        [JsonPropertyName("externalTrafficPolicy")]
        public string? ExternalTrafficPolicy { get; set; }

        [JsonPropertyName("internalTrafficPolicy")]
        public string? InternalTrafficPolicy { get; set; }

        [JsonPropertyName("ipFamilies")]
        public IList<string>? IpFamilies { get; set; }

        [JsonPropertyName("ipFamilyPolicy")]
        public string? IpFamilyPolicy { get; set; }

        [JsonPropertyName("loadBalancerClass")]
        public string? LoadBalancerClass { get; set; }

        [JsonPropertyName("loadBalancerIP")]
        public string? LoadBalancerIP { get; set; }

        [JsonPropertyName("loadBalancerSourceRanges")]
        public IList<string>? LoadBalancerSourceRanges { get; set; }

        [JsonPropertyName("publishNotReadyAddresses")]
        public bool? PublishNotReadyAddresses { get; set; }

        [JsonPropertyName("sessionAffinity")]
        public string? SessionAffinity { get; set; }

        [JsonPropertyName("sessionAffinityConfig")]
        public V1SessionAffinityConfig? SessionAffinityConfig { get; set; }

        [JsonPropertyName("trafficDistribution")]
        public string? TrafficDistribution { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("port")]
        public int? Port { get; set; }

    }

}
