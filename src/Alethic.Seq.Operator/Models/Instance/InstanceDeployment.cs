using System.Text.Json.Serialization;

using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Models.Instance
{

    /// <summary>
    /// Describes how to deploy a new instance of Seq.
    /// </summary>
    public class InstanceDeployment
    {

        /// <summary>
        /// Secret to use for the management API key.
        /// </summary>
        [JsonPropertyName("secretRef")]
        [Required]
        public V1SecretReference? SecretRef { get; set; }

    }

}
