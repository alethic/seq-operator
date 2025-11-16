using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    public partial class V1alpha1InstanceSpec : V1alpha1EntitySpec<InstanceConf>
    {

        /// <summary>
        /// Connection information for a remote instance.
        /// </summary>
        [JsonPropertyName("remote")]
        public InstanceRemoteSpec? Remote { get; set; }

        /// <summary>
        /// Connection information for the instance.
        /// </summary>
        [JsonPropertyName("deployment")]
        public InstanceDeploymentSpec? Deployment { get; set; }

        /// <summary>
        /// Permissions to apply on access to the instance. The first permission entry that matches the entity is applied.
        /// </summary>
        [JsonPropertyName("permissions")]
        public InstancePermission[]? Permissions { get; set; }

        /// <summary>
        /// Configuration to apply when reconcilling the entity.
        /// </summary>
        [JsonPropertyName("conf")]
        public InstanceConf? Conf { get; set; }

    }

}
