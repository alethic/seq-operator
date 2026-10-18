using System;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Instance;
using Alethic.Seq.Operator.Options;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Reconciliation;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Seq.Api;

namespace Alethic.Seq.Operator
{

    public abstract class V1alpha1InstanceEntityController<TEntity, TSpec, TStatus, TConf, TInfo> :
        V1alpha1Controller<TEntity, TSpec, TStatus, TConf, TInfo>
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
        /// <param name="cache"></param>
        /// <param name="lookup"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public V1alpha1InstanceEntityController(IKubernetesClient kube, IMemoryCache cache, LookupService lookup, IOptions<OperatorOptions> options, ILogger logger) :
            base(kube, cache, lookup, options, logger)
        {

        }

        /// <summary>
        /// Returns <c>true</c> if the given entity can attach to existing Seq objects in the given instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract Task<bool> CanAttachFromAsync(V1alpha1Instance instance, TEntity entity, CancellationToken cancellationToken);

        /// <summary>
        /// Returns <c>true</c> if the given entity can create new Seq objects in the given instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract Task<bool> CanCreateFromAsync(V1alpha1Instance instance, TEntity entity, CancellationToken cancellationToken);

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
        /// <param name="instance"></param>
        /// <param name="entity"></param>
        /// <param name="conf"></param>
        /// <returns></returns>
        protected virtual async Task<string?> ValidateCreateAsync(V1alpha1Instance instance, TEntity entity, TConf? conf, CancellationToken cancellationToken)
        {
            return await ValidateUpdateAsync(instance, entity, conf, cancellationToken);
        }

        /// <summary>
        /// Performs a validation on the <paramref name="conf"/> parameter for usage in update operations.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="entity"></param>
        /// <param name="conf"></param>
        /// <returns></returns>
        protected abstract Task<string?> ValidateUpdateAsync(V1alpha1Instance instance, TEntity entity, TConf? conf, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to perform a creation through the API. If successful returns the new ID value.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="entity"></param>
        /// <param name="api"></param>
        /// <param name="conf"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<string> CreateAsync(V1alpha1Instance instance, TEntity entity, SeqConnection api, TConf? conf, string defaultNamespace, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to perform an update through the API.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="entity"></param>
        /// <param name="api"></param>
        /// <param name="id"></param>
        /// <param name="info"></param>
        /// <param name="conf"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task UpdateAsync(V1alpha1Instance instance, TEntity entity, SeqConnection api, string id, TInfo? info, TConf? conf, string defaultNamespace, CancellationToken cancellationToken);

        /// <summary>
        /// Removes an object just created in Seq whose ID could not be recorded, so that the retry does not leave it
        /// behind as a duplicate. Failure to remove it is logged and swallowed, so the original error is what surfaces.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="entity"></param>
        /// <param name="api"></param>
        /// <param name="id"></param>
        /// <param name="cause"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task DeleteOrphanAsync(V1alpha1Instance instance, TEntity entity, SeqConnection api, string id, Exception cause, CancellationToken cancellationToken)
        {
            Logger.LogWarning(cause, "{EntityTypeName} {Namespace}/{Name} created {Id} in Seq but could not record it; removing it so the retry does not create a duplicate.", EntityTypeName, entity.Namespace(), entity.Name(), id);

            try
            {
                await DeleteAsync(instance, api, id, cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "{EntityTypeName} {Namespace}/{Name} could not remove {Id} from Seq; it is now orphaned and must be removed by hand.", EntityTypeName, entity.Namespace(), entity.Name(), id);
            }
        }

        /// <inheritdoc />
        protected override async Task<TEntity> Reconcile(TEntity entity, CancellationToken cancellationToken)
        {
            if (entity.Spec.InstanceRef is null)
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} missing a Seq instance reference.");

            var instance = await Lookup.ResolveInstanceRefAsync(entity.Spec.InstanceRef, entity.Namespace(), cancellationToken);
            if (instance is null)
                throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} could not resolve Seq instance.");

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

                    if (await CanCreateFromAsync(instance, entity, cancellationToken) == false)
                        throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} is invalid: referenced Seq instance does not permit creating from this namespace.");

                    // validate configuration version used for initialization
                    if (await ValidateCreateAsync(instance, entity, entity.Spec.Conf, cancellationToken) is string msg)
                        throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} is invalid: {msg}");

                    // create new entity and associate
                    var id = await CreateAsync(instance, entity, api, entity.Spec.Conf, entity.Namespace(), cancellationToken);
                    Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} created with {Id}", EntityTypeName, entity.Namespace(), entity.Name(), id);

                    // the ID is the only link to what was just created, and most kinds cannot be found again without it,
                    // so save it now; if that fails, remove the object rather than let the retry create a second one
                    entity.Status.Id = id;
                    try
                    {
                        entity = await Kube.UpdateStatusAsync(entity, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        await DeleteOrphanAsync(instance, entity, api, id, e, cancellationToken);
                        throw;
                    }
                }
                else
                {
                    if (await CanAttachFromAsync(instance, entity, cancellationToken) == false)
                        throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} is invalid: referenced Seq instance does not permit attaching from this namespace.");

                    entity.Status.Id = entityId;
                    Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} found with {Id}", EntityTypeName, entity.Namespace(), entity.Name(), entity.Status.Id);

                    // save the status
                    entity = await Kube.UpdateStatusAsync(entity, cancellationToken);
                }
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

            // apply configuration if specified
            if (entity.Spec.Conf is { } conf)
            {
                if (await ValidateUpdateAsync(instance, entity, conf, cancellationToken) is string msg)
                    throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} is invalid: {msg}");

                await UpdateAsync(instance, entity, api, entity.Status.Id, info, conf, entity.Namespace(), cancellationToken);
            }

            // save entity
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
        /// <param name="instance"></param>
        /// <param name="api"></param>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task DeleteAsync(V1alpha1Instance instance, SeqConnection api, string id, CancellationToken cancellationToken);

        /// <inheritdoc />
        public override sealed async Task<ReconciliationResult<TEntity>> DeletedAsync(TEntity entity, CancellationToken cancellationToken)
        {
            // whether the Instance is itself being deleted, in which case removing the object from Seq is best effort
            var instanceDeleting = false;

            try
            {
                if (string.IsNullOrWhiteSpace(entity.Status.Id))
                {
                    Logger.LogWarning("{EntityTypeName} {EntityNamespace}/{EntityName} has no known ID, skipping delete (reason: entity was never successfully created in Seq).", EntityTypeName, entity.Namespace(), entity.Name());
                    return ReconciliationResult<TEntity>.Success(entity);
                }

                if (entity.Spec.InstanceRef is not { Name: { } instanceName } instanceRef)
                {
                    Logger.LogWarning("{EntityTypeName} {EntityNamespace}/{EntityName} has no Seq instance reference, skipping delete of {Id}.", EntityTypeName, entity.Namespace(), entity.Name(), entity.Status.Id);
                    return ReconciliationResult<TEntity>.Success(entity);
                }

                // read past the lookup cache: a cached Instance may already be gone, or not yet show it is being deleted
                var instance = await Kube.GetAsync<V1alpha1Instance>(instanceName, instanceRef.Namespace ?? entity.Namespace(), cancellationToken: cancellationToken);
                if (instance is null)
                {
                    // without the Instance there are no credentials to reach Seq with; holding the finalizer would
                    // only block the delete (and any namespace containing it) forever
                    Logger.LogWarning("{EntityTypeName} {EntityNamespace}/{EntityName} references Seq instance {InstanceName} which no longer exists, skipping delete of {Id} (it is left in Seq).", EntityTypeName, entity.Namespace(), entity.Name(), instanceName, entity.Status.Id);
                    return ReconciliationResult<TEntity>.Success(entity);
                }

                instanceDeleting = instance.Metadata.DeletionTimestamp is not null;

                var api = await GetInstanceConnectionAsync(instance, cancellationToken);
                if (api is null)
                    throw new InvalidOperationException($"{EntityTypeName} {entity.Namespace()}/{entity.Name()} failed to retrieve API client.");

                var self = await GetAsync(entity, api, entity.Status.Id, entity.Namespace(), cancellationToken);
                if (self is null)
                {
                    Logger.LogWarning("{EntityTypeName} {EntityNamespace}/{EntityName} with ID {Id} not found in Seq, skipping delete (reason: already deleted externally).", EntityTypeName, entity.Namespace(), entity.Name(), entity.Status.Id);
                    return ReconciliationResult<TEntity>.Success(entity);
                }

                Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} initiating deletion from Seq with ID: {Id} (reason: Kubernetes entity was deleted)", EntityTypeName, entity.Namespace(), entity.Name(), entity.Status.Id);
                await DeleteAsync(instance, api, entity.Status.Id, cancellationToken);
                Logger.LogInformation("{EntityTypeName} {Namespace}/{Name} deletion completed successfully", EntityTypeName, entity.Namespace(), entity.Name());
                return ReconciliationResult<TEntity>.Success(entity);
            }
            catch (Exception e) when (instanceDeleting)
            {
                // the Instance is going too, and a deployed one takes its Seq server with it; an unreachable Seq is no
                // reason to hold this entity, or the namespace both are in, in Terminating
                Logger.LogWarning(e, "{EntityTypeName} {EntityNamespace}/{EntityName} could not remove {Id} from Seq while its Seq instance is being deleted, skipping delete (it is left in Seq).", EntityTypeName, entity.Namespace(), entity.Name(), entity.Status.Id);
                return ReconciliationResult<TEntity>.Success(entity);
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

                var interval = Options.Reconciliation.RetryInterval;
                Logger.LogInformation("Rescheduling delete after {TimeSpan}.", interval);
                return ReconciliationResult<TEntity>.Failure(entity, e.Message, e, interval);
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
