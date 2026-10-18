using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.Instance;

using k8s.Models;

using KubeOps.KubernetesClient;

using Microsoft.Extensions.Caching.Memory;

namespace Alethic.Seq.Operator
{

    /// <summary>
    /// Provides the ability to lookup and cache various entities.
    /// </summary>
    public class LookupService
    {

        readonly IKubernetesClient _kube;
        readonly IMemoryCache _cache;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="cache"></param>
        /// <param name="logger"></param>
        public LookupService(IKubernetesClient kube, IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _kube = kube ?? throw new ArgumentNullException(nameof(kube));
        }

        /// <summary>
        /// Resolves the specified namespace object.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1Namespace?> ResolveNamespaceAsync(string namespaceName, CancellationToken cancellationToken)
        {
            return await _cache.GetOrCreateAsync((typeof(LookupService), nameof(ResolveNamespaceAsync), namespaceName), async entry =>
            {
                var ns = await _kube.GetAsync<V1Namespace>(name: namespaceName, cancellationToken: cancellationToken);
                entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
                return ns;
            });
        }

        /// <summary>
        /// Attempts to resolve the secret document referenced by the secret reference.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="namespaceName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1Secret?> ResolveSecretAsync(string name, string namespaceName, CancellationToken cancellationToken)
        {
            return await _cache.GetOrCreateAsync((typeof(LookupService), nameof(ResolveSecretAsync), name, namespaceName), async entry =>
            {
                var ns = await _kube.GetAsync<V1Secret>(name, namespaceName, cancellationToken: cancellationToken);
                entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
                return ns;
            });
        }

        /// <summary>
        /// Attempts to resolve the secret document referenced by the secret reference, which must be in the namespace of
        /// the resource holding the reference.
        /// </summary>
        /// <param name="secretRef"></param>
        /// <param name="ownerNamespace">The namespace of the resource holding the reference.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1Secret?> ResolveSecretRefAsync(V1SecretReference? secretRef, string ownerNamespace, CancellationToken cancellationToken)
        {
            if (secretRef is null)
                return null;

            if (string.IsNullOrWhiteSpace(secretRef.Name))
                throw new InvalidOperationException($"Secret reference {secretRef} has no name.");

            if (string.IsNullOrWhiteSpace(ownerNamespace))
                throw new InvalidOperationException($"Secret reference {secretRef} has no discovered namespace.");

            // The operator can read Secrets in every namespace, so following a reference into another namespace would let
            // anyone able to create a resource here read any Secret in the cluster through it.
            if (IsCrossNamespace(secretRef, ownerNamespace))
                throw new InvalidOperationException($"Secret reference {secretRef.Name} names namespace {secretRef.NamespaceProperty}, but Secrets must be in the namespace of the resource referencing them, {ownerNamespace}.");

            return await ResolveSecretAsync(secretRef.Name, ownerNamespace, cancellationToken);
        }

        /// <summary>
        /// Returns whether the secret reference names a namespace other than <paramref name="ownerNamespace"/>.
        /// </summary>
        /// <param name="secretRef"></param>
        /// <param name="ownerNamespace"></param>
        /// <returns></returns>
        internal static bool IsCrossNamespace(V1SecretReference secretRef, string ownerNamespace)
        {
            return string.IsNullOrEmpty(secretRef.NamespaceProperty) == false && secretRef.NamespaceProperty != ownerNamespace;
        }

        /// <summary>
        /// Attempts to resolve the secret value referenced by the secret key selector.
        /// </summary>
        /// <param name="secretKeySelector"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<byte[]?> ResolveSecretKeySelectorAsync(V1SecretKeySelector? secretKeySelector, string defaultNamespace, CancellationToken cancellationToken)
        {
            if (secretKeySelector is null)
                return null;

            if (string.IsNullOrWhiteSpace(secretKeySelector.Name))
                throw new InvalidOperationException($"Secret selector {secretKeySelector} has no name.");

            if (secretKeySelector.Key is null)
                throw new InvalidOperationException($"Secret selector {secretKeySelector} has no key.");

            var ns = defaultNamespace;
            if (string.IsNullOrWhiteSpace(ns))
                throw new InvalidOperationException($"Secret selector {secretKeySelector} has no discovered namespace.");

            var secret = await ResolveSecretAsync(secretKeySelector.Name, ns, cancellationToken);
            if (secret is null)
                if (secretKeySelector.Optional == false)
                    throw new InvalidOperationException($"Secret selector {secretKeySelector} could not be found.");
                else
                    return null;

            secret.Data ??= new Dictionary<string, byte[]>();
            if (secret.Data.TryGetValue(secretKeySelector.Key, out var buf) == false)
                if (secretKeySelector.Optional == false)
                    throw new InvalidOperationException($"Secret selector {secretKeySelector} has missing key value on secret.");
                else
                    return null;

            return buf;
        }

        /// <summary>
        /// Attempts to resolve the secret document referenced by the secret reference.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="namespaceName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1alpha1Instance?> ResolveInstanceAsync(string name, string namespaceName, CancellationToken cancellationToken)
        {
            return await _cache.GetOrCreateAsync((typeof(LookupService), nameof(ResolveInstanceAsync), name, namespaceName), async entry =>
            {
                var ns = await _kube.GetAsync<V1alpha1Instance>(name, namespaceName, cancellationToken: cancellationToken);
                entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
                return ns;
            });
        }

        /// <summary>
        /// Attempts to resolve the instance document referenced by the instance reference.
        /// </summary>
        /// <param name="instanceRef"></param>
        /// <param name="defaultNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<V1alpha1Instance?> ResolveInstanceRefAsync(V1alpha1InstanceReference? instanceRef, string defaultNamespace, CancellationToken cancellationToken)
        {
            if (instanceRef is null)
                return null;

            if (string.IsNullOrWhiteSpace(instanceRef.Name))
                throw new InvalidOperationException($"Instance reference {instanceRef} has no name.");

            var ns = instanceRef.Namespace ?? defaultNamespace;
            if (string.IsNullOrWhiteSpace(ns))
                throw new InvalidOperationException($"Instance reference {instanceRef} has no discovered namesace.");

            return await ResolveInstanceAsync(instanceRef.Name, ns, cancellationToken);
        }

    }

}
