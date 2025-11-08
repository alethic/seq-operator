using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Models.Instance
{

    public partial class InstanceConf
    {

        [JsonPropertyName("value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Value { get; set; }

    }

}
