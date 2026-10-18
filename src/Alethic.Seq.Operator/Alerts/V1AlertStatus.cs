using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Alerts
{

    public class V1AlertStatus : V1InstanceEntityStatus<AlertInfo>
    {

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("info")]
        public AlertInfo? Info { get; set; }

        [JsonPropertyName("conditions")]
        public IList<V1EntityCondition> Conditions { get; set; } = new List<V1EntityCondition>();

    }

}
