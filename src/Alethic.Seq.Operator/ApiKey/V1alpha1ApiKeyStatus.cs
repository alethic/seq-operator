using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.ApiKey
{

    public class V1alpha1ApiKeyStatus : V1alpha1InstanceEntityStatus<ApiKeyInfo>
    {

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("info")]
        public ApiKeyInfo? Info { get; set; }

        [JsonPropertyName("conditions")]
        public IList<V1alpha1Condition> Conditions { get; set; } = new List<V1alpha1Condition>();

    }

}
