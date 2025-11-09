using System.Text.Json.Serialization;

using Alethic.Seq.Operator.Core.Models;
using Alethic.Seq.Operator.Core.Models.ApiKey;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Models
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1alpha1", Kind = "ApiKey")]
    [KubernetesEntityShortNames("seqapikey")]
    public partial class V1Alpha1ApiKey :
        CustomKubernetesEntity<V1Alpha1ApiKey.SpecDef, V1Alpha1ApiKey.StatusDef>,
        V1Alpha1InstanceEntity<V1Alpha1ApiKey.SpecDef, V1Alpha1ApiKey.StatusDef, ApiKeyConf, ApiKeyInfo>
    {

        public class SpecDef : V1Alpha1InstanceEntitySpec<ApiKeyConf>
        {

            [JsonPropertyName("policy")]
            public V1Alpha1EntityPolicyType[]? Policy { get; set; }

            [JsonPropertyName("instanceRef")]
            [Required]
            public V1Alpha1InstanceReference? InstanceRef { get; set; }

            /// <summary>
            /// Reference to secret where the ApiKey will be read from or stored to.
            /// </summary>
            [JsonPropertyName("secretRef")]
            public V1SecretReference? SecretRef { get; set; }

            [JsonPropertyName("find")]
            public ApiKeyFind? Find { get; set; }

            [JsonPropertyName("init")]
            public ApiKeyConf? Init { get; set; }

            [JsonPropertyName("conf")]
            [Required]
            public ApiKeyConf? Conf { get; set; }

        }

        public class StatusDef : V1Alpha1InstanceEntityStatus<ApiKeyInfo>
        {

            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("info")]
            public ApiKeyInfo? Info { get; set; }

        }

    }

}
