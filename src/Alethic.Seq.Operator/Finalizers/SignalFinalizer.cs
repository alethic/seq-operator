using Alethic.Seq.Operator.Signals;

using KubeOps.Abstractions.Reconciliation.Controller;

namespace Alethic.Seq.Operator.Finalizers
{

    /// <summary>
    /// Finalizes <see cref="V1alpha1Signal"/>, writing <c>seq.k8s.datalust.co/signalfinalizer</c> into
    /// <c>metadata.finalizers</c>. The name carries no API version deliberately; see
    /// <see cref="EntityFinalizers"/>.
    /// </summary>
    public class SignalFinalizer : EntityFinalizerBase<V1alpha1Signal>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public SignalFinalizer(IEntityController<V1alpha1Signal> controller) :
            base(controller)
        {

        }

    }

}
