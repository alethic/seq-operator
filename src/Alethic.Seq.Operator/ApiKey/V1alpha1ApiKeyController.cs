using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Instance;
using Alethic.Seq.Operator.Options;
using Alethic.Seq.Operator.Shared;

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

namespace Alethic.Seq.Operator.ApiKey
{

    [EntityRbac(typeof(V1alpha1Instance), Verbs = RbacVerb.List | RbacVerb.Get)]
    [EntityRbac(typeof(V1alpha1ApiKey), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1Secret), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(Eventsv1Event), Verbs = RbacVerb.All)]
    public partial class V1alpha1ApiKeyController :
        V1alpha1InstanceEntityController<V1alpha1ApiKey, V1alpha1ApiKeySpec, V1alpha1ApiKeyStatus, ApiKeyConf, ApiKeyInfo>,
        IEntityController<V1alpha1ApiKey>
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
        public V1alpha1ApiKeyController(IKubernetesClient kube, EntityRequeue<V1alpha1ApiKey> requeue, IMemoryCache cache, LookupService lookup, IOptions<OperatorOptions> options, ILogger<V1alpha1ApiKeyController> logger) :
            base(kube, requeue, cache, lookup, options, logger)
        {

        }

        /// <inheritdoc />
        protected override string EntityTypeName => "ApiKey";

        /// <summary>
        /// Returns <c>true</c> if this ApiKey is the deployment token which has automatic permissions. This token has all permissions always.
        /// </summary>
        /// <returns></returns>
        bool IsDeploymentToken(V1alpha1Instance instance, V1alpha1ApiKey entity)
        {
            // remote instance can never have an admin token
            if (instance.Spec.Remote is not null)
                return false;

            // match to expected admin entity name/namespace
            var adminApiKeyNamespace = instance.Spec.Deployment?.TokenSecretRef?.NamespaceProperty ?? instance.Namespace();
            var adminApiKeyName = instance.Name();
            return entity.Namespace() == adminApiKeyNamespace && entity.Name() == adminApiKeyName;
        }

        /// <inheritdoc />
        protected override async Task<bool> CanAttachFromAsync(V1alpha1Instance instance, V1alpha1ApiKey entity, CancellationToken cancellationToken)
        {
            return IsDeploymentToken(instance, entity) || await instance.CheckPermissionAsync(Lookup, entity, false, p => p.ApiKeys?.Attach, cancellationToken);
        }

        /// <inheritdoc />
        protected override async Task<bool> CanCreateFromAsync(V1alpha1Instance instance, V1alpha1ApiKey entity, CancellationToken cancellationToken)
        {
            return IsDeploymentToken(instance, entity) || await instance.CheckPermissionAsync(Lookup, entity, false, p => p.ApiKeys?.Create, cancellationToken);
        }

        /// <summary>
        /// Returns <c>true</c> if this ApiKey can set the title.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        async Task<bool> CanSetTitleAsync(V1alpha1Instance instance, V1alpha1ApiKey entity, CancellationToken cancellationToken)
        {
            return IsDeploymentToken(instance, entity) || await instance.CheckPermissionAsync(Lookup, entity, false, p => p.ApiKeys?.SetTitle, cancellationToken);
        }

        /// <summary>
        /// Searches for an existing entity by title.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="api"></param>
        /// <param name="title"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<string?> FindByTitleAsync(V1alpha1ApiKey entity, SeqConnection api, string title, string defaultNamespace, CancellationToken cancellationToken)
        {
            try
            {
                var apiKeys = (IEnumerable<ApiKeyEntity>)await api.ApiKeys.ListAsync(null, shared: true, cancellationToken: cancellationToken);
                var apiKey = apiKeys.FirstOrDefault(i => i.Title == title);
                if (apiKey is null)
                {
                    Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} could not find ApiKey with title {Title}.", EntityTypeName, entity.Namespace(), entity.Name(), title);
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

        /// <inheritdoc />
        protected override async Task<string?> FindAsync(V1alpha1ApiKey entity, SeqConnection api, V1alpha1ApiKeySpec spec, string defaultNamespace, CancellationToken cancellationToken)
        {
            // no manually specified title
            // this would result in an auto generated title, which we can search on safely
            if (spec.Conf is { Title: null })
                return await FindByTitleAsync(entity, api, "SeqOperatorApiKey_" + entity.Uid(), defaultNamespace, cancellationToken);

            return null;
        }

        /// <inheritdoc />
        protected override async Task<ApiKeyInfo?> GetAsync(V1alpha1ApiKey entity, SeqConnection api, string id, string defaultNamespace, CancellationToken cancellationToken)
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
        protected override async Task<string?> ValidateUpdateAsync(V1alpha1Instance instance, V1alpha1ApiKey entity, ApiKeyConf? conf, CancellationToken cancellationToken)
        {
            var ns = await Lookup.ResolveNamespaceAsync(entity.Namespace(), cancellationToken);
            if (ns is null)
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} is invalid: cannot retrieve namespace.");

            var setTitle = await CanSetTitleAsync(instance, entity, cancellationToken);
            if (setTitle == false && conf?.Title != null)
                return "title cannot be set explicitly";

            return null;
        }

        /// <inheritdoc />
        protected override async Task<string> CreateAsync(V1alpha1Instance instance, V1alpha1ApiKey entity, SeqConnection api, ApiKeyConf? conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            // create a new entity
            var create = new ApiKeyEntity();
            ApplyToApi(entity, create, conf, null);

            // entity specifies an existing secret, this may be a token source
            if (entity.Spec.SecretRef is not null)
            {
                var secret = await Lookup.ResolveSecretRefAsync(entity.Spec.SecretRef, entity.Spec.SecretRef.NamespaceProperty ?? defaultNamespace, cancellationToken);
                if (secret is not null)
                    if (secret.Data.TryGetValue("token", out var tokenBuf))
                        create.Token = Encoding.UTF8.GetString(tokenBuf);
            }

            // submit the new key to the API
            var self = await api.ApiKeys.AddAsync(create, cancellationToken);

            // update the secret with the token, which may be from the server
            // todo we need to find a clean way to not do this here, or to propigate out the changes
            await ApplySecretAsync(entity, self.Token ?? create.Token, defaultNamespace, cancellationToken);

            // newly created ID
            return self.Id;
        }

        /// <inheritdoc />
        protected override async Task UpdateAsync(V1alpha1Instance instance, V1alpha1ApiKey entity, SeqConnection api, string id, ApiKeyInfo? info, ApiKeyConf? conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            await api.ApiKeys.UpdateAsync(ApplyToApi(entity, await api.ApiKeys.FindAsync(id, cancellationToken), conf, info), cancellationToken);
        }

        /// <inheritdoc />
        protected override async Task DeleteAsync(V1alpha1Instance instance, SeqConnection api, string id, CancellationToken cancellationToken)
        {
            await api.ApiKeys.RemoveAsync(await api.ApiKeys.FindAsync(id, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Queries for the related object with the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="apikey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<T?> FindRelatedAsync<T>(V1alpha1ApiKey apikey, CancellationToken cancellationToken)
            where T : k8s.IKubernetesObject<V1ObjectMeta>
        {
            var l = await Kube.ListAsync<T>(apikey.Namespace(), $"seq.k8s.datalust.co/apikey={apikey.Name()}", cancellationToken);
            return l.FirstOrDefault();
        }

        /// <summary>
        /// Applies the client secret.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task ApplySecretAsync(V1alpha1ApiKey entity, string? token, string defaultNamespace, CancellationToken cancellationToken)
        {
            var secretName = entity.Spec.SecretRef?.Name ?? entity.Name() + "-seq-apikey";
            var secretNamespace = entity.Spec.SecretRef?.NamespaceProperty ?? entity.Namespace();
            if (secretNamespace != entity.Namespace())
                throw new RetryException($"ApiKey {entity.Namespace()}/{entity.Name()} could not deploy: Secret must be in same namespace as ApiKey.");

            // find existing secret or create
            var secret = await FindRelatedAsync<V1Secret>(entity, cancellationToken);

            // we have an existing secret, owned by us
            if (secret is not null && secret.IsOwnedBy(entity))
            {
                // existing secret does not match our specification
                if (secret.Name() != secretName || secret.Namespace() != secretNamespace)
                {
                    await Kube.DeleteAsync(secret, cancellationToken);
                    secret = null;
                }
            }

            // no secret remaining, but we need one
            if (secret is null)
            {
                Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} referenced secret {SecretName} which does not exist: creating.", EntityTypeName, entity.Namespace(), entity.Name(), secretName);
                secret = await Kube.CreateAsync(
                    new V1Secret(
                            metadata: new V1ObjectMeta(
                                namespaceProperty: secretNamespace ?? defaultNamespace,
                                name: secretName,
                                labels: new Dictionary<string, string>()
                                {
                                    ["seq.k8s.datalust.co/apikey"] = entity.Name(),
                                }))
                        .WithOwnerReference(entity),
                    cancellationToken);
            }

            // only apply actual values if we are the owner
            if (secret.IsOwnedBy(entity))
            {
                Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} referenced secret {SecretName}: updating.", EntityTypeName, entity.Namespace(), entity.Name(), secret.Name());
                secret.StringData ??= new Dictionary<string, string>();

                // ensure the key exists, possible empty, if we're retrieving an existing ApiKey
                if (token is not null)
                {
                    secret.StringData["token"] = token;
                    Logger.LogDebug("{EntityTypeName} {EntityNamespace}/{EntityName} updated secret {SecretName} with token", EntityTypeName, entity.Namespace(), entity.Name(), secret.Name());
                }
                else if (!secret.StringData.ContainsKey("apikey"))
                {
                    secret.StringData["token"] = "";
                    Logger.LogDebug("{EntityTypeName} {EntityNamespace}/{EntityName} initialized empty token in secret {SecretName}", EntityTypeName, entity.Namespace(), entity.Name(), secret.Name());
                }

                secret = await Kube.UpdateAsync(secret, cancellationToken);
                Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} successfully updated secret {SecretName}", EntityTypeName, entity.Namespace(), entity.Name(), secret.Name());
            }
            else
            {
                Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} secret {SecretName} exists but is not owned by this ApiKey, skipping update", EntityTypeName, entity.Namespace(), entity.Name(), secret.Name());
            }

            // apply the reference to the secret to the apikey
            entity.Status.SecretRef = new V1SecretReference(secret.Name(), secret.Namespace());
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
        /// Creates an <see cref="ApiKeyEntity"/> for creating or updating.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="target"></param>
        /// <param name="conf"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        ApiKeyEntity ApplyToApi(V1alpha1ApiKey entity, ApiKeyEntity target, ApiKeyConf? conf, ApiKeyInfo? info)
        {
            var title = conf?.Title ?? "SeqOperatorApiKey_" + entity.Uid();
            if (title is not null)
                if (info == null || info.Title != title)
                    target.Title = title;

            if (conf?.Permissions != null)
                ApplyToApi(target, conf.Permissions);
            else if (info != null && info.Permissions != null)
                ApplyToApi(target, info.Permissions);

            if (conf?.InputSettings != null)
                ApplyToApi(target.InputSettings, conf.InputSettings);
            else if (info != null && info.InputSettings != null)
                ApplyToApi(target.InputSettings, info.InputSettings);

            return target;
        }

        /// <summary>
        /// Transforms the entity version of <see cref="ApiKeyPermission"/> into the API version.
        /// </summary>
        /// <param name="target"></param>
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

            if (source.MinimumLevel is Shared.LogEventLevel l)
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
        /// Transforms a <see cref="LogEventLevel"/> to a <see cref="LogEventLevel"/>
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        global::Seq.Api.Model.LogEvents.LogEventLevel ToApi(Shared.LogEventLevel level) => level switch
        {
            Shared.LogEventLevel.Verbose => global::Seq.Api.Model.LogEvents.LogEventLevel.Verbose,
            Shared.LogEventLevel.Debug => global::Seq.Api.Model.LogEvents.LogEventLevel.Debug,
            Shared.LogEventLevel.Information => global::Seq.Api.Model.LogEvents.LogEventLevel.Information,
            Shared.LogEventLevel.Warning => global::Seq.Api.Model.LogEvents.LogEventLevel.Warning,
            Shared.LogEventLevel.Error => global::Seq.Api.Model.LogEvents.LogEventLevel.Error,
            Shared.LogEventLevel.Fatal => global::Seq.Api.Model.LogEvents.LogEventLevel.Fatal,
            _ => throw new NotImplementedException(),
        };

    }

}
