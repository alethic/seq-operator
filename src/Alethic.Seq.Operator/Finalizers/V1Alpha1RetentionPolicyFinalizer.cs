using System;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Controllers;
using Alethic.Seq.Operator.Models;

using KubeOps.Abstractions.Finalizer;

namespace Alethic.Seq.Operator.Finalizers
{

    public class V1Alpha1RetentionPolicyFinalizer : IEntityFinalizer<V1Alpha1RetentionPolicy>
    {

        readonly V1Alpha1RetentionPolicyController _controller;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1Alpha1RetentionPolicyFinalizer(V1Alpha1RetentionPolicyController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <inheritdoc />
        public async Task FinalizeAsync(V1Alpha1RetentionPolicy entity, CancellationToken cancellationToken)
        {
            await _controller.DeletedAsync(entity, cancellationToken);
        }

    }

}
