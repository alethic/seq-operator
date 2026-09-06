using Alethic.Seq.Operator.RetentionPolicy;

using KubeOps.Abstractions.Reconciliation.Controller;

namespace Alethic.Seq.Operator.Finalizers
{

    /// <summary>
    /// Finalizes <see cref="V1alpha1RetentionPolicy"/>, writing <c>seq.k8s.datalust.co/retentionpolicyfinalizer</c> into
    /// <c>metadata.finalizers</c>. The name carries no API version deliberately; see
    /// <see cref="EntityFinalizers"/>.
    /// </summary>
    public class RetentionPolicyFinalizer : EntityFinalizerBase<V1alpha1RetentionPolicy>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public RetentionPolicyFinalizer(IEntityController<V1alpha1RetentionPolicy> controller) :
            base(controller)
        {

        }

    }

}
