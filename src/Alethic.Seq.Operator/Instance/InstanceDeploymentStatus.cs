using System.Text.Json.Serialization;

using k8s.Models;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes how to deploy a new instance of Seq.
    /// </summary>
    public class InstanceDeploymentStatus
    {

        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; set; }

        [JsonPropertyName("adminSecretRef")]
        public V1SecretReference? AdminSecretRef { get; set; }

        [JsonPropertyName("TokenSecretRef")]
        public V1SecretReference? TokenSecretRef { get; set; }

    }

}
