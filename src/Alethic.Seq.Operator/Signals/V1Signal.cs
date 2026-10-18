using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Signals
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1", Kind = "Signal")]
    [StorageVersion]
    [KubernetesEntityShortNames("seqsignal")]
    public partial class V1Signal :
        CustomKubernetesEntity<V1SignalSpec, V1SignalStatus>,
        V1InstanceEntity<V1SignalSpec, V1SignalStatus, SignalConf, SignalInfo>
    {

    }

}
