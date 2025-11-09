using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Core.Models.ApiKey;
using Alethic.Seq.Operator.Models;
using Alethic.Seq.Operator.Options;

using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Seq.Api;
using Seq.Api.Client;
using Seq.Api.Model.Inputs;
using Seq.Api.Model.Security;
using Seq.Api.Model.Shared;

namespace Alethic.Seq.Operator.Controllers
{

    [EntityRbac(typeof(V1Alpha1Instance), Verbs = RbacVerb.List | RbacVerb.Get)]
    [EntityRbac(typeof(V1Alpha1ApiKey), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1Secret), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(Eventsv1Event), Verbs = RbacVerb.All)]
    public partial class V1Alpha1ApiKeyController :
        V1Alpha1InstanceEntityController<V1Alpha1ApiKey, V1Alpha1ApiKey.SpecDef, V1Alpha1ApiKey.StatusDef, ApiKeyConf, ApiKeyInfo>,
        IEntityController<V1Alpha1ApiKey>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public V1Alpha1ApiKeyController(IKubernetesClient kube, EntityRequeue<V1Alpha1ApiKey> requeue, IMemoryCache cache, ILogger<V1Alpha1ApiKeyController> logger, IOptions<OperatorOptions> options) :
            base(kube, requeue, cache, logger, options)
        {

        }

        /// <inheritdoc />
        protected override string EntityTypeName => "ApiKey";

        /// <inheritdoc />
        protected override async Task<ApiKeyInfo?> Get(SeqConnection api, string id, string defaultNamespace, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override async Task<string?> Find(SeqConnection api, V1Alpha1ApiKey entity, V1Alpha1ApiKey.SpecDef spec, string defaultNamespace, CancellationToken cancellationToken)
        {
            if (spec.Find is not null)
            {
                var title = spec.Find.Title;
                if (title is not null)
                {
                    try
                    {
                        var apiKeys = (IEnumerable<ApiKeyEntity>)await api.ApiKeys.ListAsync(spec.Find.OwnerId, shared: true, cancellationToken: cancellationToken);
                        if (title is not null)
                            apiKeys = apiKeys.Where(i => i.Title == title);

                        var apiKey = apiKeys.FirstOrDefault();
                        if (apiKey is null)
                        {
                            Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} could not find ApiKey with title {Title} or owner {OwnerId}.", EntityTypeName, entity.Namespace(), entity.Name(), title, spec.Find.OwnerId);
                            return null;
                        }


                        Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} found existing ApiKey: {Id}", EntityTypeName, entity.Namespace(), entity.Name(), apiKey.Id);
                        return apiKey.Id;
                    }
                    catch (SeqApiException e)
                    {
                        Logger.LogInformation(e, "{EntityTypeName} {EntityNamespace}/{EntityName} exception finding ApiKey.", EntityTypeName, entity.Namespace(), entity.Name());
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
        protected override string? ValidateCreate(ApiKeyConf conf)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override async Task<string> Create(SeqConnection api, ApiKeyConf conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} creating ApiKey in Seq with title: {Title}", EntityTypeName, conf.Title);
            var self = await api.ApiKeys.AddAsync(ToApi(conf, null), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully created ApiKey in Seq with ID: {Id} and title: {Title}", EntityTypeName, self.Id, conf.Title);
            return self.Id;
        }

        /// <inheritdoc />
        protected override async Task Update(SeqConnection api, string id, ApiKeyInfo? info, ApiKeyConf conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} updating ApiKey in Seq with id: {Id} and title: {Title}", EntityTypeName, id, conf.Title);
            await api.ApiKeys.UpdateAsync(ToApi(conf, info), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully updated ApiKey in Seq with id: {Id} and title: {Title}", EntityTypeName, id, conf.Title);
        }

        /// <inheritdoc />
        protected override async Task ApplyStatus(SeqConnection api, V1Alpha1ApiKey entity, ApiKeyInfo? info, string defaultNamespace, CancellationToken cancellationToken)
        {
            // always apply secret if specified, even if token value is empty, to ensure we initialize an empty secret
            if (entity.Spec.SecretRef is not null)
                await ApplySecret(entity, info?.Token, defaultNamespace, cancellationToken);

            await base.ApplyStatus(api, entity, info, defaultNamespace, cancellationToken);
        }

        /// <inheritdoc />
        protected override async Task Delete(SeqConnection api, string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            //Logger.LogInformation("{EntityTypeName} deleting client from Seq with ID: {ClientId} (reason: Kubernetes entity deleted)", EntityTypeName, id);
            //Logger.LogInformation("{EntityTypeName} successfully deleted client from Seq with ID: {ClientId}", EntityTypeName, id);
        }

        /// <summary>
        /// Applies the client secret.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task ApplySecret(V1Alpha1ApiKey entity, string? token, string defaultNamespace, CancellationToken cancellationToken)
        {
            if (entity.Spec.SecretRef is null)
                return;

            // find existing secret or create
            var secret = await ResolveSecretRef(entity.Spec.SecretRef, entity.Spec.SecretRef.NamespaceProperty ?? defaultNamespace, cancellationToken);
            if (secret is null)
            {
                Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} referenced secret {SecretName} which does not exist: creating.", EntityTypeName, entity.Namespace(), entity.Name(), entity.Spec.SecretRef.Name);
                secret = await Kube.CreateAsync(
                    new V1Secret(
                        metadata: new V1ObjectMeta(namespaceProperty: entity.Spec.SecretRef.NamespaceProperty ?? defaultNamespace, name: entity.Spec.SecretRef.Name))
                        .WithOwnerReference(entity),
                    cancellationToken);
            }

            // only apply actual values if we are the owner
            if (secret.IsOwnedBy(entity))
            {
                Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} referenced secret {SecretName}: updating.", EntityTypeName, entity.Namespace(), entity.Name(), entity.Spec.SecretRef.Name);
                secret.StringData ??= new Dictionary<string, string>();

                // ensure the key exists, possible empty, if we're retrieving an existing ApiKey
                if (token is not null)
                {
                    secret.StringData["apikey"] = token;
                    Logger.LogDebug("{EntityTypeName} {EntityNamespace}/{EntityName} updated secret {SecretName} with clientSecret", EntityTypeName, entity.Namespace(), entity.Name(), entity.Spec.SecretRef.Name);
                }
                else if (!secret.StringData.ContainsKey("apikey"))
                {
                    secret.StringData["apikey"] = "";
                    Logger.LogDebug("{EntityTypeName} {EntityNamespace}/{EntityName} initialized empty ApiKey in secret {SecretName}", EntityTypeName, entity.Namespace(), entity.Name(), entity.Spec.SecretRef.Name);
                }

                secret = await Kube.UpdateAsync(secret, cancellationToken);
                Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} successfully updated secret {SecretName}", EntityTypeName, entity.Namespace(), entity.Name(), entity.Spec.SecretRef.Name);
            }
            else
            {
                Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} secret {SecretName} exists but is not owned by this ApiKey, skipping update", EntityTypeName, entity.Namespace(), entity.Name(), entity.Spec.SecretRef.Name);
            }
        }

        /// <summary>
        /// Creates an <see cref="ApiKeyEntity"/> for creating or updating.
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        ApiKeyEntity ToApi(ApiKeyConf conf, ApiKeyInfo? info)
        {
            var entity = new ApiKeyEntity()
            {
                Title = conf.Title ?? info?.Title,
                Token = conf.Token,
                IsDefault = conf.IsDefault ?? info?.IsDefault ?? false,
                OwnerId = conf.OwnerId ?? info?.OwnerId,
            };

            if (conf.AssignedPermissions != null)
                ApplyToApi(entity, conf.AssignedPermissions);
            else if (info != null && info.AssignedPermissions != null)
                ApplyToApi(entity, info.AssignedPermissions);

            if (conf.InputSettings != null)
                ApplyToApi(entity.InputSettings, conf.InputSettings);
            else if (info != null && info.InputSettings != null)
                ApplyToApi(entity.InputSettings, info.InputSettings);

            return entity;
        }

        /// <summary>
        /// Transforms the entity version of <see cref="ApiKeyPermission"/> into the API version.
        /// </summary>
        /// <param name="assignedPermissions"></param>
        /// <returns></returns>
        void ApplyToApi(ApiKeyEntity entity, ApiKeyPermission[] assignedPermissions)
        {
            foreach (var i in assignedPermissions)
                entity.AssignedPermissions.Add(ToApi(i));
        }

        /// <summary>
        /// Transforms the entity version of <see cref="ApiKeyPermission"/> into the API version.
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        Permission ToApi(ApiKeyPermission permission) => permission switch
        {
            ApiKeyPermission.Undefined => Permission.Undefined,
            ApiKeyPermission.Public => Permission.Public,
            ApiKeyPermission.Ingest => Permission.Ingest,
            ApiKeyPermission.Read => Permission.Read,
            ApiKeyPermission.Write => Permission.Write,
            ApiKeyPermission.Setup => Permission.Setup,
            ApiKeyPermission.Project => Permission.Project,
            ApiKeyPermission.System => Permission.System,
            ApiKeyPermission.Organization => Permission.Organization,
            _ => throw new NotImplementedException(),
        };


        /// <summary>
        /// Applies the <see cref="ApiKeyInputSettings"/> to the API version.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="inputSettings"></param>
        /// <returns></returns>
        void ApplyToApi(InputSettingsPart api, ApiKeyInputSettings inputSettings)
        {
            if (inputSettings.AppliedProperties is not null)
                foreach (var i in inputSettings.AppliedProperties)
                    api.AppliedProperties.Add(ToApi(i));

            if (inputSettings.Filter is not null)
                ApplyToApi(api.Filter, inputSettings.Filter);

            if (inputSettings.UseServerTimestamps is bool b)
                api.UseServerTimestamps = b;

            if (inputSettings.MinimumLevel is ApiKeyLogEventLevel l)
                api.MinimumLevel = ToApi(l);
        }

        EventPropertyPart ToApi(ApiKeyEventProperty property)
        {
            return new EventPropertyPart(property.Name, property.Value);
        }

        /// <summary>
        /// Applies the <see cref="ApiKeyDescriptiveFilter"/> to the API version.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="filter"></param>
        void ApplyToApi(DescriptiveFilterPart api, ApiKeyDescriptiveFilter filter)
        {
            if (filter.DescriptionIsExcluded is bool d)
                api.DescriptionIsExcluded = d;
            if (filter.Description is string s)
                api.Description = s;
            if (filter.Filter is string f)
                api.Filter = f;
            if (filter.FilterNonStrict is string z)
                api.FilterNonStrict = z;
        }

        global::Seq.Api.Model.LogEvents.LogEventLevel ToApi(ApiKeyLogEventLevel level) => level switch
        {
            ApiKeyLogEventLevel.Verbose => global::Seq.Api.Model.LogEvents.LogEventLevel.Verbose,
            ApiKeyLogEventLevel.Debug => global::Seq.Api.Model.LogEvents.LogEventLevel.Debug,
            ApiKeyLogEventLevel.Information => global::Seq.Api.Model.LogEvents.LogEventLevel.Information,
            ApiKeyLogEventLevel.Warning => global::Seq.Api.Model.LogEvents.LogEventLevel.Warning,
            ApiKeyLogEventLevel.Error => global::Seq.Api.Model.LogEvents.LogEventLevel.Error,
            ApiKeyLogEventLevel.Fatal => global::Seq.Api.Model.LogEvents.LogEventLevel.Fatal,
            _ => throw new NotImplementedException(),
        };

    }

}
