using System;
using System.Collections.Generic;
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
using Seq.Api.Model.Shared;
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

            var data = new SignalEntity();
            ApplyToApi(data, conf);
            var self = await api.Signals.AddAsync(data, cancellationToken);

            Logger.LogInformation("{EntityTypeName} successfully created Signal in Seq with id: {Id}", EntityTypeName, self.Id);

            return self.Id;
        }

        /// <inheritdoc />
        protected override async Task UpdateAsync(V1alpha1Instance instance, V1alpha1Signal entity, SeqConnection api, string id, SignalInfo? info, SignalConf? conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} updating Signal in Seq with id: {Id}", EntityTypeName, id);

            var data = await api.Signals.FindAsync(id, cancellationToken);
            if (ApplyToApi(data, conf))
                await api.Signals.UpdateAsync(data, cancellationToken);

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
        bool ApplyToApi(SignalEntity target, SignalConf? conf)
        {
            var changed = false;

            if (conf is null)
                return false;

            if (conf.Title is not null && target.Title != conf.Title)
            {
                target.Title = conf.Title;
                changed = true;
            }

            if (conf.Description is not null && target.Description != conf.Description)
            {
                target.Description = conf.Description;
                changed = true;
            }

            if (conf.ExplicitGroupName is not null && target.ExplicitGroupName != conf.ExplicitGroupName)
            {
                target.ExplicitGroupName = conf.ExplicitGroupName;
                changed = true;
            }

            if (conf.IsProtected is not null && target.IsProtected != conf.IsProtected)
            {
                target.IsProtected = (bool)conf.IsProtected;
                changed = true;
            }

            if (conf.Grouping is not null && target.Grouping != ToApi((SignalGrouping)conf.Grouping))
            {
                target.Grouping = ToApi((SignalGrouping)conf.Grouping);
                changed = true;
            }

            if (conf.Filters is { } filters)
                changed |= ApplyToApi(target.Filters, filters);

            if (conf.Columns is { } columns)
                changed |= ApplyToApi(target.Columns, columns);

            return changed;
        }

        /// <summary>
        /// Applies the source filters to the target filter list.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <exception cref="NotImplementedException"></exception>
        bool ApplyToApi(List<DescriptiveFilterPart> target, IList<DescriptiveFilter> source)
        {
            var changed = false;

            // apply each source item in order
            for (int i = 0; i < source.Count; i++)
            {
                var src = source[i];
                var dst = i < target.Count ? target[i] : null;
                if (dst is null)
                {
                    target.Add(dst = new DescriptiveFilterPart());
                    changed = true;
                }

                if (src.Description is not null && dst.Description != src.Description)
                {
                    dst.Description = src.Description;
                    changed = true;
                }

                if (src.DescriptionIsExcluded is not null && dst.DescriptionIsExcluded != src.DescriptionIsExcluded)
                {
                    dst.DescriptionIsExcluded = (bool)src.DescriptionIsExcluded;
                    changed = true;
                }

                if (src.Filter is not null && dst.Filter != src.Filter)
                {
                    dst.Filter = src.Filter;
                    changed = true;
                }

                if (src.FilterNonStrict is not null && dst.FilterNonStrict != src.FilterNonStrict)
                {
                    dst.FilterNonStrict = src.FilterNonStrict;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Applies the source columns to the target columns list.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <exception cref="NotImplementedException"></exception>
        bool ApplyToApi(List<SignalColumnPart> target, IList<SignalColumn> source)
        {
            var changed = false;

            // apply each source item in order
            for (int i = 0; i < source.Count; i++)
            {
                var src = source[i];
                var dst = i < target.Count ? target[i] : null;
                if (dst is null)
                {
                    target.Add(dst = new SignalColumnPart());
                    changed = true;
                }

                if (src.Expression is not null && dst.Expression != src.Expression)
                {
                    dst.Expression = src.Expression;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Transforms a <see cref="SignalGrouping"/> to a <see cref="global::Seq.Api.Model.Signals.SignalGrouping"/>.
        /// </summary>
        /// <param name="grouping"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        global::Seq.Api.Model.Signals.SignalGrouping ToApi(SignalGrouping grouping)
        {
            return grouping switch
            {
                SignalGrouping.Inferred => global::Seq.Api.Model.Signals.SignalGrouping.Inferred,
                SignalGrouping.Explicit => global::Seq.Api.Model.Signals.SignalGrouping.Explicit,
                SignalGrouping.None => global::Seq.Api.Model.Signals.SignalGrouping.None,
                _ => throw new NotImplementedException(),
            };
        }
    }

}
