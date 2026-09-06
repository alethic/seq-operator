using System.Threading;
using System.Threading.Tasks;

using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Finalizer;

namespace Alethic.Seq.Operator.Instance
{

    public class V1alpha1InstanceFinalizer : IEntityFinalizer<V1alpha1Instance>
    {

        public Task<ReconciliationResult<V1alpha1Instance>> FinalizeAsync(V1alpha1Instance entity, CancellationToken cancellationToken)
        {
            return Task.FromResult(ReconciliationResult<V1alpha1Instance>.Success(entity));
        }

    }

}
