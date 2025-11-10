using System;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Options;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Seq.Api;

namespace Alethic.Seq.Operator
{

    public abstract class V1alpha1InstanceEntityController<TEntity, TSpec, TStatus, TConf, TInfo> : V1alpha1Controller<TEntity, TSpec, TStatus, TConf, TInfo>
        where TEntity : IKubernetesObject<V1ObjectMeta>, V1alpha1InstanceEntity<TSpec, TStatus, TConf, TInfo>
        where TSpec : V1alpha1InstanceEntitySpec<TConf>
        where TStatus : V1alpha1InstanceEntityStatus<TInfo>
        where TConf : class
        where TInfo : class
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public V1alpha1InstanceEntityController(IKubernetesClient kube, EntityRequeue<TEntity> requeue, IMemoryCache cache, IOptions<OperatorOptions> options, ILogger logger) :
            base(kube, requeue, cache, options, logger)
        {

        }

        /// <summary>
        /// Gets the <typeparamref name="TInfo"/> for the entity with the given <paramref name="id"/> or returns <c>null</c>.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="api"></param>
        /// <param name="id"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<TInfo?> GetAsync(TEntity entity, SeqConnection api, string id, string defaultNamespace, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to locate an existing matching entity, or returns <c>null</c>.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="api"></param>
        /// <param name="spec"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task<string?> FindAsync(TEntity entity, SeqConnection api, TSpec spec, string defaultNamespace, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(null);
        }

        /// <summary>
        /// Performs a validation on the <paramref name="conf"/> parameter for usage in create operations.
        /// </summary>
        /// <param name="conf"></param>
        /// <returns></returns>
        protected abstract string? ValidateCreate(TConf conf);

        /// <summary>
        /// Attempts to perform a creation through the API. If successful returns the new ID value.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="api"></param>
        /// <param name="conf"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<string> CreateAsync(TEntity entity, SeqConnection api, TConf conf, string defaultNamespace, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to perform an update through the API.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="api"></param>
        /// <param name="id"></param>
        /// <param name="info"></param>
        /// <param name="conf"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task UpdateAsync(TEntity entity, SeqConnection api, string id, TInfo? info, TConf conf, string defaultNamespace, CancellationToken cancellationToken);

        /// <inheritdoc />
        protected override async Task<TEntity> Reconcile(TEntity entity, CancellationToken cancellationToken)
        {
            if (entity.Spec.InstanceRef is null)
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} missing an instance reference.");

            if (entity.Spec.Conf is null)
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} missing configuration.");

            var instance = await ResolveInstanceRef(entity.Spec.InstanceRef, entity.Namespace(), cancellationToken);
            if (instance is null)
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} missing an instance.");

            var api = await GetInstanceConnectionAsync(instance, cancellationToken);
            if (api is null)
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} failed to retrieve API client.");

            // ensure we hold a reference to the tenant
            var md = entity.EnsureMetadata();
            var an = md.EnsureAnnotations();
            an["seq.k8s.datalust.co/instance-uid"] = instance.Uid();

            // we have not resolved a remote entity
            if (string.IsNullOrWhiteSpace(entity.Status.Id))
            {
                Logger.LogDebug("{EntityTypeName} {Namespace}/{Name} has not yet been reconciled, checking if entity exists in Seq.", EntityTypeName, entity.Namespace(), entity.Name());

                // find existing remote entity
                var entityId = await FindAsync(entity, api, entity.Spec, entity.Namespace(), cancellationToken);
                if (entityId is null)
                {
                    Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} could not be located, creating.", EntityTypeName, entity.Namespace(), entity.Name());

                    // reject creation if disallowed
                    if (entity.HasPolicy(V1alpha1EntityPolicyType.Create) == false)
                    {
                        Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} does not support creation.", EntityTypeName, entity.Namespace(), entity.Name());
                        return entity;
                    }

                    // validate configuration version used for initialization
                    var init = entity.Spec.Init ?? entity.Spec.Conf;
                    if (ValidateCreate(init) is string msg)
                        throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} is invalid: {msg}");

                    // create new entity and associate
                    entity.Status.Id = await CreateAsync(entity, api, init, entity.Namespace(), cancellationToken);
                    Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} created with {Id}", EntityTypeName, entity.Namespace(), entity.Name(), entity.Status.Id);
                }
                else
                {
                    entity.Status.Id = entityId;
                    Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} found with {Id}", EntityTypeName, entity.Namespace(), entity.Name(), entity.Status.Id);
                }

                // save the status
                entity = await Kube.UpdateStatusAsync(entity, cancellationToken);
            }

            // at this point we must have a reference to an entity
            if (string.IsNullOrWhiteSpace(entity.Status.Id))
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} is missing an existing ID.");

            // attempt to retrieve existing entity
            Logger.LogDebug("{EntityTypeName} {Namespace}/{Name} checking if entity exists in Seq with ID {Id}", EntityTypeName, entity.Namespace(), entity.Name(), entity.Status.Id);
            var info = await GetAsync(entity, api, entity.Status.Id, entity.Namespace(), cancellationToken);
            if (info is null)
            {
                // no matching remote entity that correlates directly with ID, reset and retry to go back to Find/Create
                Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} not found in Seq, clearing status and scheduling recreation", EntityTypeName, entity.Namespace(), entity.Name());
                entity.Status.Id = null;
                entity.Status.Info = null;
                entity = await Kube.UpdateStatusAsync(entity, cancellationToken);
                throw new RetryException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} has missing API object, invalidating.");
            }

            // apply updates if allowed
            if (entity.HasPolicy(V1alpha1EntityPolicyType.Update))
            {
                if (entity.Spec.Conf is { } conf)
                    await UpdateAsync(entity, api, entity.Status.Id, info, conf, entity.Namespace(), cancellationToken);
            }
            else
            {
                Logger.LogDebug("{EntityTypeName} {Namespace}/{Name} does not support update.", EntityTypeName, entity.Namespace(), entity.Name());
            }

            // apply new configuration
            await ApplyStatusAsync(entity, api, info, entity.Namespace(), cancellationToken);
            entity = await Kube.UpdateStatusAsync(entity, cancellationToken);
            return entity;
        }

        /// <summary>
        /// Applies any modification to the entity status just before saving it.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="api"></param>
        /// <param name="info"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task ApplyStatusAsync(TEntity entity, SeqConnection api, TInfo? info, string defaultNamespace, CancellationToken cancellationToken)
        {
            entity.Status.Info = info;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Implement this method to delete a specific entity from the API.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task DeleteAsync(SeqConnection api, string id, CancellationToken cancellationToken);

        /// <inheritdoc />
        public override sealed async Task DeletedAsync(TEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                if (entity.Spec.InstanceRef is null)
                    throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} is missing an instance reference.");

                var tenant = await ResolveInstanceRef(entity.Spec.InstanceRef, entity.Namespace(), cancellationToken);
                if (tenant is null)
                    throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} is missing an instance.");

                var api = await GetInstanceConnectionAsync(tenant, cancellationToken);
                if (api is null)
                    throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} failed to retrieve API client.");

                if (string.IsNullOrWhiteSpace(entity.Status.Id))
                {
                    Logger.LogWarning("{EntityTypeName} {EntityNamespace}/{EntityName} has no known ID, skipping delete (reason: entity was never successfully created in Seq).", EntityTypeName, entity.Namespace(), entity.Name());
                    return;
                }

                var self = await GetAsync(entity, api, entity.Status.Id, entity.Namespace(), cancellationToken);
                if (self is null)
                {
                    Logger.LogWarning("{EntityTypeName} {EntityNamespace}/{EntityName} with ID {Id} not found in Seq, skipping delete (reason: already deleted externally).", EntityTypeName, entity.Namespace(), entity.Name(), entity.Status.Id);
                    return;
                }

                // reject deletion if disallowed by policy
                if (entity.HasPolicy(V1alpha1EntityPolicyType.Delete) == false)
                {
                    Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} does not support delete (reason: Delete policy not enabled).", EntityTypeName, entity.Namespace(), entity.Name());
                }
                else
                {
                    Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} initiating deletion from Seq with ID: {Id} (reason: Kubernetes entity was deleted)", EntityTypeName, entity.Namespace(), entity.Name(), entity.Status.Id);
                    await DeleteAsync(api, entity.Status.Id, cancellationToken);
                    Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} deletion completed successfully", EntityTypeName, entity.Namespace(), entity.Name());
                }
            }
            catch (RetryException e)
            {
                try
                {
                    Logger.LogError("Retry hit deleting {EntityTypeName} {EntityNamespace}/{EntityName}: {Messge}", EntityTypeName, entity.Namespace(), entity.Name(), e.Message);
                    await DeletingWarningAsync(entity, "Retry", e.Message, cancellationToken);
                }
                catch (Exception e2)
                {
                    Logger.LogCritical(e2, "Unexpected exception creating event.");
                }

                Logger.LogInformation("Rescheduling delete after {TimeSpan}.", TimeSpan.FromMinutes(1));
                Requeue(entity, TimeSpan.FromMinutes(1));
            }
            catch (Exception e)
            {
                try
                {
                    Logger.LogError(e, "Unexpected exception deleting {EntityTypeName} {EntityNamespace}/{EntityName}.", EntityTypeName, entity.Namespace(), entity.Name());
                    await DeletingWarningAsync(entity, "Unknown", e.Message, cancellationToken);
                }
                catch (Exception e2)
                {
                    Logger.LogCritical(e2, "Unexpected exception creating event.");
                }

                throw;
            }
        }

    }

}
