using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Instance;
using Alethic.Seq.Operator.Options;
using Alethic.Seq.Operator.Shared;

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
using Seq.Api.Model.Signals;

namespace Alethic.Seq.Operator.Signals
{

    [EntityRbac(typeof(V1alpha1Instance), Verbs = RbacVerb.List | RbacVerb.Get)]
    [EntityRbac(typeof(V1alpha1Signal), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1Secret), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(Eventsv1Event), Verbs = RbacVerb.All)]
    public partial class V1alpha1SignalController :
        V1alpha1InstanceEntityController<V1alpha1Signal, V1alpha1SignalSpec, V1alpha1SignalStatus, SignalConf, SignalInfo>,
        IEntityController<V1alpha1Signal>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="lookup"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public V1alpha1SignalController(IKubernetesClient kube, EntityRequeue<V1alpha1Signal> requeue, IMemoryCache cache, LookupService lookup, IOptions<OperatorOptions> options, ILogger<V1alpha1SignalController> logger) :
            base(kube, requeue, cache, lookup, options, logger)
        {

        }

        /// <inheritdoc />
        protected override string EntityTypeName => "Signal";

        /// <inheritdoc />
        protected override Task<bool> CanAttachFromAsync(V1alpha1Instance instance, V1alpha1Signal entity, CancellationToken cancellationToken) => instance.CheckPermissionAsync(Lookup, entity, false, p => p.Signals?.Attach, cancellationToken);

        /// <inheritdoc />
        protected override Task<bool> CanCreateFromAsync(V1alpha1Instance instance, V1alpha1Signal entity, CancellationToken cancellationToken) => instance.CheckPermissionAsync(Lookup, entity, false, p => p.Signals?.Create, cancellationToken);

        /// <inheritdoc />
        protected override async Task<string?> FindAsync(V1alpha1Signal entity, SeqConnection api, V1alpha1SignalSpec spec, string defaultNamespace, CancellationToken cancellationToken)
        {
            return null;
        }

        /// <inheritdoc />
        protected override async Task<SignalInfo?> GetAsync(V1alpha1Signal entity, SeqConnection api, string id, string defaultNamespace, CancellationToken cancellationToken)
        {
            try
            {
                return ToInfo(await api.Signals.FindAsync(id, cancellationToken));
            }
            catch (SeqApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <inheritdoc />
        protected override async Task<string?> ValidateUpdateAsync(V1alpha1Instance instance, V1alpha1Signal entity, SignalConf? conf, CancellationToken cancellationToken)
        {
            return null;
        }

        /// <inheritdoc />
        protected override async Task<string> CreateAsync(V1alpha1Instance instance, V1alpha1Signal entity, SeqConnection api, SignalConf? conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} creating Signal in Seq.", EntityTypeName);
            var self = await api.Signals.AddAsync(ApplyToApi(new SignalEntity(), conf, null), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully created Signal in Seq with id: {Id}", EntityTypeName, self.Id);
            return self.Id;
        }

        /// <inheritdoc />
        protected override async Task UpdateAsync(V1alpha1Instance instance, V1alpha1Signal entity, SeqConnection api, string id, SignalInfo? info, SignalConf? conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} updating Signal in Seq with id: {Id}", EntityTypeName, id);
            await api.Signals.UpdateAsync(ApplyToApi(await api.Signals.FindAsync(id, cancellationToken), conf, info), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully updated Signal in Seq with id: {Id}", EntityTypeName, id);
        }

        /// <inheritdoc />
        protected override async Task DeleteAsync(V1alpha1Instance instance, SeqConnection api, string id, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} deleting Signal from Seq with ID: {Id} (reason: Kubernetes entity deleted)", EntityTypeName, id);
            await api.Signals.RemoveAsync(await api.Signals.FindAsync(id, cancellationToken), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully deleted Signal from Seq with ID: {Id}", EntityTypeName, id);
        }

        /// <summary>
        /// Translates a <see cref="SignalEntity"/> to a <see cref="SignalInfo"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        SignalInfo ToInfo(SignalEntity source) => new SignalInfo()
        {
            Title = source.Title,
            Description = source.Description,
            ExplicitGroupName = source.ExplicitGroupName,
            IsIndexSuppressed = source.IsIndexSuppressed,
            IsProtected = source.IsProtected,
            Grouping = ToInfo(source.Grouping),
            Filters = source.Filters.Select(DescriptiveFilter.FromApi).ToList(),
            Columns = source.Columns.Select(ToInfo).ToList(),
        };

        /// <summary>
        /// Transforms the <see cref="global::Seq.Api.Model.Signals.SignalGrouping"/> to a <see cref="SignalGrouping"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        SignalGrouping ToInfo(global::Seq.Api.Model.Signals.SignalGrouping source) => source switch
        {
            global::Seq.Api.Model.Signals.SignalGrouping.Inferred => SignalGrouping.Inferred,
            global::Seq.Api.Model.Signals.SignalGrouping.Explicit => SignalGrouping.Explicit,
            global::Seq.Api.Model.Signals.SignalGrouping.None => SignalGrouping.None,
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Transforms the <see cref="SignalColumnPart"/> to a <see cref="SignalColumn"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        SignalColumn ToInfo(SignalColumnPart source) => new SignalColumn()
        {
            Expression = source.Expression
        };

        /// <summary>
        /// Creates an <see cref="SignalEntity"/> for creating or updating.
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        SignalEntity ApplyToApi(SignalEntity target, SignalConf? conf, SignalInfo? info)
        {
            if (conf is null)
                return target;

            if (conf.Title is not null)
                if (info == null || info.Title != conf.Title)
                    target.Title = conf.Title;

            if (conf.Description is not null)
                if (info == null || info.Description != conf.Description)
                    target.Description = conf.Description;

            return target;
        }

    }

}
