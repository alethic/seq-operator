using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.ApiKey
{

    public partial class ApiKeyConf
    {

        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }

        [JsonPropertyName("permissions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiKeyPermission[]? Permissions { get; set; }

        [JsonPropertyName("inputSettings")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiKeyInputSettings? InputSettings { get; set; }

    }

}
