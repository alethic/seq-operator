using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Models.Instance
{

    public class InstanceSettingsAuth
    {

        public class LocalAuth
        {



        }

        public class ActiveDirectoryAuth
        {

            [JsonPropertyName("automaticAccessADGroup")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? AutomaticAccessADGroup { get; set; }

        }

        public class EntraAuth
        {

            [JsonPropertyName("authority")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Authority { get; set; }

            [JsonPropertyName("clientId")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ClientId { get; set; }

            [JsonPropertyName("clientKey")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ClientKey { get; set; }

            [JsonPropertyName("tenantId")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? TenantId { get; set; }

        }

        public class OidcAuth
        {

            [JsonPropertyName("authority")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Authority { get; set; }

            [JsonPropertyName("clientId")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ClientId { get; set; }

            [JsonPropertyName("clientSecret")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ClientSecret { get; set; }

            [JsonPropertyName("scopes")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string[]? Scopes { get; set; }

            [JsonPropertyName("metadataAddress")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? MetadataAddress { get; set; }

        }

        [JsonPropertyName("local")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LocalAuth? Local { get; set; }

        [JsonPropertyName("activeDirectory")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ActiveDirectoryAuth? ActiveDirectory { get; set; }

        [JsonPropertyName("entra")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public EntraAuth? Entra { get; set; }

        [JsonPropertyName("oidc")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OidcAuth? Oidc { get; set; }

        [JsonPropertyName("automaticallyProvisionAuthenticatedUsers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? AutomaticallyProvisionAuthenticatedUsers { get; set; }

    }

}
