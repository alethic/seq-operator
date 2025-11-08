using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Models;

using KubeOps.Abstractions.Finalizer;

namespace Alethic.Seq.Operator.Finalizers
{

    public class V1TenantFinalizer : IEntityFinalizer<V1Instance>
    {

        public Task FinalizeAsync(V1Instance entity, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    }

}
