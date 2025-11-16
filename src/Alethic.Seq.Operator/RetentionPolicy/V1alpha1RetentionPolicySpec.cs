using System.Text.Json.Serialization;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.RetentionPolicy
{

    public class V1alpha1RetentionPolicySpec : V1alpha1InstanceEntitySpec<RetentionPolicyConf>
    {

        [JsonPropertyName("instanceRef")]
        [Required]
        public V1alpha1InstanceReference? InstanceRef { get; set; }

        [JsonPropertyName("conf")]
        [Required]
        public RetentionPolicyConf? Conf { get; set; }

    }

}
