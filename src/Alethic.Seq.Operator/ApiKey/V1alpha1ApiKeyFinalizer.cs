using System;
using System.Threading;
using System.Threading.Tasks;

using KubeOps.Abstractions.Finalizer;

namespace Alethic.Seq.Operator.ApiKey
{

    public class V1alpha1ApiKeyFinalizer : IEntityFinalizer<V1alpha1ApiKey>
    {

        readonly V1alpha1ApiKeyController _controller;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1alpha1ApiKeyFinalizer(V1alpha1ApiKeyController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <inheritdoc />
        public async Task FinalizeAsync(V1alpha1ApiKey entity, CancellationToken cancellationToken)
        {
            await _controller.DeletedAsync(entity, cancellationToken);
        }

    }

}
