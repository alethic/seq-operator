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

        [JsonPropertyName("setPublic")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SetPublic { get; set; }

        [JsonPropertyName("setIngest")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SetIngest { get; set; }

        [JsonPropertyName("setRead")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SetRead { get; set; }

        [JsonPropertyName("setWrite")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SetWrite { get; set; }

        [JsonPropertyName("setProject")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SetProject { get; set; }

        [JsonPropertyName("setSystem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SetSystem { get; set; }

        [JsonPropertyName("setOrganization")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SetOrganization { get; set; }

    }

}
