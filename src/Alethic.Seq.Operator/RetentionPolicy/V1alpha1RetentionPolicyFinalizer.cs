using System;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Models;

using KubeOps.Abstractions.Finalizer;

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
        public async Task FinalizeAsync(V1alpha1RetentionPolicy entity, CancellationToken cancellationToken)
        {
            await _controller.DeletedAsync(entity, cancellationToken);
        }

    }

}
