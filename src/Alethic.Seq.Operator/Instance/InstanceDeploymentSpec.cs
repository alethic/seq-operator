using System.Collections.Generic;
using System.Text.Json.Serialization;

using k8s.Models;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes how to deploy a new instance of Seq.
    /// </summary>
    public class InstanceDeploymentSpec
    {

        /// <summary>
        /// Alternate endpoint to connect to deployment instance.
        /// </summary>
        [JsonPropertyName("endpoint")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Endpoint { get; set; }

        /// <summary>
        /// Secret to use for the 'admin' login.
        /// </summary>
        [JsonPropertyName("loginSecretRef")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public V1SecretReference? LoginSecretRef { get; set; }

        /// <summary>
        /// Secret to use for the generated management token.
        /// </summary>
        [JsonPropertyName("tokenSecretRef")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public V1SecretReference? TokenSecretRef { get; set; }

        [JsonPropertyName("annotations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string>? Annotations { get; set; }

        [JsonPropertyName("labels")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string>? Labels { get; set; }

        [JsonPropertyName("affinity")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public V1Affinity? Affinity { get; set; }

        [JsonPropertyName("nodeSelector")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string>? NodeSelector { get; set; }

        [JsonPropertyName("resources")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public V1ResourceRequirements? Resources { get; set; }

        [JsonPropertyName("restartPolicy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RestartPolicy { get; set; }

        [JsonPropertyName("serviceAccountName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ServiceAccountName { get; set; }

        [JsonPropertyName("terminationGracePeriodSeconds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? TerminationGracePeriodSeconds { get; set; }

        [JsonPropertyName("tolerations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<V1Toleration>? Tolerations { get; set; }

        [JsonPropertyName("topologySpreadConstraints")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<V1TopologySpreadConstraint>? TopologySpreadConstraints { get; set; }

        [JsonPropertyName("imagePullSecrets")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<V1LocalObjectReference>? ImagePullSecrets { get; set; }

        [JsonPropertyName("env")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<V1EnvVar>? Env { get; set; }

        [JsonPropertyName("envFrom")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<V1EnvFromSource>? EnvFrom { get; set; }

        [JsonPropertyName("image")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Image { get; set; }

        [JsonPropertyName("imagePullPolicy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ImagePullPolicy { get; set; }

        /// <summary>
        /// Liveness probe for the Seq container. Replaces the operator's own probe entirely when set; leave it unset
        /// to keep the default.
        /// </summary>
        /// <remarks>
        /// Seq answers <c>/health</c> from the same thread pool that serves ingestion, so on a busy or CPU-starved
        /// instance the response can take seconds. The default probe is deliberately conservative rather than
        /// generous, and a deployment that knows its own load may need to say so.
        /// </remarks>
        [JsonPropertyName("livenessProbe")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public V1Probe? LivenessProbe { get; set; }

        /// <summary>
        /// Readiness probe for the Seq container. Replaces the operator's own probe entirely when set; leave it unset
        /// to keep the default.
        /// </summary>
        [JsonPropertyName("readinessProbe")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public V1Probe? ReadinessProbe { get; set; }

        /// <summary>
        /// Startup probe for the Seq container. Replaces the operator's own probe entirely when set; leave it unset
        /// to keep the default.
        /// </summary>
        /// <remarks>
        /// While a startup probe is running the liveness and readiness probes are held off, so this is the one that
        /// governs how long a cold instance is given — a large store replaying on first boot is the case to size for.
        /// </remarks>
        [JsonPropertyName("startupProbe")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public V1Probe? StartupProbe { get; set; }

        [JsonPropertyName("service")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InstanceDeploymentServiceSpec? Service { get; set; }

        [JsonPropertyName("persistence")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InstanceDeploymentPersistenceSpec? Persistence { get; set; }

    }

}
