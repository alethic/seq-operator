using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Models.Instance
{

    public partial class InstanceInfo
    {

        [JsonPropertyName("settings")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InstanceSettings? Settings { get; set; }

    }

}
