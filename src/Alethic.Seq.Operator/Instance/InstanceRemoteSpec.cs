using System.Text.Json.Serialization;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes connection information to reach a remote Seq instance.
    /// </summary>
    public class InstanceRemoteSpec
    {

        /// <summary>
        /// Endpoint of the Seq instance in URI format.
        /// </summary>
        [JsonPropertyName("endpoint")]
        [Required]
        public string? Endpoint { get; set; }

        /// <summary>
        /// If specified, indicates a set of authentication methods that should be attempted in order.
        /// </summary>
        [JsonPropertyName("auth")]
        [Required]
        public InstanceRemoteAuthenticationSpec[]? Authentication { get; set; }

    }

}
