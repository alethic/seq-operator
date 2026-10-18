using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Signals
{

    public class V1SignalStatus : V1InstanceEntityStatus<SignalInfo>
    {

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("info")]
        public SignalInfo? Info { get; set; }

        [JsonPropertyName("conditions")]
        public IList<V1EntityCondition> Conditions { get; set; } = new List<V1EntityCondition>();

    }

}
