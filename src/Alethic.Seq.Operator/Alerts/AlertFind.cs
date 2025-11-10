using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Alerts
{

    public class AlertFind
    {

        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }

        [JsonPropertyName("ownerId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OwnerId { get; set; }

    }

}
