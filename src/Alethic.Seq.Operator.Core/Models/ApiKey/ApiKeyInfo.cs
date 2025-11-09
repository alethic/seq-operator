using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Models.ApiKey
{

    public partial class ApiKeyInfo
    {

        [JsonPropertyName("token")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Token { get; set; }

        [JsonPropertyName("tokenPrefix")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TokenPrefix { get; set; }

        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }

        [JsonPropertyName("isDefault")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsDefault { get; set; }

        [JsonPropertyName("ownerId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OwnerId { get; set; }

        [JsonPropertyName("assignedPermissions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiKeyPermission[]? AssignedPermissions { get; set; }

        [JsonPropertyName("inputSettings")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiKeyInputSettings? InputSettings { get; set; }

    }

}
