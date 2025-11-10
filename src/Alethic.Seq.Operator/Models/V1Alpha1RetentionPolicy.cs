using System.Collections.Generic;
using System.Text.Json.Serialization;

using Alethic.Seq.Operator.Core.Models;
using Alethic.Seq.Operator.Models.RetentionPolicy;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Models
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1alpha1", Kind = "RetentionPolicy")]
    [KubernetesEntityShortNames("seqretentionpolicy")]
    public partial class V1Alpha1RetentionPolicy :
        CustomKubernetesEntity<V1Alpha1RetentionPolicy.SpecDef, V1Alpha1RetentionPolicy.StatusDef>,
        V1Alpha1InstanceEntity<V1Alpha1RetentionPolicy.SpecDef, V1Alpha1RetentionPolicy.StatusDef, RetentionPolicyConf, RetentionPolicyInfo>
    {

        public class SpecDef : V1Alpha1InstanceEntitySpec<RetentionPolicyConf>
        {

            [JsonPropertyName("policy")]
            public V1Alpha1EntityPolicyType[]? Policy { get; set; }

            [JsonPropertyName("instanceRef")]
            [Required]
            public V1Alpha1InstanceReference? InstanceRef { get; set; }

            [JsonPropertyName("init")]
            public RetentionPolicyConf? Init { get; set; }

            [JsonPropertyName("conf")]
            [Required]
            public RetentionPolicyConf? Conf { get; set; }

        }

        public class StatusDef : V1Alpha1InstanceEntityStatus<RetentionPolicyInfo>
        {

            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("info")]
            public RetentionPolicyInfo? Info { get; set; }

            [JsonPropertyName("conditions")]
            public IList<V1Alpha1Condition> Conditions { get; set; } = new List<V1Alpha1Condition>();

        }

    }

}
