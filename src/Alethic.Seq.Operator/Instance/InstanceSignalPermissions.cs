using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    public class InstanceSignalPermissions
    {

        /// <summary>
        /// Allowed to create new signals.
        /// </summary>
        [JsonPropertyName("create")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Create { get; set; }

        /// <summary>
        /// Allowed to attach to existing signals.
        /// </summary>
        [JsonPropertyName("attach")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Attach { get; set; }

    }

}
