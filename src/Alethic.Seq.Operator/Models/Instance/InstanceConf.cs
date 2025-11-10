using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Models.Instance
{

    public partial class InstanceConf
    {

        [JsonPropertyName("settings")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InstanceConfSettings? Settings { get; set; }

    }

}
