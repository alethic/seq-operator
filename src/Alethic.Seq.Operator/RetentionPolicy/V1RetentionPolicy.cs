using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.RetentionPolicy
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1", Kind = "RetentionPolicy")]
    [StorageVersion]
    [KubernetesEntityShortNames("seqretentionpolicy")]
    public partial class V1RetentionPolicy :
        CustomKubernetesEntity<V1RetentionPolicySpec, V1RetentionPolicyStatus>,
        V1InstanceEntity<V1RetentionPolicySpec, V1RetentionPolicyStatus, RetentionPolicyConf, RetentionPolicyInfo>
    {

    }

}
