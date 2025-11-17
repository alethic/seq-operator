using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Signals
{

    public class SignalConf
    {

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

    }

}
