using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Models.Instance
{

    public partial class InstanceConf
    {

        [JsonPropertyName("settings")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InstanceSettings? Settings { get; set; }

    }

}
