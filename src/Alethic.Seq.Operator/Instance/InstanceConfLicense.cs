using System.Text.Json.Serialization;

using k8s.Models;

namespace Alethic.Seq.Operator.Instance
{

    public class InstanceConfLicense
    {

        [JsonPropertyName("secretKeyRef")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public V1SecretKeySelector? SecretKeyRef { get; set; }

        [JsonPropertyName("automaticallyRefresh")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? AutomaticallyRefresh { get; set; }

    }

}
