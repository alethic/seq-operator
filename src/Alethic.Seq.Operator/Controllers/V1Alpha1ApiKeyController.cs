using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
using Seq.Api.Model.LogEvents;
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
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public V1Alpha1ApiKeyController(IKubernetesClient kube, EntityRequeue<V1Alpha1ApiKey> requeue, IMemoryCache cache, IOptions<OperatorOptions> options, ILogger<V1Alpha1ApiKeyController> logger) :
            base(kube, requeue, cache, options, logger)
        {

        }

        /// <inheritdoc />
        protected override string EntityTypeName => "ApiKey";

        /// <inheritdoc />
        protected override async Task<string?> Find(V1Alpha1ApiKey entity, SeqConnection api, V1Alpha1ApiKey.SpecDef spec, string defaultNamespace, CancellationToken cancellationToken)
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
        protected override async Task<ApiKeyInfo?> Get(V1Alpha1ApiKey entity, SeqConnection api, string id, string defaultNamespace, CancellationToken cancellationToken)
        {
            try
            {
                return ToInfo(await api.ApiKeys.FindAsync(id, cancellationToken));
            }
            catch (SeqApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <inheritdoc />
        protected override string? ValidateCreate(ApiKeyConf conf)
        {
            if (conf.Title == null)
                return "conf.title is required";

            return null;
        }

        /// <inheritdoc />
        protected override async Task<string> Create(V1Alpha1ApiKey entity, SeqConnection api, ApiKeyConf conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} creating ApiKey in Seq with title: {Title}", EntityTypeName, conf.Title);

            // create a new entity
            var create = new ApiKeyEntity();
            ApplyToApi(create, conf, null);

            // entity specifies an existing secret, this may be a token source
            if (entity.Spec.SecretRef is not null)
            {
                var secret = await ResolveSecretRef(entity.Spec.SecretRef, entity.Spec.SecretRef.NamespaceProperty ?? defaultNamespace, cancellationToken);
                if (secret is not null)
                    if (secret.Data.TryGetValue("token", out var tokenBuf))
                        create.Token = Encoding.UTF8.GetString(tokenBuf);
            }

            // submit the new key to the API
            var self = await api.ApiKeys.AddAsync(create, cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully created ApiKey in Seq with ID: {Id} and title: {Title}", EntityTypeName, self.Id, conf.Title);

            // update the secret with the token, which may be from the server
            await ApplySecret(entity, self.Token ?? create.Token, defaultNamespace, cancellationToken);

            // newly created ID
            return self.Id;
        }

        /// <inheritdoc />
        protected override async Task Update(V1Alpha1ApiKey entity, SeqConnection api, string id, ApiKeyInfo? info, ApiKeyConf conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} updating ApiKey in Seq with id: {Id} and title: {Title}", EntityTypeName, id, conf.Title);
            await api.ApiKeys.UpdateAsync(ApplyToApi(await api.ApiKeys.FindAsync(id, cancellationToken), conf, info), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully updated ApiKey in Seq with id: {Id} and title: {Title}", EntityTypeName, id, conf.Title);
        }

        /// <inheritdoc />
        protected override async Task Delete(SeqConnection api, string id, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{EntityTypeName} deleting client from Seq with ID: {ClientId} (reason: Kubernetes entity deleted)", EntityTypeName, id);
            await api.ApiKeys.RemoveAsync(await api.ApiKeys.FindAsync(id, cancellationToken), cancellationToken);
            Logger.LogInformation("{EntityTypeName} successfully deleted client from Seq with ID: {ClientId}", EntityTypeName, id);
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
                    secret.StringData["token"] = token;
                    Logger.LogDebug("{EntityTypeName} {EntityNamespace}/{EntityName} updated secret {SecretName} with token", EntityTypeName, entity.Namespace(), entity.Name(), entity.Spec.SecretRef.Name);
                }
                else if (!secret.StringData.ContainsKey("apikey"))
                {
                    secret.StringData["token"] = "";
                    Logger.LogDebug("{EntityTypeName} {EntityNamespace}/{EntityName} initialized empty token in secret {SecretName}", EntityTypeName, entity.Namespace(), entity.Name(), entity.Spec.SecretRef.Name);
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
        /// Translates a <see cref="ApiKeyEntity"/> to a <see cref="ApiKeyInfo"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        ApiKeyInfo ToInfo(ApiKeyEntity source)
        {
            return new ApiKeyInfo()
            {
                TokenPrefix = source.TokenPrefix,
                Title = source.Title,
                IsDefault = source.IsDefault,
                OwnerId = source.OwnerId,
                Permissions = source.AssignedPermissions.Select(ToInfo).ToArray(),
                InputSettings = ToInfo(source.InputSettings),
            };
        }

        /// <summary>
        /// Translates a <see cref="Permission"/> to a <see cref="ApiKeyPermission"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        ApiKeyPermission ToInfo(Permission source) => source switch
        {
            Permission.Undefined => ApiKeyPermission.Undefined,
            Permission.Public => ApiKeyPermission.Public,
            Permission.Ingest => ApiKeyPermission.Ingest,
            Permission.Read => ApiKeyPermission.Read,
            Permission.Write => ApiKeyPermission.Write,
            Permission.Setup => ApiKeyPermission.Setup,
            Permission.Project => ApiKeyPermission.Project,
            Permission.System => ApiKeyPermission.System,
            Permission.Organization => ApiKeyPermission.Organization,
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Translates a <see cref="InputSettingsPart"/> to a <see cref="ApiKeyInputSettings"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        ApiKeyInputSettings ToInfo(InputSettingsPart source)
        {
            return new ApiKeyInputSettings()
            {
                AppliedProperties = source.AppliedProperties.ToDictionary(i => i.Name, i => (string?)i.Value),
                Filter = ToInfo(source.Filter),
                UseServerTimestamps = source.UseServerTimestamps,
                MinimumLevel = source.MinimumLevel != null ? ToInfo(source.MinimumLevel.Value) : null,
            };
        }

        /// <summary>
        /// Transforms a <see cref="DescriptiveFilterPart"/> to a <see cref="ApiKeyDescriptiveFilter"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        ApiKeyDescriptiveFilter ToInfo(DescriptiveFilterPart source) => new ApiKeyDescriptiveFilter()
        {
            DescriptionIsExcluded = source.DescriptionIsExcluded,
            Description = source.Description,
            Filter = source.Filter,
            FilterNonStrict = source.FilterNonStrict,
        };

        /// <summary>
        /// Translates a <see cref="LogEventLevel"/> to a <see cref="ApiKeyLogEventLevel"/>
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        ApiKeyLogEventLevel ToInfo(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => ApiKeyLogEventLevel.Verbose,
            LogEventLevel.Debug => ApiKeyLogEventLevel.Debug,
            LogEventLevel.Information => ApiKeyLogEventLevel.Information,
            LogEventLevel.Warning => ApiKeyLogEventLevel.Warning,
            LogEventLevel.Error => ApiKeyLogEventLevel.Error,
            LogEventLevel.Fatal => ApiKeyLogEventLevel.Fatal,
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Creates an <see cref="ApiKeyEntity"/> for creating or updating.
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        ApiKeyEntity ApplyToApi(ApiKeyEntity target, ApiKeyConf conf, ApiKeyInfo? info)
        {
            if (conf.Title is not null)
                if (info == null || info.Title != conf.Title)
                    target.Title = conf.Title;

            if (conf.IsDefault is not null)
                if (info == null || info.IsDefault != conf.IsDefault)
                    target.IsDefault = (bool)conf.IsDefault;

            //if (conf.OwnerId is not null)
            //    if (info == null || info.OwnerId != conf.OwnerId)
            //        target.OwnerId = conf.OwnerId;

            if (conf.Permissions != null)
                ApplyToApi(target, conf.Permissions);
            else if (info != null && info.Permissions != null)
                ApplyToApi(target, info.Permissions);

            if (conf.InputSettings != null)
                ApplyToApi(target.InputSettings, conf.InputSettings);
            else if (info != null && info.InputSettings != null)
                ApplyToApi(target.InputSettings, info.InputSettings);

            return target;
        }

        /// <summary>
        /// Transforms the entity version of <see cref="ApiKeyPermission"/> into the API version.
        /// </summary>
        /// <param name="permissions"></param>
        /// <returns></returns>
        void ApplyToApi(ApiKeyEntity target, ApiKeyPermission[] permissions)
        {
            foreach (var i in permissions)
                target.AssignedPermissions.Add(ToApi(i));
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
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        void ApplyToApi(InputSettingsPart target, ApiKeyInputSettings source)
        {
            if (source.AppliedProperties is not null)
                foreach (var kvp in source.AppliedProperties)
                    target.AppliedProperties.Add(new EventPropertyPart(kvp.Key, kvp.Value));

            if (source.Filter is not null)
                ApplyToApi(target.Filter, source.Filter);

            if (source.UseServerTimestamps is bool b)
                target.UseServerTimestamps = b;

            if (source.MinimumLevel is ApiKeyLogEventLevel l)
                target.MinimumLevel = ToApi(l);
        }

        /// <summary>
        /// Applies the <see cref="ApiKeyDescriptiveFilter"/> to the API version.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        void ApplyToApi(DescriptiveFilterPart target, ApiKeyDescriptiveFilter source)
        {
            if (source.DescriptionIsExcluded is bool descriptionIsExcluded)
                target.DescriptionIsExcluded = descriptionIsExcluded;
            if (source.Description is string description)
                target.Description = description;
            if (source.Filter is string filter)
                target.Filter = filter;
            if (source.FilterNonStrict is string filterNonStrict)
                target.FilterNonStrict = filterNonStrict;
        }

        /// <summary>
        /// Transforms a <see cref="ApiKeyLogEventLevel"/> to a <see cref="LogEventLevel"/>
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        LogEventLevel ToApi(ApiKeyLogEventLevel level) => level switch
        {
            ApiKeyLogEventLevel.Verbose => LogEventLevel.Verbose,
            ApiKeyLogEventLevel.Debug => LogEventLevel.Debug,
            ApiKeyLogEventLevel.Information => LogEventLevel.Information,
            ApiKeyLogEventLevel.Warning => LogEventLevel.Warning,
            ApiKeyLogEventLevel.Error => LogEventLevel.Error,
            ApiKeyLogEventLevel.Fatal => LogEventLevel.Fatal,
            _ => throw new NotImplementedException(),
        };

    }

}
