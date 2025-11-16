using System.Text.Json.Serialization;

using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.ApiKey
{

    public class V1alpha1ApiKeySpec : V1alpha1InstanceEntitySpec<ApiKeyConf>
    {

        [JsonPropertyName("instanceRef")]
        [Required]
        public V1alpha1InstanceReference? InstanceRef { get; set; }

        /// <summary>
        /// Reference to secret where the ApiKey will be read from or stored to.
        /// </summary>
        [JsonPropertyName("secretRef")]
        public V1SecretReference? SecretRef { get; set; }

        [JsonPropertyName("find")]
        public ApiKeyFind? Find { get; set; }

        [JsonPropertyName("conf")]
        [Required]
        public ApiKeyConf? Conf { get; set; }

    }

}
