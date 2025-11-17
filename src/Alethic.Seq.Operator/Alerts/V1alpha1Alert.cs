using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Alerts
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1alpha1", Kind = "Alert")]
    [KubernetesEntityShortNames("seqalert")]
    public partial class V1alpha1Alert :
        CustomKubernetesEntity<V1alpha1AlertSpec, V1alpha1AlertStatus>,
        V1alpha1InstanceEntity<V1alpha1AlertSpec, V1alpha1AlertStatus, AlertConf, AlertInfo>
    {

    }

}
