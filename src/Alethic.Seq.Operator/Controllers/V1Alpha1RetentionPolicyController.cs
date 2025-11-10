using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Core.Models.RetentionPolicy;
using Alethic.Seq.Operator.Models;
using Alethic.Seq.Operator.Options;

using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Seq.Api;
using Seq.Api.Client;
using Seq.Api.Model.Retention;

namespace Alethic.Seq.Operator.Controllers
{

    [EntityRbac(typeof(V1Alpha1Instance), Verbs = RbacVerb.List | RbacVerb.Get)]
    [EntityRbac(typeof(V1Alpha1RetentionPolicy), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1Secret), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(Eventsv1Event), Verbs = RbacVerb.All)]
    public partial class V1Alpha1RetentionPolicyController :
        V1Alpha1InstanceEntityController<V1Alpha1RetentionPolicy, V1Alpha1RetentionPolicy.SpecDef, V1Alpha1RetentionPolicy.StatusDef, RetentionPolicyConf, RetentionPolicyInfo>,
        IEntityController<V1Alpha1RetentionPolicy>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public V1Alpha1RetentionPolicyController(IKubernetesClient kube, EntityRequeue<V1Alpha1RetentionPolicy> requeue, IMemoryCache cache, IOptions<OperatorOptions> options, ILogger<V1Alpha1RetentionPolicyController> logger) :
            base(kube, requeue, cache, options, logger)
        {

        }

        /// <inheritdoc />
        protected override string EntityTypeName => "RetentionPolicy";

        /// <inheritdoc />
        protected override async Task<RetentionPolicyInfo?> Get(V1Alpha1RetentionPolicy entity, SeqConnection api, string id, string defaultNamespace, CancellationToken cancellationToken)
        {
            try
            {
                return ToInfo(await api.RetentionPolicies.FindAsync(id, cancellationToken));
            }
            catch (SeqApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <inheritdoc />
        protected override string? ValidateCreate(RetentionPolicyConf conf)
        {
            return null;
        }

        /// <inheritdoc />
        protected override async Task<string> Create(V1Alpha1RetentionPolicy entity, SeqConnection api, RetentionPolicyConf conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} creating RetentionPolicy in Seq.", EntityTypeName);
            var self = await api.RetentionPolicies.AddAsync(ApplyToApi(new RetentionPolicyEntity(), conf, null), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully created RetentionPolicy in Seq with id: {Id}", EntityTypeName, self.Id);
            return self.Id;
        }

        /// <inheritdoc />
        protected override async Task Update(V1Alpha1RetentionPolicy entity, SeqConnection api, string id, RetentionPolicyInfo? info, RetentionPolicyConf conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} updating RetentionPolicy in Seq with id: {Id}", EntityTypeName, id);
            await api.RetentionPolicies.UpdateAsync(ApplyToApi(await api.RetentionPolicies.FindAsync(id, cancellationToken), conf, info), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully updated RetentionPolicy in Seq with id: {Id}", EntityTypeName, id);
        }

        /// <inheritdoc />
        protected override async Task Delete(SeqConnection api, string id, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} deleting RetentionPolicy from Seq with ID: {Id} (reason: Kubernetes entity deleted)", EntityTypeName, id);
            await api.RetentionPolicies.RemoveAsync(await api.RetentionPolicies.FindAsync(id, cancellationToken), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully deleted RetentionPolicy from Seq with ID: {Id}", EntityTypeName, id);
        }

        /// <summary>
        /// Translates a <see cref="RetentionPolicyEntity"/> to a <see cref="RetentionPolicyInfo"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        RetentionPolicyInfo ToInfo(RetentionPolicyEntity source)
        {
            return new RetentionPolicyInfo()
            {
                RetentionTime = source.RetentionTime.ToString(),
            };
        }

        /// <summary>
        /// Creates an <see cref="RetentionPolicyEntity"/> for creating or updating.
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        RetentionPolicyEntity ApplyToApi(RetentionPolicyEntity target, RetentionPolicyConf conf, RetentionPolicyInfo? info)
        {
            if (conf.RetentionTime is string retentionTime)
                if (info == null || info.RetentionTime != conf.RetentionTime)
                    target.RetentionTime = TimeSpan.Parse(retentionTime);

            return target;
        }

    }

}
