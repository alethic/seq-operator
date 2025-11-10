using System.Text.Json.Serialization;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Alerts
{

    public class V1alpha1AlertSpec : V1alpha1InstanceEntitySpec<AlertConf>
    {

        [JsonPropertyName("policy")]
        public V1alpha1EntityPolicyType[]? Policy { get; set; }

        [JsonPropertyName("instanceRef")]
        [Required]
        public V1alpha1InstanceReference? InstanceRef { get; set; }

        [JsonPropertyName("find")]
        public AlertFind? Find { get; set; }

        [JsonPropertyName("init")]
        public AlertConf? Init { get; set; }

        [JsonPropertyName("conf")]
        [Required]
        public AlertConf? Conf { get; set; }

    }

}
