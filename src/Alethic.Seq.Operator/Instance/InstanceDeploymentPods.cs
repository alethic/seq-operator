using System.Collections.Generic;
using System.Text.Json.Serialization;

using k8s.Models;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes how to deploy a new instance of Seq.
    /// </summary>
    public class InstanceDeploymentPods
    {

        [JsonPropertyName("annotations")]
        public IDictionary<string, string>? Annotations { get; set; }

        [JsonPropertyName("labels")]
        public IDictionary<string, string>? Labels { get; set; }

        [JsonPropertyName("affinity")]
        public V1Affinity? Affinity { get; set; }

        [JsonPropertyName("dnsConfig")]
        public V1PodDNSConfig? DnsConfig { get; set; }

        [JsonPropertyName("dnsPolicy")]
        public string? DnsPolicy { get; set; }

        [JsonPropertyName("hostAliases")]
        public IList<V1HostAlias>? HostAliases { get; set; }

        [JsonPropertyName("nodeSelector")]
        public IDictionary<string, string>? NodeSelector { get; set; }

        [JsonPropertyName("resources")]
        public V1ResourceRequirements? Resources { get; set; }

        [JsonPropertyName("restartPolicy")]
        public string? RestartPolicy { get; set; }

        [JsonPropertyName("securityContext")]
        public V1PodSecurityContext? SecurityContext { get; set; }

        [JsonPropertyName("serviceAccountName")]
        public string? ServiceAccountName { get; set; }

        [JsonPropertyName("terminationGracePeriodSeconds")]
        public long? TerminationGracePeriodSeconds { get; set; }

        [JsonPropertyName("tolerations")]
        public IList<V1Toleration>? Tolerations { get; set; }

        [JsonPropertyName("topologySpreadConstraints")]
        public IList<V1TopologySpreadConstraint>? TopologySpreadConstraints { get; set; }

        [JsonPropertyName("imagePullSecrets")]
        public IList<V1LocalObjectReference>? ImagePullSecrets { get; set; }

        [JsonPropertyName("container")]
        public InstanceDeploymentPodsContainer? Container { get; set; }

    }

}
