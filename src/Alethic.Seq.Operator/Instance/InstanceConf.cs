using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    public partial class InstanceConf
    {

        [JsonPropertyName("settings")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InstanceConfSettings? Settings { get; set; }

    }

}
