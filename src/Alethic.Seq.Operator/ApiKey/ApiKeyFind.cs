using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.ApiKey
{

    public class ApiKeyFind
    {

        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }

    }

}
