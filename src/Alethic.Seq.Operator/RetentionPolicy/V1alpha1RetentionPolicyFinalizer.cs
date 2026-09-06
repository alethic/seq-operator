using System;
using System.Threading;
using System.Threading.Tasks;

using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Finalizer;

namespace Alethic.Seq.Operator.RetentionPolicy
{

    public class V1alpha1RetentionPolicyFinalizer : IEntityFinalizer<V1alpha1RetentionPolicy>
    {

        readonly V1alpha1RetentionPolicyController _controller;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1alpha1RetentionPolicyFinalizer(V1alpha1RetentionPolicyController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <inheritdoc />
        public async Task<ReconciliationResult<V1alpha1RetentionPolicy>> FinalizeAsync(V1alpha1RetentionPolicy entity, CancellationToken cancellationToken)
        {
            return await _controller.DeletedAsync(entity, cancellationToken);
        }

    }

}
