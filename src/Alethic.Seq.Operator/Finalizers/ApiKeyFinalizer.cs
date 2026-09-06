using Alethic.Seq.Operator.ApiKey;

using KubeOps.Abstractions.Reconciliation.Controller;

namespace Alethic.Seq.Operator.Finalizers
{

    /// <summary>
    /// Finalizes <see cref="V1alpha1ApiKey"/>, writing <c>seq.k8s.datalust.co/apikeyfinalizer</c> into
    /// <c>metadata.finalizers</c>. The name carries no API version deliberately; see
    /// <see cref="EntityFinalizers"/>.
    /// </summary>
    public class ApiKeyFinalizer : EntityFinalizerBase<V1alpha1ApiKey>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public ApiKeyFinalizer(IEntityController<V1alpha1ApiKey> controller) :
            base(controller)
        {

        }

    }

}
