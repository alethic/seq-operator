using System.Text.Json.Serialization;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Alerts
{

    public class V1AlertSpec : V1InstanceEntitySpec<AlertConf>
    {

        [JsonPropertyName("instanceRef")]
        [Required]
        public V1InstanceReference? InstanceRef { get; set; }

        [JsonPropertyName("conf")]
        [Required]
        public AlertConf? Conf { get; set; }

    }

}
