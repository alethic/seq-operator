using Alethic.Seq.Operator.Alerts;

using KubeOps.Abstractions.Reconciliation.Controller;

namespace Alethic.Seq.Operator.Finalizers
{

    /// <summary>
    /// Finalizes <see cref="V1alpha1Alert"/>, writing <c>seq.k8s.datalust.co/alertfinalizer</c> into
    /// <c>metadata.finalizers</c>. The name carries no API version deliberately; see
    /// <see cref="EntityFinalizers"/>.
    /// </summary>
    public class AlertFinalizer : EntityFinalizerBase<V1alpha1Alert>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public AlertFinalizer(IEntityController<V1alpha1Alert> controller) :
            base(controller)
        {

        }

    }

}
