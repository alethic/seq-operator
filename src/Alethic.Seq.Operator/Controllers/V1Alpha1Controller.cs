using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Core.Models;
using Alethic.Seq.Operator.Models;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Seq.Api;

namespace Alethic.Seq.Operator.Controllers
{

    public abstract class V1Alpha1Controller<TEntity, TSpec, TStatus, TConf, TInfo> : IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>, V1Alpha1Entity<TSpec, TStatus, TConf, TInfo>
        where TSpec : V1Alpha1EntitySpec<TConf>
        where TStatus : V1Alpha1EntityStatus<TInfo>
        where TConf : class
        where TInfo : class
    {

        readonly IKubernetesClient _kube;
        readonly EntityRequeue<TEntity> _requeue;
        readonly IMemoryCache _cache;
        readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="logger"></param>
        public V1Alpha1Controller(IKubernetesClient kube, EntityRequeue<TEntity> requeue, IMemoryCache cache, ILogger logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _kube = kube ?? throw new ArgumentNullException(nameof(kube));
            _requeue = requeue ?? throw new ArgumentNullException(nameof(requeue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the type name of the entity used in messages.
        /// </summary>
        protected abstract string EntityTypeName { get; }

        /// <summary>
        /// Gets the Kubernetes API client.
        /// </summary>
        protected IKubernetesClient Kube => _kube;

        /// <summary>
        /// Gets the requeue function for the entity controller.
        /// </summary>
        protected EntityRequeue<TEntity> Requeue => _requeue;

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger Logger => _logger;

        /// <summary>
        /// Attempts to resolve the secret document referenced by the secret reference.
        /// </summary>
        /// <param name="secretRef"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1Secret?> ResolveSecretRef(V1SecretReference? secretRef, string defaultNamespace, CancellationToken cancellationToken)
        {
            if (secretRef is null)
                return null;

            if (string.IsNullOrWhiteSpace(secretRef.Name))
                throw new InvalidOperationException($"Secret reference {secretRef} has no name.");

            var ns = secretRef.NamespaceProperty ?? defaultNamespace;
            if (string.IsNullOrWhiteSpace(ns))
                throw new InvalidOperationException($"Secret reference {secretRef} has no discovered namesace.");

            var secret = await _kube.GetAsync<V1Secret>(secretRef.Name, ns, cancellationToken);
            if (secret is null)
                return null;

            return secret;
        }

        /// <summary>
        /// Attempts to resolve the instance document referenced by the instance reference.
        /// </summary>
        /// <param name="instanceRef"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1Alpha1Instance?> ResolveInstanceRef(V1Alpha1InstanceReference? instanceRef, string defaultNamespace, CancellationToken cancellationToken)
        {
            if (instanceRef is null)
                return null;

            if (string.IsNullOrWhiteSpace(instanceRef.Name))
                throw new InvalidOperationException($"Instance reference {instanceRef} has no name.");

            var ns = instanceRef.Namespace ?? defaultNamespace;
            if (string.IsNullOrWhiteSpace(ns))
                throw new InvalidOperationException($"Instance reference {instanceRef} has no discovered namesace.");

            var tenant = await _kube.GetAsync<V1Alpha1Instance>(instanceRef.Name, ns, cancellationToken);
            if (tenant is null)
                throw new RetryException($"Instance reference {instanceRef} cannot be resolved.");

            return tenant;
        }

        /// <summary>
        /// Creates a new instance connection for the Login authentication method.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="endpoint"></param>
        /// <param name="loginDef"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="RetryException"></exception>
        async Task<SeqConnection> CreateSeqConnectionAsync(V1Alpha1Instance instance, string endpoint, V1Alpha1Instance.SpecDef.LoginAuthentication loginDef, CancellationToken cancellationToken)
        {
            var secretRef = loginDef.SecretRef;
            if (secretRef == null)
                throw new InvalidOperationException($"Instance {instance.Namespace()}/{instance.Name()} has no login authentication secret.");

            if (string.IsNullOrWhiteSpace(secretRef.Name))
                throw new InvalidOperationException($"Instance {instance.Namespace()}/{instance.Name()} has no secret name.");

            var secret = _kube.Get<V1Secret>(secretRef.Name, secretRef.NamespaceProperty ?? instance.Namespace());
            if (secret == null)
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has missing secret.");

            if (secret.Data.TryGetValue("username", out var usernameBuf) == false)
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has missing username value on secret.");

            if (secret.Data.TryGetValue("password", out var passwordBuf) == false)
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has missing password value on secret.");

            // unpack buffers
            var username = Encoding.UTF8.GetString(usernameBuf);
            var password = Encoding.UTF8.GetString(passwordBuf);

            // create connection and login
            var connection = new SeqConnection(endpoint);
            await connection.Users.LoginAsync(username, password);

            return connection;
        }

        /// <summary>
        /// Creates a new Seq connection for the ApiKey authentication method.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="endpoint"></param>
        /// <param name="apiKeyDef"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="RetryException"></exception>
        async Task<SeqConnection> CreateSeqConnectionAsync(V1Alpha1Instance instance, string endpoint, V1Alpha1Instance.SpecDef.ApiKeyAuthentication apiKeyDef, CancellationToken cancellationToken)
        {
            var secretRef = apiKeyDef.SecretRef;
            if (secretRef == null)
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has no apikey authentication secret.");

            if (string.IsNullOrWhiteSpace(secretRef.Name))
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has no secret name.");

            var secret = _kube.Get<V1Secret>(secretRef.Name, secretRef.NamespaceProperty ?? instance.Namespace());
            if (secret == null)
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has missing secret.");

            if (secret.Data.TryGetValue("apikey", out var apikeyBuf) == false)
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has missing apikey value on secret.");

            // unpack buffers
            var apikey = Encoding.UTF8.GetString(apikeyBuf);

            // create connection
            var connection = new SeqConnection(endpoint, apikey);

            return connection;
        }

        /// <summary>
        /// Creates a new Seq connection for the specified instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        async Task<SeqConnection> CreateSeqConnectionAsync(V1Alpha1Instance instance, V1Alpha1Instance.SpecDef.ConnectionDef remoteDef, CancellationToken cancellationToken)
        {
            var endpoint = remoteDef.Endpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has no remote endpoint.");

            if (remoteDef.Login != null)
                return await CreateSeqConnectionAsync(instance, endpoint, remoteDef.Login, cancellationToken);

            if (remoteDef.ApiKey != null)
                return await CreateSeqConnectionAsync(instance, endpoint, remoteDef.ApiKey, cancellationToken);

            throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has valid authentication information.");
        }

        /// <summary>
        /// Gets an active <see cref="SeqConnection"/> for the specified instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SeqConnection> GetInstanceConnectionAsync(V1Alpha1Instance instance, CancellationToken cancellationToken)
        {
            var connection = await _cache.GetOrCreateAsync((instance.Namespace(), instance.Name()), async entry =>
            {
                if (instance.Spec.Connection is null)
                    throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has no remote connection information.");

                var connection = await CreateSeqConnectionAsync(instance, instance.Spec.Connection, cancellationToken);
                if (connection is null)
                    throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} could not retrieve connection.");

                // cache connection for 1 minute
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                return connection;
            });

            if (connection is null)
                throw new RetryException("Cannot retrieve tenant API client.");

            return connection;
        }

        /// <summary>
        /// Updates the Reconcile event to a warning.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task ReconcileSuccessAsync(TEntity entity, CancellationToken cancellationToken)
        {
            await _kube.CreateAsync(new Eventsv1Event(
                    DateTime.Now,
                    metadata: new V1ObjectMeta(namespaceProperty: entity.Namespace(), generateName: "seq"),
                    reportingController: "seq.k8s.datalust.co/operator",
                    reportingInstance: Dns.GetHostName(),
                    regarding: entity.MakeObjectReference(),
                    action: "Reconcile",
                    type: "Normal",
                    reason: "Success"),
                cancellationToken);
        }

        /// <summary>
        /// Updates the Reconcile event to a warning.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="reason"></param>
        /// <param name="note"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task ReconcileWarningAsync(TEntity entity, string reason, string note, CancellationToken cancellationToken)
        {
            await _kube.CreateAsync(new Eventsv1Event(
                    DateTime.Now,
                    metadata: new V1ObjectMeta(namespaceProperty: entity.Namespace(), generateName: "seq"),
                    reportingController: "seq.k8s.datalust.co/operator",
                    reportingInstance: Dns.GetHostName(),
                    regarding: entity.MakeObjectReference(),
                    action: "Reconcile",
                    type: "Warning",
                    reason: reason,
                    note: note),
                cancellationToken);
        }

        /// <summary>
        /// Updates the Deleting event to a warning.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="reason"></param>
        /// <param name="note"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task DeletingWarningAsync(TEntity entity, string reason, string note, CancellationToken cancellationToken)
        {
            await _kube.CreateAsync(new Eventsv1Event(
                    DateTime.Now,
                    metadata: new V1ObjectMeta(namespaceProperty: entity.Namespace(), generateName: "seq"),
                    reportingController: "seq.k8s.datalust.co/operator",
                    reportingInstance: Dns.GetHostName(),
                    regarding: entity.MakeObjectReference(),
                    action: "Deleting",
                    type: "Warning",
                    reason: reason,
                    note: note),
                cancellationToken);
        }

        /// <summary>
        /// Implement this method to attempt the reconcillation.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        protected abstract Task Reconcile(TEntity entity, CancellationToken cancellationToken);

        /// <inheritdoc />
        public async Task ReconcileAsync(TEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                if (entity.Spec.Conf == null)
                    throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} is missing configuration.");

                // does the actual work of reconciling
                await Reconcile(entity, cancellationToken);
                await ReconcileSuccessAsync(entity, cancellationToken);
            }
            catch (RetryException e)
            {
                try
                {
                    Logger.LogError("Retry hit reconciling {EntityTypeName} {EntityNamespace}/{EntityName}: {Message}", EntityTypeName, entity.Namespace(), entity.Name(), e.Message);
                    await DeletingWarningAsync(entity, "Retry", e.Message, cancellationToken);
                }
                catch (Exception e2)
                {
                    Logger.LogCritical(e2, "Unexpected exception creating event.");
                }

                Logger.LogInformation("Rescheduling reconcilation after {TimeSpan}.", TimeSpan.FromMinutes(1));
                Requeue(entity, TimeSpan.FromMinutes(1));
            }
            catch (Exception e)
            {
                try
                {
                    await ReconcileWarningAsync(entity, "Unknown", e.Message, cancellationToken);
                }
                catch (Exception e2)
                {
                    Logger.LogCritical(e2, "Unexpected exception creating event.");
                }

                throw;
            }
        }

        /// <inheritdoc />
        public abstract Task DeletedAsync(TEntity entity, CancellationToken cancellationToken);

    }

}
