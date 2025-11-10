using System.Threading;
using System.Threading.Tasks;

using KubeOps.Abstractions.Finalizer;

namespace Alethic.Seq.Operator.Instance
{

    public class V1alpha1InstanceFinalizer : IEntityFinalizer<V1alpha1Instance>
    {

        public Task FinalizeAsync(V1alpha1Instance entity, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    }

}
