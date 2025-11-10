using System.Text.Json.Serialization;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Instance
{

    public partial class V1alpha1InstanceSpec : V1alpha1EntitySpec<InstanceConf>
    {

        /// <summary>
        /// Describes the permitted operations on the entity.
        /// </summary>
        [JsonPropertyName("policy")]
        public V1alpha1EntityPolicyType[]? Policy { get; set; }

        /// <summary>
        /// Connection information for the instance.
        /// </summary>
        [JsonPropertyName("connections")]
        [Required]
        public InstanceConnection[]? Connections { get; set; }

        /// <summary>
        /// Connection information for the instance.
        /// </summary>
        [JsonPropertyName("deployment")]
        public InstanceDeploymentSpec? Deployment { get; set; }

        /// <summary>
        /// Configuration to apply when initializing the entity for the first time.
        /// </summary>
        [JsonPropertyName("init")]
        public InstanceConf? Init { get; set; }

        /// <summary>
        /// Configuration to apply when reconcilling the entity.
        /// </summary>
        [JsonPropertyName("conf")]
        [Required]
        public InstanceConf? Conf { get; set; }

    }

}
