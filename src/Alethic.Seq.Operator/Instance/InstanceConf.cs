using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    public class InstanceConf
    {

        [JsonPropertyName("auth")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InstanceAuthenticationSpec? Auth { get; set; }

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

        //[JsonPropertyName("newUserPreferences")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public Dictionary<string, string>? NewUserPreferences { get; set; }

        //[JsonPropertyName("newUserRoleIds")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public string[]? NewUserRoleIds { get; set; }

        //[JsonPropertyName("newUserShowSignalIds")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public string[]? NewUserShowSignalIds { get; set; }

        //[JsonPropertyName("newUserShowQueryIds")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public string[]? NewUserShowQueryIds { get; set; }

        //[JsonPropertyName("newUserShowDashboardIds")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public string[]? NewUserShowDashboardIds { get; set; }

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
