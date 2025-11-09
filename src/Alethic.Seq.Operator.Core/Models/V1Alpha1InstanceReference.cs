using System.Text.Json.Serialization;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Core.Models
{

    public class V1Alpha1InstanceReference
    {

        [JsonPropertyName("namespace")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Namespace { get; set; }

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [Required]
        public string? Name { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Namespace}/{Name}";
        }

    }

}
