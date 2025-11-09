using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Models;

using KubeOps.Abstractions.Finalizer;

namespace Alethic.Seq.Operator.Finalizers
{

    public class V1Alpha1TenantFinalizer : IEntityFinalizer<V1Alpha1Instance>
    {

        public Task FinalizeAsync(V1Alpha1Instance entity, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    }

}
