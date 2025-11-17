using System.Text.Json.Serialization;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Signals
{

    public class V1alpha1SignalSpec : V1alpha1InstanceEntitySpec<SignalConf>
    {

        [JsonPropertyName("instanceRef")]
        [Required]
        public V1alpha1InstanceReference? InstanceRef { get; set; }

        [JsonPropertyName("conf")]
        [Required]
        public SignalConf? Conf { get; set; }

    }

}
