using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    public class InstanceRetentionPolicyPermissions
    {

        /// <summary>
        /// Allowed to create new retention policies.
        /// </summary>
        [JsonPropertyName("create")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Create { get; set; }

        /// <summary>
        /// Allowed to attach to existing retention policies.
        /// </summary>
        [JsonPropertyName("attach")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Attach { get; set; }

    }

}
