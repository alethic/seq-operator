using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Alerts
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1", Kind = "Alert")]
    [StorageVersion]
    [KubernetesEntityShortNames("seqalert")]
    public partial class V1Alert :
        CustomKubernetesEntity<V1AlertSpec, V1AlertStatus>,
        V1InstanceEntity<V1AlertSpec, V1AlertStatus, AlertConf, AlertInfo>
    {

    }

}
