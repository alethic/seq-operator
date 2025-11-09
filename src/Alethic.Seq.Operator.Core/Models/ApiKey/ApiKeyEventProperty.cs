using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Models.ApiKey
{

    public class ApiKeyEventProperty
    {

        /// <summary>
        /// The property name (required).
        /// </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; }

        /// <summary>
        /// The property value, or <c>null</c>.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Value { get; }

    }

}