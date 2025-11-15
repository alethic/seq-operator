using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    public class InstanceAlertPermissions
    {

        /// <summary>
        /// Allowed to create new alerts.
        /// </summary>
        [JsonPropertyName("create")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Create { get; set; }

        /// <summary>
        /// Allowed to attach to existing alerts.
        /// </summary>
        [JsonPropertyName("attach")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Attach { get; set; }

    }

}
