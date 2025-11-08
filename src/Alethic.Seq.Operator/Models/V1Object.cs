using System.Text.Json.Serialization;

using Alethic.Seq.Operator.Core.Models;
using Alethic.Seq.Operator.Core.Models.Object;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Models
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "k8s.seq.datalust.co", ApiVersion = "v1alpha1", Kind = "Object")]
    [KubernetesEntityShortNames("seqobject")]
    public partial class V1Object :
        CustomKubernetesEntity<V1Object.SpecDef, V1Object.StatusDef>,
        V1InstanceEntity<V1Object.SpecDef, V1Object.StatusDef, ObjectConf>
    {

        public class SpecDef : V1InstanceEntitySpec<ObjectConf>
        {

            [JsonPropertyName("policy")]
            public V1EntityPolicyType[]? Policy { get; set; }

            [JsonPropertyName("instanceRef")]
            [Required]
            public V1InstanceReference? InstanceRef { get; set; }

            [JsonPropertyName("find")]
            public ObjectFind? Find { get; set; }

            [JsonPropertyName("init")]
            public ObjectConf? Init { get; set; }

            [JsonPropertyName("conf")]
            [Required]
            public ObjectConf? Conf { get; set; }

        }

        public class StatusDef : V1InstanceEntityStatus
        {

            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("lastConf")]
            public string? LastConf { get; set; }

        }

    }

}
