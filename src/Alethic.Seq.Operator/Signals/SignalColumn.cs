using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Signals
{

    public class SignalColumn
    {

        [JsonPropertyName("expression")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Expression { get; set; }

    }

}
