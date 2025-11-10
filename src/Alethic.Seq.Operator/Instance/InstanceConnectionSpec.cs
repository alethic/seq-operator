using System.Text.Json.Serialization;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes connection information to reach a remote Seq instance.
    /// </summary>
    public class InstanceConnectionSpec
    {

        /// <summary>
        /// Endpoint of the Seq instance in URI format.
        /// </summary>
        [JsonPropertyName("endpoint")]
        [Required]
        public string? Endpoint { get; set; }

        /// <summary>
        /// If specified, indicates operations should use token authentication.
        /// </summary>
        [JsonPropertyName("token")]
        public InstanceTokenAuthenticationSpec? Token { get; set; }

        /// <summary>
        /// If specified, indicates login by username/password should be attempted.
        /// </summary>
        [JsonPropertyName("login")]
        public InstanceLoginAuthenticationSpec? Login { get; set; }

    }

}
