using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Signals
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1alpha1", Kind = "Signal")]
    [KubernetesEntityShortNames("seqsignal")]
    public partial class V1alpha1Signal :
        CustomKubernetesEntity<V1alpha1SignalSpec, V1alpha1SignalStatus>,
        V1alpha1InstanceEntity<V1alpha1SignalSpec, V1alpha1SignalStatus, SignalConf, SignalInfo>
    {

    }

}
