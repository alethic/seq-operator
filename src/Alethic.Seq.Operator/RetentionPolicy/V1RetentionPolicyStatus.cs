using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.RetentionPolicy
{

    public class V1RetentionPolicyStatus : V1InstanceEntityStatus<RetentionPolicyInfo>
    {

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("info")]
        public RetentionPolicyInfo? Info { get; set; }

        [JsonPropertyName("conditions")]
        public IList<V1EntityCondition> Conditions { get; set; } = new List<V1EntityCondition>();

    }

}
