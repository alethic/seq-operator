using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Alerts
{

    public class AlertSelectColumn
    {

        [JsonPropertyName("label")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Label { get; set; }

        [JsonPropertyName("value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Value { get; set; }

    }

}
