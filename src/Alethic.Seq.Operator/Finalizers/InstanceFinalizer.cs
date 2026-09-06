using Alethic.Seq.Operator.Instance;

using KubeOps.Abstractions.Reconciliation.Controller;

namespace Alethic.Seq.Operator.Finalizers
{

    /// <summary>
    /// Finalizes <see cref="V1alpha1Instance"/>, writing <c>seq.k8s.datalust.co/instancefinalizer</c> into
    /// <c>metadata.finalizers</c>. The name carries no API version deliberately; see
    /// <see cref="EntityFinalizers"/>.
    /// </summary>
    public class InstanceFinalizer : EntityFinalizerBase<V1alpha1Instance>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public InstanceFinalizer(IEntityController<V1alpha1Instance> controller) :
            base(controller)
        {

        }

    }

}
