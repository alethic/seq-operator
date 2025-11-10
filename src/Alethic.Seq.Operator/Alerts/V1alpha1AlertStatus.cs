using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Alerts
{

    public class V1alpha1AlertStatus : V1alpha1InstanceEntityStatus<AlertInfo>
    {

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("info")]
        public AlertInfo? Info { get; set; }

        [JsonPropertyName("conditions")]
        public IList<V1alpha1Condition> Conditions { get; set; } = new List<V1alpha1Condition>();

    }

}
