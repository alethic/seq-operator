using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    public class InstanceRemoteAuthenticationSpec
    {

        /// <summary>
        /// If specified, indicates operations should use token authentication.
        /// </summary>
        [JsonPropertyName("token")]
        public InstanceTokenRemoteAuthenticationSpec? Token { get; set; }

        /// <summary>
        /// If specified, indicates login by username/password should be attempted.
        /// </summary>
        [JsonPropertyName("login")]
        public InstanceLoginRemoteAuthenticationSpec? Login { get; set; }

    }

}
