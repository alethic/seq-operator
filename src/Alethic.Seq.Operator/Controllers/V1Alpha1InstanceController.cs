using System;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Core.Models.Instance;
using Alethic.Seq.Operator.Models;

using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Alethic.Seq.Operator.Controllers
{

    [EntityRbac(typeof(V1Alpha1Instance), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1Secret), Verbs = RbacVerb.List | RbacVerb.Get)]
    [EntityRbac(typeof(Eventsv1Event), Verbs = RbacVerb.All)]
    public class V1Alpha1InstanceController :
        V1Alpha1Controller<V1Alpha1Instance, V1Alpha1Instance.SpecDef, V1Alpha1Instance.StatusDef, InstanceConf, InstanceInfo>,
        IEntityController<V1Alpha1Instance>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="logger"></param>
        public V1Alpha1InstanceController(IKubernetesClient kube, EntityRequeue<V1Alpha1Instance> requeue, IMemoryCache cache, ILogger<V1Alpha1InstanceController> logger) :
            base(kube, requeue, cache, logger)
        {

        }

        /// <inheritdoc />
        protected override string EntityTypeName => "Instance";

        /// <inheritdoc />
        protected override async Task Reconcile(V1Alpha1Instance entity, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Task DeletedAsync(V1Alpha1Instance entity, CancellationToken cancellationToken)
        {
            Logger.LogWarning("Unsupported operation deleting entity {Entity}.", entity);
            return Task.CompletedTask;
        }

    }

}
