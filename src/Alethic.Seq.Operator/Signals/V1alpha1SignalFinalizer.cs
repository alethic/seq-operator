using System;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.RetentionPolicy;

using KubeOps.Abstractions.Finalizer;

namespace Alethic.Seq.Operator.Signals
{

    public class V1alpha1SignalFinalizer : IEntityFinalizer<V1alpha1Signal>
    {

        readonly V1alpha1SignalController _controller;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1alpha1SignalFinalizer(V1alpha1SignalController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <inheritdoc />
        public async Task FinalizeAsync(V1alpha1Signal entity, CancellationToken cancellationToken)
        {
            await _controller.DeletedAsync(entity, cancellationToken);
        }

    }

}
