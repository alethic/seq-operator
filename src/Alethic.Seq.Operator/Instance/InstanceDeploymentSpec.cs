using System.Collections.Generic;
using System.Text.Json.Serialization;

using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes how to deploy a new instance of Seq.
    /// </summary>
    public class InstanceDeploymentSpec
    {

        /// <summary>
        /// Secret to use for the 'admin' login.
        /// </summary>
        [JsonPropertyName("adminSecretRef")]
        [Required]
        public V1SecretReference? AdminSecretRef { get; set; }

        /// <summary>
        /// Secret to use for the generated management token.
        /// </summary>
        [JsonPropertyName("tokenSecretRef")]
        [Required]
        public V1SecretReference? TokenSecretRef { get; set; }

        [JsonPropertyName("annotations")]
        public IDictionary<string, string>? Annotations { get; set; }

        [JsonPropertyName("labels")]
        public IDictionary<string, string>? Labels { get; set; }

        [JsonPropertyName("affinity")]
        public V1Affinity? Affinity { get; set; }

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

        [JsonPropertyName("env")]
        public IList<V1EnvVar>? Env { get; set; }

        [JsonPropertyName("envFrom")]
        public IList<V1EnvFromSource>? EnvFrom { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("imagePullPolicy")]
        public string? ImagePullPolicy { get; set; }

        /// <summary>
        /// Options to use on the created service.
        /// </summary>
        [JsonPropertyName("service")]
        public InstanceDeploymentServiceSpec? Service { get; set; }

        /// <summary>
        /// Options to use on the created storage.
        /// </summary>
        [JsonPropertyName("persistence")]
        public InstanceDeploymentPersistenceSpec? Persistence { get; set; }

    }

}
