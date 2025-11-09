using System;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Controllers;
using Alethic.Seq.Operator.Models;

using KubeOps.Abstractions.Finalizer;

namespace Alethic.Seq.Operator.Finalizers
{

    public class V1Alpha1ApiKeyFinalizer : IEntityFinalizer<V1Alpha1ApiKey>
    {

        readonly V1Alpha1ApiKeyController _controller;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1Alpha1ApiKeyFinalizer(V1Alpha1ApiKeyController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <inheritdoc />
        public async Task FinalizeAsync(V1Alpha1ApiKey entity, CancellationToken cancellationToken)
        {
            await _controller.DeletedAsync(entity, cancellationToken);
        }

    }

}
