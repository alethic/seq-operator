using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Signals
{

    public class SignalInfo
    {

        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        [JsonPropertyName("explicitGroupName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ExplicitGroupName { get; set; }

        [JsonPropertyName("isIndexSuppressed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsIndexSuppressed { get; set; }

        [JsonPropertyName("isProtected")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsProtected { get; set; }

    }

}
