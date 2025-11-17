using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Signals
{

    public class V1alpha1SignalStatus : V1alpha1InstanceEntityStatus<SignalInfo>
    {

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("info")]
        public SignalInfo? Info { get; set; }

        [JsonPropertyName("conditions")]
        public IList<V1alpha1Condition> Conditions { get; set; } = new List<V1alpha1Condition>();

    }

}
