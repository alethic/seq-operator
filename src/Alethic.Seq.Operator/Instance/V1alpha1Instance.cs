using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// The deprecated <c>v1alpha1</c> version of <see cref="V1Instance"/>. It shares the spec and status types, so the two
    /// schemas are identical and the API server converts between them by relabelling <c>apiVersion</c> alone; no
    /// conversion webhook is needed. Exists only so the CustomResourceDefinition keeps serving <c>v1alpha1</c> to
    /// existing clients: the operator works with <see cref="V1Instance"/>, which is also the storage version.
    /// </summary>
    /// <remarks>
    /// Never give this type properties or attributes that change its schema. A schema that differs from v1 would
    /// need a conversion webhook, and objects written through one version would lose fields when read through the other.
    /// </remarks>
    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1alpha1", Kind = "Instance")]
    [KubernetesEntityShortNames("seqinstance")]
    public class V1alpha1Instance : CustomKubernetesEntity<V1InstanceSpec, V1InstanceStatus>
    {

    }

}
