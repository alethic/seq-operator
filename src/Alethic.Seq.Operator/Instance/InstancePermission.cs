using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Represents a set of entity permissions applied to a type of namespace.
    /// </summary>
    public class InstancePermission
    {

        /// <summary>
        /// Namespaces to be applied to.
        /// </summary>
        [JsonPropertyName("namespaces")]
        public InstancePermissionNamespaces? Namespaces { get; set; }

        /// <summary>
        /// Permissions to apply to alerts.
        /// </summary>
        [JsonPropertyName("alerts")]
        public InstanceAlertPermissions? Alerts { get; set; }

        /// <summary>
        /// Permissions to apply to API keys.
        /// </summary>
        [JsonPropertyName("apiKeys")]
        public InstanceApiKeyPermissions? ApiKeys { get; set; }

        /// <summary>
        /// Permissions to apply to retention policies.
        /// </summary>
        [JsonPropertyName("retentionPolicies")]
        public InstanceRetentionPolicyPermissions? RetentionPolicies { get; set; }

    }

}
