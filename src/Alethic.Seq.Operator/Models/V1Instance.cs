using System.Text.Json.Serialization;

using Alethic.Seq.Operator.Core.Models.Instance;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Models
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "k8s.seq.datalust.co", ApiVersion = "v1alpha1", Kind = "Instance")]
    [KubernetesEntityShortNames("seqinstance")]
    public partial class V1Instance :
        CustomKubernetesEntity<V1Instance.SpecDef, V1Instance.StatusDef>,
        V1Entity<V1Instance.SpecDef, V1Instance.StatusDef, InstanceConf>
    {

        public class SpecDef : V1EntitySpec<InstanceConf>
        {

            public class AuthDef
            {

                [JsonPropertyName("unknown")]
                [Required]
                public string? Unknown { get; set; }

                [JsonPropertyName("secretRef")]
                [Required]
                public V1SecretReference? SecretRef { get; set; }

            }

            [JsonPropertyName("policy")]
            public V1EntityPolicyType[]? Policy { get; set; }

            [JsonPropertyName("name")]
            [Required]
            public string Name { get; set; } = "";

            [JsonPropertyName("auth")]
            [Required]
            public AuthDef? Auth { get; set; }

            [JsonPropertyName("init")]
            public InstanceConf? Init { get; set; }

            [JsonPropertyName("conf")]
            [Required]
            public InstanceConf? Conf { get; set; }

        }

        public class StatusDef : V1EntityStatus
        {

            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("lastConf")]
            public string? LastConf { get; set; }

        }

    }

}
