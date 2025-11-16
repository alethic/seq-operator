using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Instance;
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

namespace Alethic.Seq.Operator.RetentionPolicy
{

    [EntityRbac(typeof(V1alpha1Instance), Verbs = RbacVerb.List | RbacVerb.Get)]
    [EntityRbac(typeof(V1alpha1RetentionPolicy), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1Secret), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(Eventsv1Event), Verbs = RbacVerb.All)]
    public partial class V1alpha1RetentionPolicyController :
        V1alpha1InstanceEntityController<V1alpha1RetentionPolicy, V1alpha1RetentionPolicySpec, V1alpha1RetentionPolicyStatus, RetentionPolicyConf, RetentionPolicyInfo>,
        IEntityController<V1alpha1RetentionPolicy>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public V1alpha1RetentionPolicyController(IKubernetesClient kube, EntityRequeue<V1alpha1RetentionPolicy> requeue, IMemoryCache cache, V1alpha1LookupService lookup, IOptions<OperatorOptions> options, ILogger<V1alpha1RetentionPolicyController> logger) :
            base(kube, requeue, cache, lookup, options, logger)
        {

        }

        /// <inheritdoc />
        protected override string EntityTypeName => "RetentionPolicy";

        /// <inheritdoc />
        protected override Task<bool> CanAttachFromAsync(V1alpha1Instance instance, V1alpha1RetentionPolicy entity, CancellationToken cancellationToken) => instance.CheckPermissionAsync(Lookup, entity, false, p => p.RetentionPolicies?.Attach, cancellationToken);

        /// <inheritdoc />
        protected override Task<bool> CanCreateFromAsync(V1alpha1Instance instance, V1alpha1RetentionPolicy entity, CancellationToken cancellationToken) => instance.CheckPermissionAsync(Lookup, entity, false, p => p.RetentionPolicies?.Create, cancellationToken);

        /// <inheritdoc />
        protected override async Task<RetentionPolicyInfo?> GetAsync(V1alpha1RetentionPolicy entity, SeqConnection api, string id, string defaultNamespace, CancellationToken cancellationToken)
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
        protected override async Task<string?> ValidateUpdateAsync(V1alpha1Instance instance, V1alpha1RetentionPolicy entity, RetentionPolicyConf? conf, CancellationToken cancellationToken)
        {
            return null;
        }

        /// <inheritdoc />
        protected override async Task<string> CreateAsync(V1alpha1Instance instance, V1alpha1RetentionPolicy entity, SeqConnection api, RetentionPolicyConf? conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} creating RetentionPolicy in Seq.", EntityTypeName);
            var self = await api.RetentionPolicies.AddAsync(ApplyToApi(new RetentionPolicyEntity(), conf, null), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully created RetentionPolicy in Seq with id: {Id}", EntityTypeName, self.Id);
            return self.Id;
        }

        /// <inheritdoc />
        protected override async Task UpdateAsync(V1alpha1Instance instance, V1alpha1RetentionPolicy entity, SeqConnection api, string id, RetentionPolicyInfo? info, RetentionPolicyConf? conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} updating RetentionPolicy in Seq with id: {Id}", EntityTypeName, id);
            await api.RetentionPolicies.UpdateAsync(ApplyToApi(await api.RetentionPolicies.FindAsync(id, cancellationToken), conf, info), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully updated RetentionPolicy in Seq with id: {Id}", EntityTypeName, id);
        }

        /// <inheritdoc />
        protected override async Task DeleteAsync(V1alpha1Instance instance, SeqConnection api, string id, CancellationToken cancellationToken)
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
        RetentionPolicyEntity ApplyToApi(RetentionPolicyEntity target, RetentionPolicyConf? conf, RetentionPolicyInfo? info)
        {
            if (conf is null)
                return target; ;

            if (conf.RetentionTime is string retentionTime)
                if (info == null || info.RetentionTime != conf.RetentionTime)
                    target.RetentionTime = TimeSpan.Parse(retentionTime);

            return target;
        }

    }

}
