using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Alerts
{

    public class AlertConf
    {

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

    }

}
