using System;
using System.Linq;
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

using Seq.Api;
using Seq.Api.Model.Settings;

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
            var api = await GetInstanceConnectionAsync(entity, cancellationToken);
            if (api == null)
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}:{entity.Name()} failed to retrieve API client.");

            // get existing info
            var info = await GetInfoAsync(api, cancellationToken);
            if (info is null)
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} cannot be loaded from API.");

            // configuration was specified
            if (entity.Spec.Conf is { } conf)
            {
                await PutConfAsync(api, info, conf, cancellationToken);
                info = await GetInfoAsync(api, cancellationToken);
            }

            // retrieve and copy applied settings to status
            entity.Status.Info = info;
            entity = await Kube.UpdateStatusAsync(entity, cancellationToken);

            await ReconcileSuccessAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public override Task DeletedAsync(V1Alpha1Instance entity, CancellationToken cancellationToken)
        {
            Logger.LogWarning("Unsupported operation deleting entity {Entity}.", entity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Queries for the specified setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="api"></param>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<T?> GetSettingValueAsync<T>(SeqConnection api, SettingName name, CancellationToken cancellationToken)
        {
            var setting = await api.Settings.FindNamedAsync(SettingName.AutomaticAccessADGroup, cancellationToken);
            return (T)setting.Value;
        }

        /// <summary>
        /// Gets the various info for the entity from the API.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        async Task<InstanceInfo> GetInfoAsync(SeqConnection api, CancellationToken cancellationToken)
        {
            var info = new InstanceInfo();
            info.Settings = new InstanceSettings();
            info.Settings.Auth = new InstanceSettings.AuthConf();
            await ApplyAuthSettingsAsync(api, info.Settings.Auth, cancellationToken);
            info.Settings.DataAgeWarningThresholdMilliseconds = await GetSettingValueAsync<int>(api, SettingName.DataAgeWarningThresholdMilliseconds, cancellationToken);
            // more
            return info;
        }

        /// <summary>
        /// Gets the authentication settings from the API and outputs them to the <see cref="InstanceSettings.AuthConf"/> object.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="info"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task ApplyAuthSettingsAsync(SeqConnection api, InstanceSettings.AuthConf info, CancellationToken cancellationToken)
        {
            if (await GetSettingValueAsync<bool>(api, SettingName.IsAuthenticationEnabled, cancellationToken))
            {
                switch (await GetSettingValueAsync<string>(api, SettingName.AuthenticationProvider, cancellationToken))
                {
                    case null:
                        info.Local = new InstanceSettings.AuthConf.LocalAuthConf();
                        break;
                    case "Active Directory":
                        info.ActiveDirectory = new InstanceSettings.AuthConf.ActiveDirectoryAuthConf();
                        info.ActiveDirectory.AutomaticAccessADGroup = await GetSettingValueAsync<string>(api, SettingName.AutomaticAccessADGroup, cancellationToken);
                        info.AutomaticallyProvisionAuthenticatedUsers = await GetSettingValueAsync<bool>(api, SettingName.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);
                        break;
                    case "Microsoft Entra ID":
                        info.Entra = new InstanceSettings.AuthConf.EntraAuthConf();
                        info.Entra.Authority = await GetSettingValueAsync<string>(api, SettingName.EntraIDAuthority, cancellationToken);
                        info.Entra.TenantId = await GetSettingValueAsync<string>(api, SettingName.EntraIDTenantId, cancellationToken);
                        info.Entra.ClientId = await GetSettingValueAsync<string>(api, SettingName.EntraIDClientId, cancellationToken);
                        info.Entra.ClientKey = await GetSettingValueAsync<string>(api, SettingName.EntraIDClientKey, cancellationToken);
                        info.AutomaticallyProvisionAuthenticatedUsers = await GetSettingValueAsync<bool>(api, SettingName.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);
                        return;
                    case "OpenID Connect":
                        info.Oidc = new InstanceSettings.AuthConf.OidcAuthConf();
                        info.Oidc.Authority = await GetSettingValueAsync<string>(api, SettingName.OpenIdConnectAuthority, cancellationToken);
                        info.Oidc.ClientId = await GetSettingValueAsync<string>(api, SettingName.OpenIdConnectClientId, cancellationToken);
                        info.Oidc.ClientSecret = await GetSettingValueAsync<string>(api, SettingName.OpenIdConnectClientSecret, cancellationToken);
                        info.Oidc.Scopes = await GetSettingValueAsync<string[]>(api, SettingName.OpenIdConnectScopes, cancellationToken);
                        info.Oidc.MetadataAddress = await GetSettingValueAsync<string>(api, SettingName.OpenIdConnectMetadataAddress, cancellationToken);
                        info.AutomaticallyProvisionAuthenticatedUsers = await GetSettingValueAsync<bool>(api, SettingName.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);
                        break;
                }
            }
        }

        /// <summary>
        /// Queries for the specified setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="api"></param>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PutSettingValueAsync<T>(SeqConnection api, SettingName name, T? value, CancellationToken cancellationToken)
        {
            await api.Settings.UpdateAsync(new SettingEntity() { Name = name.ToString(), Value = value }, cancellationToken);
        }

        /// <summary>
        /// Puts the configuration to the API.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        async Task PutConfAsync(SeqConnection api, InstanceInfo? info, InstanceConf conf, CancellationToken cancellationToken)
        {
            if (conf.Settings is not null)
            {
                if (conf.Settings.Auth is not null)
                {
                    await PutAuthSettingsAsync(api, info?.Settings?.Auth, conf.Settings.Auth, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Puts the given authentication configuration to the API.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="info"></param>
        /// <param name="conf"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PutAuthSettingsAsync(SeqConnection api, InstanceSettings.AuthConf? info, InstanceSettings.AuthConf conf, CancellationToken cancellationToken)
        {
            if (conf.Local is { } local)
            {
                // only apply if existing authentication mode is not local
                if (info is null or { Local: null })
                {
                    await PutSettingValueAsync(api, SettingName.IsAuthenticationEnabled, true, cancellationToken);
                    await PutSettingValueAsync<string>(api, SettingName.AuthenticationProvider, null, cancellationToken);
                }

                return;
            }

            if (conf.ActiveDirectory is { } activeDirectory)
            {
                if (info is null or { ActiveDirectory: null })
                {
                    await PutSettingValueAsync(api, SettingName.IsAuthenticationEnabled, true, cancellationToken);
                    await PutSettingValueAsync(api, SettingName.AuthenticationProvider, "Active Directory", cancellationToken);
                }

                if (activeDirectory.AutomaticAccessADGroup is not null)
                    if (info?.ActiveDirectory?.AutomaticAccessADGroup != activeDirectory.AutomaticAccessADGroup)
                        await PutSettingValueAsync(api, SettingName.AutomaticAccessADGroup, activeDirectory.AutomaticAccessADGroup, cancellationToken);

                if (conf.AutomaticallyProvisionAuthenticatedUsers is not null)
                    if (info?.AutomaticallyProvisionAuthenticatedUsers != conf.AutomaticallyProvisionAuthenticatedUsers)
                        await PutSettingValueAsync(api, SettingName.AutomaticallyProvisionAuthenticatedUsers, conf.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);

                return;
            }

            if (conf.Entra is { } entra)
            {
                if (info is null or { Entra: null })
                {
                    await PutSettingValueAsync(api, SettingName.IsAuthenticationEnabled, true, cancellationToken);
                    await PutSettingValueAsync(api, SettingName.AuthenticationProvider, "Microsoft Entra ID", cancellationToken);
                }

                if (entra.Authority is not null)
                    if (info?.Entra?.Authority != entra.Authority)
                        await PutSettingValueAsync(api, SettingName.EntraIDAuthority, entra.Authority, cancellationToken);

                if (entra.TenantId is not null)
                    if (info?.Entra?.TenantId != entra.TenantId)
                        await PutSettingValueAsync(api, SettingName.EntraIDTenantId, entra.TenantId, cancellationToken);

                if (entra.ClientId is not null)
                    if (info?.Entra?.ClientId != entra.ClientId)
                        await PutSettingValueAsync(api, SettingName.EntraIDClientId, entra.ClientId, cancellationToken);

                if (entra.ClientKey is not null)
                    if (info?.Entra?.ClientKey != entra.ClientKey)
                        await PutSettingValueAsync(api, SettingName.EntraIDClientKey, entra.ClientKey, cancellationToken);

                if (conf.AutomaticallyProvisionAuthenticatedUsers is not null)
                    if (info?.AutomaticallyProvisionAuthenticatedUsers != conf.AutomaticallyProvisionAuthenticatedUsers)
                        await PutSettingValueAsync(api, SettingName.AutomaticallyProvisionAuthenticatedUsers, conf.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);

                return;
            }

            if (conf.Oidc is { } oidc)
            {
                if (info is null or { Oidc: null })
                {
                    await PutSettingValueAsync(api, SettingName.IsAuthenticationEnabled, true, cancellationToken);
                    await PutSettingValueAsync(api, SettingName.AuthenticationProvider, "OpenID Connect", cancellationToken);
                }

                if (oidc.Authority is not null)
                    if (info?.Oidc?.Authority != oidc.Authority)
                        await PutSettingValueAsync(api, SettingName.OpenIdConnectAuthority, oidc.Authority, cancellationToken);

                if (oidc.ClientId is not null)
                    if (info?.Oidc?.ClientId != oidc.ClientId)
                        await PutSettingValueAsync(api, SettingName.OpenIdConnectClientId, oidc.ClientId, cancellationToken);

                if (oidc.ClientSecret is not null)
                    if (info?.Oidc?.ClientSecret != oidc.ClientSecret)
                        await PutSettingValueAsync(api, SettingName.OpenIdConnectClientSecret, oidc.ClientSecret, cancellationToken);

                if (oidc.MetadataAddress is not null)
                    if (info?.Oidc?.MetadataAddress != oidc.MetadataAddress)
                        await PutSettingValueAsync(api, SettingName.OpenIdConnectMetadataAddress, oidc.MetadataAddress, cancellationToken);

                if (oidc.Scopes is not null)
                    if (SetEqual(info?.Oidc?.Scopes, oidc.Scopes) == false)
                        await PutSettingValueAsync(api, SettingName.OpenIdConnectScopes, oidc.Scopes, cancellationToken);

                if (conf.AutomaticallyProvisionAuthenticatedUsers is not null)
                    if (info?.AutomaticallyProvisionAuthenticatedUsers != conf.AutomaticallyProvisionAuthenticatedUsers)
                        await PutSettingValueAsync(api, SettingName.AutomaticallyProvisionAuthenticatedUsers, conf.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);

                return;
            }

            // disable authentication
            await PutSettingValueAsync(api, SettingName.IsAuthenticationEnabled, false, cancellationToken);
        }

        /// <summary>
        /// Returns <c>true</c> if the sets are equal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool SetEqual<T>(T[]? a, T[]? b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is not null && b is null)
                return false;

            if (a is null && b is not null)
                return false;

            return a.ToHashSet().SetEquals(b.ToHashSet());
        }

    }

}
