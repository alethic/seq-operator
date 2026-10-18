using System.Text.Json.Serialization;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.RetentionPolicy
{

    public class V1RetentionPolicySpec : V1InstanceEntitySpec<RetentionPolicyConf>
    {

        [JsonPropertyName("instanceRef")]
        [Required]
        public V1InstanceReference? InstanceRef { get; set; }

        [JsonPropertyName("conf")]
        [Required]
        public RetentionPolicyConf? Conf { get; set; }

    }

}
