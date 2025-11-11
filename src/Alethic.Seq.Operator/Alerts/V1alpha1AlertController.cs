using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Instance;
using Alethic.Seq.Operator.Options;
using Alethic.Seq.Operator.RetentionPolicy;

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
using Seq.Api.Model.Alerting;

namespace Alethic.Seq.Operator.Alerts
{

    [EntityRbac(typeof(V1alpha1Instance), Verbs = RbacVerb.List | RbacVerb.Get)]
    [EntityRbac(typeof(V1alpha1Alert), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1Secret), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(Eventsv1Event), Verbs = RbacVerb.All)]
    public partial class V1alpha1AlertController :
        V1alpha1InstanceEntityController<V1alpha1Alert, V1alpha1AlertSpec, V1alpha1AlertStatus, AlertConf, AlertInfo>,
        IEntityController<V1alpha1Alert>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public V1alpha1AlertController(IKubernetesClient kube, EntityRequeue<V1alpha1Alert> requeue, IMemoryCache cache, IOptions<OperatorOptions> options, ILogger<V1alpha1AlertController> logger) :
            base(kube, requeue, cache, options, logger)
        {

        }

        /// <inheritdoc />
        protected override string EntityTypeName => "Alert";

        /// <inheritdoc />
        protected override async Task<string?> FindAsync(V1alpha1Alert entity, SeqConnection api, V1alpha1AlertSpec spec, string defaultNamespace, CancellationToken cancellationToken)
        {

            if (spec.Find is not null)
            {
                var title = spec.Find.Title;
                if (title is not null)
                {
                    try
                    {
                        var alerts = (IEnumerable<AlertEntity>)await api.Alerts.ListAsync(spec.Find.OwnerId, shared: true, cancellationToken: cancellationToken);
                        if (title is not null)
                            alerts = alerts.Where(i => i.Title == title);

                        var apiKey = alerts.FirstOrDefault();
                        if (apiKey is null)
                        {
                            Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} could not find Alert with title {Title} or owner {OwnerId}.", EntityTypeName, entity.Namespace(), entity.Name(), title, spec.Find.OwnerId);
                            return null;
                        }

                        Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} found existing Alert: {Id}", EntityTypeName, entity.Namespace(), entity.Name(), apiKey.Id);
                        return apiKey.Id;
                    }
                    catch (SeqApiException e)
                    {
                        Logger.LogInformation(e, "{EntityTypeName} {EntityNamespace}/{EntityName} exception finding Alert.", EntityTypeName, entity.Namespace(), entity.Name());
                        return null;
                    }
                }

                return null;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc />
        protected override async Task<AlertInfo?> GetAsync(V1alpha1Alert entity, SeqConnection api, string id, string defaultNamespace, CancellationToken cancellationToken)
        {
            try
            {
                return ToInfo(await api.Alerts.FindAsync(id, cancellationToken));
            }
            catch (SeqApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <inheritdoc />
        protected override string? ValidateCreate(AlertConf conf)
        {
            return null;
        }

        /// <inheritdoc />
        protected override async Task<string> CreateAsync(V1alpha1Alert entity, SeqConnection api, AlertConf conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} creating Alert in Seq.", EntityTypeName);
            var self = await api.Alerts.AddAsync(ApplyToApi(new AlertEntity(), conf, null), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully created Alert in Seq with id: {Id}", EntityTypeName, self.Id);
            return self.Id;
        }

        /// <inheritdoc />
        protected override async Task UpdateAsync(V1alpha1Alert entity, SeqConnection api, string id, AlertInfo? info, AlertConf conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} updating Alert in Seq with id: {Id}", EntityTypeName, id);
            await api.Alerts.UpdateAsync(ApplyToApi(await api.Alerts.FindAsync(id, cancellationToken), conf, info), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully updated Alert in Seq with id: {Id}", EntityTypeName, id);
        }

        /// <inheritdoc />
        protected override async Task DeleteAsync(SeqConnection api, string id, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} deleting Alert from Seq with ID: {Id} (reason: Kubernetes entity deleted)", EntityTypeName, id);
            await api.Alerts.RemoveAsync(await api.Alerts.FindAsync(id, cancellationToken), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully deleted Alert from Seq with ID: {Id}", EntityTypeName, id);
        }

        /// <summary>
        /// Translates a <see cref="AlertEntity"/> to a <see cref="AlertInfo"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        AlertInfo ToInfo(AlertEntity source)
        {
            return new AlertInfo()
            {
                Title = source.Title,
                Description = source.Description,
                OwnerId = source.OwnerId,
                Protected = source.IsProtected,
                Disabled = source.IsDisabled,
                Where = source.Where,
                GroupBy = source.GroupBy.Select(i => new AlertGroupingColumn() { Label = i.Label, Value = i.Value, CaseInsensitive = i.IsCaseInsensitive }).ToList(),
                TimeGrouping = source.TimeGrouping.ToString(),
                Select = source.Select.Select(i => new AlertSelectColumn() { Label = i.Label, Value = i.Value, }).ToList(),
                Having = source.Having,
                NotificationLevel = ToInfo(source.NotificationLevel),
                NotificationProperties = source.NotificationProperties.ToDictionary(i => i.Name, i => i.Value?.ToString()),
                SuppressionTime = source.SuppressionTime.ToString(),
            };
        }

        /// <summary>
        /// Translates a <see cref="LogEventLevel"/> to a <see cref="LogEventLevel"/>
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        Shared.LogEventLevel ToInfo(global::Seq.Api.Model.LogEvents.LogEventLevel level) => level switch
        {
            global::Seq.Api.Model.LogEvents.LogEventLevel.Verbose => Shared.LogEventLevel.Verbose,
            global::Seq.Api.Model.LogEvents.LogEventLevel.Debug => Shared.LogEventLevel.Debug,
            global::Seq.Api.Model.LogEvents.LogEventLevel.Information => Shared.LogEventLevel.Information,
            global::Seq.Api.Model.LogEvents.LogEventLevel.Warning => Shared.LogEventLevel.Warning,
            global::Seq.Api.Model.LogEvents.LogEventLevel.Error => Shared.LogEventLevel.Error,
            global::Seq.Api.Model.LogEvents.LogEventLevel.Fatal => Shared.LogEventLevel.Fatal,
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Creates an <see cref="AlertEntity"/> for creating or updating.
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        AlertEntity ApplyToApi(AlertEntity target, AlertConf conf, AlertInfo? info)
        {
            if (conf.Title is not null)
                if (info == null || info.Title != conf.Title)
                    target.Title = conf.Title;

            return target;
        }

    }

}
