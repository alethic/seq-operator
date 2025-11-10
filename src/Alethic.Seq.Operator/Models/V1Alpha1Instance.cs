using System.Collections.Generic;
using System.Text.Json.Serialization;

using Alethic.Seq.Operator.Models.Instance;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Models
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1alpha1", Kind = "Instance")]
    [KubernetesEntityShortNames("seqinstance")]
    public partial class V1Alpha1Instance :
        CustomKubernetesEntity<V1Alpha1Instance.SpecDef, V1Alpha1Instance.StatusDef>,
        V1Alpha1Entity<V1Alpha1Instance.SpecDef, V1Alpha1Instance.StatusDef, InstanceConf, InstanceInfo>
    {

        public partial class SpecDef : V1Alpha1EntitySpec<InstanceConf>
        {

            /// <summary>
            /// Describes the permitted operations on the entity.
            /// </summary>
            [JsonPropertyName("policy")]
            public V1Alpha1EntityPolicyType[]? Policy { get; set; }

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
            public InstanceDeployment? Deployment { get; set; }

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

        public class StatusDef : V1Alpha1EntityStatus<InstanceInfo>
        {

            /// <summary>
            /// Describes the managed deployment.
            /// </summary>
            public class DeploymentStatus
            {



            }

            /// <summary>
            /// Information about the associated deployment.
            /// </summary>
            [JsonPropertyName("deployment")]
            public DeploymentStatus? Deployment { get; set; }

            [JsonPropertyName("info")]
            public InstanceInfo? Info { get; set; }

            [JsonPropertyName("conditions")]
            public IList<V1Alpha1Condition> Conditions { get; set; } = new List<V1Alpha1Condition>();

        }

    }

}
