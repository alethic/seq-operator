using System;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Controllers;
using Alethic.Seq.Operator.Models;

using KubeOps.Abstractions.Finalizer;

namespace Alethic.Seq.Operator.Finalizers
{

    public class V1ClientFinalizer : IEntityFinalizer<V1Object>
    {

        readonly V1ObjectController _controller;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1ClientFinalizer(V1ObjectController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <inheritdoc />
        public async Task FinalizeAsync(V1Object entity, CancellationToken cancellationToken)
        {
            await _controller.DeletedAsync(entity, cancellationToken);
        }

    }

}
