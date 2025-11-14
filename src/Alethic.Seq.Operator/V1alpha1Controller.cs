using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Instance;
using Alethic.Seq.Operator.Options;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Seq.Api;
using Seq.Api.Client;

namespace Alethic.Seq.Operator
{

    public abstract class V1alpha1Controller<TEntity, TSpec, TStatus, TConf, TInfo> : IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>, V1alpha1Entity<TSpec, TStatus, TConf, TInfo>
        where TSpec : V1alpha1EntitySpec<TConf>
        where TStatus : V1alpha1EntityStatus<TInfo>
        where TConf : class
        where TInfo : class
    {

        readonly IKubernetesClient _kube;
        readonly EntityRequeue<TEntity> _requeue;
        readonly IMemoryCache _cache;
        readonly IOptions<OperatorOptions> _options;
        readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public V1alpha1Controller(IKubernetesClient kube, EntityRequeue<TEntity> requeue, IMemoryCache cache, IOptions<OperatorOptions> options, ILogger logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _options = options ?? throw new ArgumentNullException(nameof(options));
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
        /// Gets a memory cache that can be used for simple values.
        /// </summary>
        protected IMemoryCache Cache => _cache;

        /// <summary>
        /// Gets the operator options.
        /// </summary>
        protected OperatorOptions Options => _options.Value;

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
        protected async Task<V1Secret?> ResolveSecretRef(V1SecretReference? secretRef, string defaultNamespace, CancellationToken cancellationToken)
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
        protected async Task<V1alpha1Instance?> ResolveInstanceRef(V1alpha1InstanceReference? instanceRef, string defaultNamespace, CancellationToken cancellationToken)
        {
            if (instanceRef is null)
                return null;

            if (string.IsNullOrWhiteSpace(instanceRef.Name))
                throw new InvalidOperationException($"Instance reference {instanceRef} has no name.");

            var ns = instanceRef.Namespace ?? defaultNamespace;
            if (string.IsNullOrWhiteSpace(ns))
                throw new InvalidOperationException($"Instance reference {instanceRef} has no discovered namesace.");

            var tenant = await _kube.GetAsync<V1alpha1Instance>(instanceRef.Name, ns, cancellationToken);
            if (tenant is null)
                throw new RetryException($"Instance reference {instanceRef} cannot be resolved.");

            return tenant;
        }

        /// <summary>
        /// Tests that the conncetion is able to retrieve server status.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<bool> TestConnectionAsync(SeqConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                await connection.Diagnostics.GetServerStatusAsync(cancellationToken);
                return true;
            }
            catch (SeqApiException e)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a new instance connection for the Login authentication method.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="endpoint"></param>
        /// <param name="secretRef"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="RetryException"></exception>
        async Task<SeqConnection?> CreateSeqLoginConnectionAsync(V1alpha1Instance instance, string endpoint, V1SecretReference secretRef, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(secretRef.Name))
            {
                Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} has no login secret name.");
                return null;
            }

            var secret = _kube.Get<V1Secret>(secretRef.Name, secretRef.NamespaceProperty ?? instance.Namespace());
            if (secret == null)
            {
                Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} has missing login secret.");
                return null;
            }

            if (secret.Data.TryGetValue("username", out var usernameBuf) == false)
            {
                Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} has missing login username value on secret.");
                return null;
            }

            if (secret.Data.TryGetValue("password", out var passwordBuf) == false)
            {
                Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} has missing login password value on secret.");
                return null;
            }

            // create connection
            var connection = new SeqConnection(endpoint);

            // initiate login
            try
            {
                var user = await connection.Users.LoginAsync(
                    Encoding.UTF8.GetString(usernameBuf),
                    Encoding.UTF8.GetString(passwordBuf),
                    cancellationToken);
                if (user is null)
                    throw new InvalidOperationException("No user returned.");

                // test that connection is usable
                if (await TestConnectionAsync(connection, cancellationToken) == false)
                {
                    Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} was unable to authenticate with password.");
                    return null;
                }
            }
            catch (SeqApiException e) when (e.StatusCode == HttpStatusCode.BadRequest && e.Message.Contains("Invalid", StringComparison.InvariantCultureIgnoreCase))
            {
                // if we cannot authenticate with the primary password, we might have 'firstRun' password available
                // if we authenticate with the firstRun password, we should be sure to change the password to the normal one as soon as we succeed

                if (secret.Data.TryGetValue("firstRun", out var firstRunBuf) == false)
                {
                    Logger.LogError(e, $"Instance {instance.Namespace()}/{instance.Name()} has was unable to authenticate with password, and firstRun was not present.");
                    return null;
                }

                try
                {
                    // try again with the firstRun
                    var user = await connection.Users.LoginAsync(
                        Encoding.UTF8.GetString(usernameBuf),
                        Encoding.UTF8.GetString(firstRunBuf),
                        cancellationToken);
                    if (user is null)
                        throw new InvalidOperationException("No user returned.");

                    // test that connection is usable
                    if (await TestConnectionAsync(connection, cancellationToken) == false)
                    {
                        Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} was unable to authenticate with firstRun password.");
                        return null;
                    }

                    try
                    {
                        // attempt to update password
                        user.NewPassword = Encoding.UTF8.GetString(passwordBuf);
                        await connection.Users.UpdateAsync(user, cancellationToken);

                    }
                    catch (SeqApiException e2)
                    {
                        Logger.LogError(e2, $"Instance {instance.Namespace()}/{instance.Name()} was unable to update password after authenticating with firstRun password.");
                        return null;
                    }
                }
                catch (SeqApiException e2)
                {
                    Logger.LogError(e2, $"Instance {instance.Namespace()}/{instance.Name()} was unable to authenticate with firstRun password.");
                    return null;
                }
            }

            return connection;
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
        async Task<SeqConnection?> CreateSeqLoginConnectionAsync(V1alpha1Instance instance, string endpoint, InstanceLoginRemoteAuthenticationSpec loginDef, CancellationToken cancellationToken)
        {
            var secretRef = loginDef.SecretRef;
            if (secretRef == null)
            {
                Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} has no login authentication secret.");
                return null;
            }

            return await CreateSeqLoginConnectionAsync(instance, endpoint, secretRef, cancellationToken);
        }

        /// <summary>
        /// Creates a new Seq connection for the token authentication method.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="endpoint"></param>
        /// <param name="secretRef"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="RetryException"></exception>
        async Task<SeqConnection?> CreateSeqTokenConnectionAsync(V1alpha1Instance instance, string endpoint, V1SecretReference secretRef, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(secretRef.Name))
            {
                Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} has no secret name.");
                return null;
            }

            var secret = _kube.Get<V1Secret>(secretRef.Name, secretRef.NamespaceProperty ?? instance.Namespace());
            if (secret == null)
            {
                Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} has missing secret.");
                return null;
            }

            if (secret.Data.TryGetValue("token", out var tokenBuf) == false || tokenBuf.Length == 0)
            {
                Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} has missing token value on secret.");
                return null;
            }

            // create connection
            var connection = new SeqConnection(endpoint, Encoding.UTF8.GetString(tokenBuf));

            // test that connection is usable
            if (await TestConnectionAsync(connection, cancellationToken) == false)
            {
                Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} was unable to authenticate with token.");
                return null;
            }

            return connection;
        }

        /// <summary>
        /// Creates a new Seq connection for the ApiKey authentication method.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="endpoint"></param>
        /// <param name="tokenDef"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="RetryException"></exception>
        async Task<SeqConnection?> CreateSeqTokenConnectionAsync(V1alpha1Instance instance, string endpoint, InstanceTokenRemoteAuthenticationSpec tokenDef, CancellationToken cancellationToken)
        {
            var secretRef = tokenDef.SecretRef;
            if (secretRef == null)
            {
                Logger.LogError($"Instance {instance.Namespace()}/{instance.Name()} has no token authentication secret.");
                return null;
            }

            return await CreateSeqTokenConnectionAsync(instance, endpoint, secretRef, cancellationToken);
        }

        /// <summary>
        /// Creates a new Seq connection for the specified instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        async Task<SeqConnection?> CreateSeqConnectionAsync(V1alpha1Instance instance, InstanceRemoteSpec remote, CancellationToken cancellationToken)
        {
            var endpoint = remote.Endpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has no remote endpoint.");

            if (remote.Authentication is null || remote.Authentication is null || remote.Authentication.Length == 0)
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} has no remote authentication information.");

            // we try the proposed connection items in order to allow firstRun
            foreach (var connectionDef in remote.Authentication)
            {
                // this is a token connection
                if (connectionDef.Token != null)
                {
                    var connection = await CreateSeqTokenConnectionAsync(instance, endpoint, connectionDef.Token, cancellationToken);
                    if (connection is not null)
                        return connection;
                }

                // this is a login connection
                if (connectionDef.Login != null)
                {
                    var connection = await CreateSeqLoginConnectionAsync(instance, endpoint, connectionDef.Login, cancellationToken);
                    if (connection is not null)
                        return connection;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets an active <see cref="SeqConnection"/> for the specified instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task<SeqConnection> GetInstanceConnectionAsync(V1alpha1Instance instance, CancellationToken cancellationToken)
        {
            var connection = await _cache.GetOrCreateAsync((typeof(V1alpha1Controller<,,,,>), nameof(GetInstanceConnectionAsync), instance.Namespace(), instance.Name()), async entry =>
            {
                // use specified remote connection(s) if present
                if (instance.Spec.Remote is not null)
                {
                    // search for connection from available connection options
                    var connection = await CreateSeqConnectionAsync(instance, instance.Spec.Remote, cancellationToken);
                    if (connection is not null)
                    {
                        // cache connection for 1 minute
                        entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                        return connection;
                    }
                }

                // otherwise, fall back to deployed instance
                if (instance.Status.Deployment is { Endpoint: string endpoint } && string.IsNullOrWhiteSpace(endpoint) == false)
                {
                    // override connection to deployed endpoint
                    if (instance.Spec.Deployment is { Endpoint: string alternateEndpoint } && string.IsNullOrWhiteSpace(alternateEndpoint) == false)
                        endpoint = alternateEndpoint;

                    // a token is available from the deployment
                    if (instance.Status.Deployment.TokenSecretRef is { } tokenSecretRef)
                    {
                        var connection = await CreateSeqTokenConnectionAsync(instance, endpoint, tokenSecretRef, cancellationToken);
                        if (connection is not null)
                        {
                            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                            return connection;
                        }
                    }

                    // a login is available from the deployment
                    if (instance.Status.Deployment.LoginSecretRef is { } adminSecretRef)
                    {
                        var connection = await CreateSeqLoginConnectionAsync(instance, endpoint, adminSecretRef, cancellationToken);
                        if (connection is not null)
                        {
                            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                            return connection;
                        }
                    }
                }

                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} could not retrieve connection.");
            });

            if (connection is null)
                throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} could not retrieve connection.");

            return connection;
        }

        /// <summary>1
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
        protected abstract Task<TEntity> Reconcile(TEntity entity, CancellationToken cancellationToken);

        /// <inheritdoc />
        public async Task ReconcileAsync(TEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                // set initial ready status
                var ready = entity.Status.Conditions.FirstOrDefault(i => i.Type == "Ready");
                if (ready is null)
                {
                    entity.Status.Conditions.Add(ready = new V1alpha1Condition() { Type = "Ready", Status = "False" });
                    entity = await _kube.UpdateStatusAsync(entity, cancellationToken);
                }

                // set initial healthy status
                var healthy = entity.Status.Conditions.FirstOrDefault(i => i.Type == "Healthy");
                if (healthy is null)
                {
                    entity.Status.Conditions.Add(healthy = new V1alpha1Condition() { Type = "Healthy", Status = "False" });
                    entity = await _kube.UpdateStatusAsync(entity, cancellationToken);
                }

                // does the actual work of reconciling
                entity = await Reconcile(entity, cancellationToken);
                await ReconcileSuccessAsync(entity, cancellationToken);

                // update conditions
                entity = await UpdateStatusAsync(entity, "Ready", "True", null, null, cancellationToken);
                entity = await UpdateStatusAsync(entity, "Healthy", "True", null, null, cancellationToken);

                // schedule periodic reconciliation to detect external changes (e.g., manual deletion from Seq)
                var interval = _options.Value.Reconciliation.Interval;
                Logger.LogDebug("{EntityTypeName} {Namespace}/{Name} scheduling next reconciliation in {IntervalSeconds}s", EntityTypeName, entity.Namespace(), entity.Name(), interval.TotalSeconds);
                Requeue(entity, interval);
            }
            catch (SeqApiException e)
            {
                try
                {
                    Logger.LogError("Seq error hit reconciling {EntityTypeName} {EntityNamespace}/{EntityName}: {Message}", EntityTypeName, entity.Namespace(), entity.Name(), e.Message);
                    await ReconcileWarningAsync(entity, "Retry", e.Message, cancellationToken);

                    // update conditions
                    entity = await UpdateStatusAsync(entity, "Ready", "False", e.Message, null, cancellationToken);
                    entity = await UpdateStatusAsync(entity, "Healthy", "False", e.Message, null, cancellationToken);
                }
                catch (Exception e2)
                {
                    Logger.LogCritical(e2, "Unexpected exception creating event.");
                }

                var interval = _options.Value.Reconciliation.RetryInterval;
                Logger.LogDebug("{EntityTypeName} {Namespace}/{Name} rescheduling next reconciliation in {IntervalSeconds}s", EntityTypeName, entity.Namespace(), entity.Name(), interval.TotalSeconds);
                Requeue(entity, interval);
            }
            catch (RetryException e)
            {
                try
                {
                    Logger.LogError("Retry hit reconciling {EntityTypeName} {EntityNamespace}/{EntityName}: {Message}", EntityTypeName, entity.Namespace(), entity.Name(), e.Message);
                    await ReconcileWarningAsync(entity, "Retry", e.Message, cancellationToken);

                    // update conditions
                    entity = await UpdateStatusAsync(entity, "Ready", "False", e.Message, null, cancellationToken);
                    entity = await UpdateStatusAsync(entity, "Healthy", "False", e.Message, null, cancellationToken);
                }
                catch (Exception e2)
                {
                    Logger.LogCritical(e2, "Unexpected exception creating event.");
                }

                var interval = _options.Value.Reconciliation.RetryInterval;
                Logger.LogDebug("{EntityTypeName} {Namespace}/{Name} rescheduling next reconciliation in {IntervalSeconds}s", EntityTypeName, entity.Namespace(), entity.Name(), interval.TotalSeconds);
                Requeue(entity, interval);
            }
            catch (Exception e)
            {
                try
                {
                    Logger.LogError(e, "Unexpected exception reconciling {EntityTypeName} {EntityNamespace}/{EntityName}: {Message}", EntityTypeName, entity.Namespace(), entity.Name(), e.Message);
                    await ReconcileWarningAsync(entity, "Error", e.Message, cancellationToken);

                    // update conditions
                    entity = await UpdateStatusAsync(entity, "Ready", "False", e.Message, null, cancellationToken);
                    entity = await UpdateStatusAsync(entity, "Healthy", "False", e.Message, null, cancellationToken);
                }
                catch (Exception e2)
                {
                    Logger.LogCritical(e2, "Unexpected exception creating event.");
                }

                var interval = _options.Value.Reconciliation.RetryInterval;
                Logger.LogDebug("{EntityTypeName} {Namespace}/{Name} rescheduling next reconciliation in {IntervalSeconds}s", EntityTypeName, entity.Namespace(), entity.Name(), interval.TotalSeconds);
                Requeue(entity, interval);
            }
        }

        /// <inheritdoc />
        public abstract Task DeletedAsync(TEntity entity, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the specified status type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="status"></param>
        /// <param name="error"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        async Task<TEntity> UpdateStatusAsync(TEntity entity, string type, string status, string? error, string? message, CancellationToken cancellationToken)
        {
            var condition = entity.Status.Conditions.FirstOrDefault(i => i.Type == type);
            if (condition is null)
                entity.Status.Conditions.Add(condition = new V1alpha1Condition());

            condition.Type = type;
            condition.Status = status;
            condition.Error = error;
            condition.Message = message;
            return await _kube.UpdateStatusAsync(entity, cancellationToken);
        }

    }

}
