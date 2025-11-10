using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Models.Instance
{

    public partial class InstanceSettings
    {

        public class AuthConf
        {

            public class LocalAuthConf
            {



            }

            public class ActiveDirectoryAuthConf
            {

                [JsonPropertyName("automaticAccessADGroup")]
                [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
                public string? AutomaticAccessADGroup { get; set; }

            }

            public class EntraAuthConf
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

            public class OidcAuthConf
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
            public LocalAuthConf? Local { get; set; }

            [JsonPropertyName("activeDirectory")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public ActiveDirectoryAuthConf? ActiveDirectory { get; set; }

            [JsonPropertyName("entra")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public EntraAuthConf? Entra { get; set; }

            [JsonPropertyName("oidc")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public OidcAuthConf? Oidc { get; set; }

            [JsonPropertyName("automaticallyProvisionAuthenticatedUsers")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public bool? AutomaticallyProvisionAuthenticatedUsers { get; set; }

        }

        [JsonPropertyName("auth")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AuthConf? Auth { get; set; }

        [JsonPropertyName("dataAgeWarningThresholdMilliseconds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? DataAgeWarningThresholdMilliseconds { get; set; }

        [JsonPropertyName("backupLocation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BackupLocation { get; set; }

        [JsonPropertyName("backupsToKeep")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? BackupsToKeep { get; set; }

        [JsonPropertyName("backupUtcTimeOfDay")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BackupUtcTimeOfDay { get; set; }

        [JsonPropertyName("checkForPackageUpdates")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? CheckForPackageUpdates { get; set; }

        [JsonPropertyName("checkForUpdates")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? CheckForUpdates { get; set; }

        [JsonPropertyName("instanceTitle")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? InstanceTitle { get; set; }

        [JsonPropertyName("minimumFreeStorageSpace")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? MinimumFreeStorageSpace { get; set; }

        [JsonPropertyName("newUserPreferences")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? NewUserPreferences { get; set; }

        [JsonPropertyName("newUserRoleIds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? NewUserRoleIds { get; set; }

        [JsonPropertyName("newUserShowSignalIds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? NewUserShowSignalIds { get; set; }

        [JsonPropertyName("newUserShowQueryIds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? NewUserShowQueryIds { get; set; }

        [JsonPropertyName("newUserShowDashboardIds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? NewUserShowDashboardIds { get; set; }

        [JsonPropertyName("requireApiKeyForWritingEvents")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? RequireApiKeyForWritingEvents { get; set; }

        [JsonPropertyName("rawEventMaximumContentLength")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? RawEventMaximumContentLength { get; set; }

        [JsonPropertyName("rawPayloadMaximumContentLength")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? RawPayloadMaximumContentLength { get; set; }

        [JsonPropertyName("targetReplicaCount")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? TargetReplicaCount { get; set; }

        [JsonPropertyName("themeStyles")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ThemeStyles { get; set; }

    }

}
