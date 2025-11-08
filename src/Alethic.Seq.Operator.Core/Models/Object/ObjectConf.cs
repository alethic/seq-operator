using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Models.Object
{

    public partial class ObjectConf
    {

        [JsonPropertyName("value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Value { get; set; }

    }

}
