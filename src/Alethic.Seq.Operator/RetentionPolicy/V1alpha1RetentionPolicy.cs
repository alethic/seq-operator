using Alethic.Seq.Operator.RetentionPolicy;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Models
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1alpha1", Kind = "RetentionPolicy")]
    [KubernetesEntityShortNames("seqretentionpolicy")]
    public partial class V1alpha1RetentionPolicy :
        CustomKubernetesEntity<V1alpha1RetentionPolicySpec, V1alpha1RetentionPolicyStatus>,
        V1alpha1InstanceEntity<V1alpha1RetentionPolicySpec, V1alpha1RetentionPolicyStatus, RetentionPolicyConf, RetentionPolicyInfo>
    {

    }

}
