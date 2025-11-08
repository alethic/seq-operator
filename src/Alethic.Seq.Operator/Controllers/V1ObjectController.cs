using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Core.Models.Object;
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

namespace Alethic.Seq.Operator.Controllers
{

    [EntityRbac(typeof(V1Instance), Verbs = RbacVerb.List | RbacVerb.Get)]
    [EntityRbac(typeof(V1Object), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1Secret), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(Eventsv1Event), Verbs = RbacVerb.All)]
    public class V1ObjectController :
        V1InstanceEntityController<V1Object, V1Object.SpecDef, V1Object.StatusDef, ObjectConf>,
        IEntityController<V1Object>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public V1ObjectController(IKubernetesClient kube, EntityRequeue<V1Object> requeue, IMemoryCache cache, ILogger<V1ObjectController> logger, IOptions<OperatorOptions> options) :
            base(kube, requeue, cache, logger, options)
        {

        }

        /// <inheritdoc />
        protected override string EntityTypeName => "Object";

        /// <inheritdoc />
        protected override async Task<Hashtable?> Get(object api, string id, string defaultNamespace, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override async Task<string?> Find(object api, V1Object entity, V1Object.SpecDef spec, string defaultNamespace, CancellationToken cancellationToken)
        {
            if (spec.Find is not null)
            {
                throw new NotImplementedException();
            }
            else
            {
                var conf = spec.Init ?? spec.Conf;
                if (conf is null)
                    return null;

                throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        protected override string? ValidateCreate(ObjectConf conf)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override async Task<string> Create(object api, ObjectConf conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            //Logger.LogInformation("{EntityTypeName} creating client in Seq with name: {ClientName}", EntityTypeName, conf.Name);
            //Logger.LogInformation("{EntityTypeName} successfully created client in Seq with ID: {ClientId} and name: {ClientName}", EntityTypeName, self.ClientId, conf.Name);
        }

        /// <inheritdoc />
        protected override async Task Update(object api, string id, Hashtable? last, ObjectConf conf, string defaultNamespace, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            //Logger.LogInformation("{EntityTypeName} updating client in Seq with id: {ClientId} and name: {ClientName}", EntityTypeName, id, conf.Name);
            //Logger.LogInformation("{EntityTypeName} successfully updated client in Seq with id: {ClientId} and name: {ClientName}", EntityTypeName, id, conf.Name);
        }

        /// <inheritdoc />
        protected override async Task ApplyStatus(object api, V1Object entity, Hashtable lastConf, string defaultNamespace, CancellationToken cancellationToken)
        {
            if (entity.Spec.SecretRef is not null)
            {
                throw new NotImplementedException();
                await ApplySecret(entity, null, null, defaultNamespace, cancellationToken);
            }

            await base.ApplyStatus(api, entity, lastConf, defaultNamespace, cancellationToken);
        }

        /// <summary>
        /// Applies the client secret.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task ApplySecret(V1Object entity, string? clientId, string? clientSecret, string defaultNamespace, CancellationToken cancellationToken)
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

                throw new NotImplementedException();

                secret = await Kube.UpdateAsync(secret, cancellationToken);
                Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} successfully updated secret {SecretName}", EntityTypeName, entity.Namespace(), entity.Name(), entity.Spec.SecretRef.Name);
            }
            else
            {
                Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} secret {SecretName} exists but is not owned by this client, skipping update", EntityTypeName, entity.Namespace(), entity.Name(), entity.Spec.SecretRef.Name);
            }
        }

        /// <inheritdoc />
        protected override async Task Delete(object api, string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            //Logger.LogInformation("{EntityTypeName} deleting client from Seq with ID: {ClientId} (reason: Kubernetes entity deleted)", EntityTypeName, id);
            //Logger.LogInformation("{EntityTypeName} successfully deleted client from Seq with ID: {ClientId}", EntityTypeName, id);
        }

    }

}
