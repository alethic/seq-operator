using System;
using System.Threading;
using System.Threading.Tasks;

using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Finalizer;

namespace Alethic.Seq.Operator.Alerts
{

    public class V1alpha1AlertFinalizer : IEntityFinalizer<V1alpha1Alert>
    {

        readonly V1alpha1AlertController _controller;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1alpha1AlertFinalizer(V1alpha1AlertController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <inheritdoc />
        public async Task<ReconciliationResult<V1alpha1Alert>> FinalizeAsync(V1alpha1Alert entity, CancellationToken cancellationToken)
        {
            return await _controller.DeletedAsync(entity, cancellationToken);
        }

    }

}
