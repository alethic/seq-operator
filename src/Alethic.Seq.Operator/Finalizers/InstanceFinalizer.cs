using Alethic.Seq.Operator.Instance;

using KubeOps.Abstractions.Reconciliation.Controller;

namespace Alethic.Seq.Operator.Finalizers
{

    /// <summary>
    /// Finalizes <see cref="V1Instance"/>, writing <c>seq.k8s.datalust.co/instancefinalizer</c> into
    /// <c>metadata.finalizers</c>. The name carries no API version deliberately; see
    /// <see cref="EntityFinalizers"/>.
    /// </summary>
    public class InstanceFinalizer : EntityFinalizerBase<V1Instance>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public InstanceFinalizer(IEntityController<V1Instance> controller) :
            base(controller)
        {

        }

    }

}
