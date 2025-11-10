using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Alerts
{

    public class AlertConf
    {

        [JsonPropertyName("title")]
        public string? Title { get; set; }

    }

}
