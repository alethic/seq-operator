using System.Text.Json.Serialization;

using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes information to authenticate with Seq using a username and password.
    /// </summary>
    public class InstanceLoginAuthenticationSpec
    {

        [JsonPropertyName("secretRef")]
        [Required]
        public V1SecretReference? SecretRef { get; set; }

    }

}
