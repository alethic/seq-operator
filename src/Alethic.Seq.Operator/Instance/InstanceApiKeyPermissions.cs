using System.Text.Json.Serialization;

using Alethic.Seq.Operator.ApiKey;

namespace Alethic.Seq.Operator.Instance
{

    public class InstanceApiKeyPermissions
    {

        /// <summary>
        /// Allowed to create new API keys.
        /// </summary>
        [JsonPropertyName("create")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Create { get; set; }

        /// <summary>
        /// Allowed to attach to existing API keys.
        /// </summary>
        [JsonPropertyName("attach")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Attach { get; set; }

        /// <summary>
        /// Allowed to set a custom title. If not allowed, only a generated title is permitted.
        /// </summary>
        [JsonPropertyName("setTitle")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SetTitle { get; set; }

        /// <summary>
        /// Allowed permissions of keys.
        /// </summary>
        [JsonPropertyName("allowedPermissions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiKeyPermission[]? AllowedPermissions { get; set; }

    }

}
