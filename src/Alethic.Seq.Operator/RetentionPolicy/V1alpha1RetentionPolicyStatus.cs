using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.RetentionPolicy
{

    public class V1alpha1RetentionPolicyStatus : V1alpha1InstanceEntityStatus<RetentionPolicyInfo>
    {

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("info")]
        public RetentionPolicyInfo? Info { get; set; }

        [JsonPropertyName("conditions")]
        public IList<V1alpha1Condition> Conditions { get; set; } = new List<V1alpha1Condition>();

    }

}
