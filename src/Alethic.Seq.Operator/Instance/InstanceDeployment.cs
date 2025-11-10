using System.Text.Json.Serialization;

using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes how to deploy a new instance of Seq.
    /// </summary>
    public class InstanceDeployment
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

        /// <summary>
        /// Options to use on created pods.
        /// </summary>
        [JsonPropertyName("pods")]
        public InstanceDeploymentPods? Pods { get; set; }

        /// <summary>
        /// Options to use on the created service.
        /// </summary>
        [JsonPropertyName("service")]
        public InstanceDeploymentService? Service { get; set; }

        /// <summary>
        /// Options to use on the created storage.
        /// </summary>
        [JsonPropertyName("persistence")]
        public InstanceDeploymentPersistence? Persistence { get; set; }

    }

}
