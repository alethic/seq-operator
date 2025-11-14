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

        [JsonPropertyName("loginSecretRef")]
        public V1SecretReference? LoginSecretRef { get; set; }

        [JsonPropertyName("tokenSecretRef")]
        public V1SecretReference? TokenSecretRef { get; set; }

    }

}
