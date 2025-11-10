using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Instance
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1alpha1", Kind = "Instance")]
    [KubernetesEntityShortNames("seqinstance")]
    public partial class V1alpha1Instance :
        CustomKubernetesEntity<V1alpha1InstanceSpec, V1alpha1InstanceStatus>,
        V1alpha1Entity<V1alpha1InstanceSpec, V1alpha1InstanceStatus, InstanceConf, InstanceInfo>
    {

    }

}
