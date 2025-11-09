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

            var api = await GetInstanceConnectionAsync(entity, cancellationToken);
            if (api == null)
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}:{entity.Name()} failed to retrieve API client.");

            //var settings = await api.Settings.FindNamedAsync( global::Seq.Api.Model.Settings.SettingName.)
            //if (settings is null)
            //    throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} cannot be loaded from API.");

            //// configuration was specified
            //if (entity.Spec.Conf is { } conf)
            //{
            //    // verify that no changes to enable_sso are being made
            //    if (conf.Flags != null && conf.Flags.EnableSSO != null && settings.Flags.EnableSSO != null && conf.Flags.EnableSSO != settings.Flags.EnableSSO)
            //        throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()}: updating the enable_sso flag is not allowed.");

            //    // push update to Auth0
            //    var req = TransformToNewtonsoftJson<TenantConf, TenantSettingsUpdateRequest>(conf);
            //    req.Flags.EnableSSO = null;
            //    settings = await api.TenantSettings.UpdateAsync(req, cancellationToken);
            //}

            //// retrieve and copy applied settings to status
            //settings = await api.TenantSettings.GetAsync(cancellationToken: cancellationToken);
            //entity.Status.LastConf = TransformToSystemTextJson<Hashtable>(settings);
            //entity = await Kube.UpdateStatusAsync(entity, cancellationToken);

            await ReconcileSuccessAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public override Task DeletedAsync(V1Alpha1Instance entity, CancellationToken cancellationToken)
        {
            Logger.LogWarning("Unsupported operation deleting entity {Entity}.", entity);
            return Task.CompletedTask;
        }

    }

}
