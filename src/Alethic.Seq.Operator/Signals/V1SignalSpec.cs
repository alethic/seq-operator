using System.Text.Json.Serialization;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Signals
{

    public class V1SignalSpec : V1InstanceEntitySpec<SignalConf>
    {

        [JsonPropertyName("instanceRef")]
        [Required]
        public V1InstanceReference? InstanceRef { get; set; }

        [JsonPropertyName("conf")]
        [Required]
        public SignalConf? Conf { get; set; }

    }

}
