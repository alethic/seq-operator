using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Alethic.Seq.Operator.Alerts;
using Alethic.Seq.Operator.ApiKey;
using Alethic.Seq.Operator.Finalizers.Legacy;
using Alethic.Seq.Operator.Instance;
using Alethic.Seq.Operator.RetentionPolicy;
using Alethic.Seq.Operator.Signals;

using k8s;
using k8s.Models;

namespace Alethic.Seq.Operator.Finalizers
{

    /// <summary>
    /// The single source of truth for the finalizer identifiers this operator writes into
    /// <c>metadata.finalizers</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// KubeOps derives a finalizer's identifier from its class name, both when attaching it
    /// (<c>EntityFinalizerExtensions.GetIdentifierName</c>, which reads <c>GetType().Name</c>) and when resolving it
    /// during finalization (the keyed registration emitted by <c>KubeOps.Generator</c>). Naming finalizers after the
    /// API version therefore mints a new identifier on every version bump and leaves the previous one behind on live
    /// entities.
    /// </para>
    /// <para>
    /// A leftover identifier is not merely untidy. The reconciler examines <c>Finalizers()[0]</c> only, and returns
    /// without removing it when no finalizer is registered under that identifier, so an entity whose first finalizer
    /// belongs to a retired release can never finish deleting. The retired identifiers therefore stay registered --
    /// via the shim classes in <see cref="Legacy"/> -- so entities already carrying them drain, and
    /// <see cref="V1alpha1Controller{TEntity, TSpec, TStatus, TConf, TInfo}"/> strips them from live entities so they
    /// are not carried forward. Current finalizer names are version-free and must not change again.
    /// </para>
    /// </remarks>
    public static class EntityFinalizers
    {

        /// <summary>
        /// The API group finalizer identifiers are qualified with. Matches the group of every entity kind.
        /// </summary>
        const string Group = "seq.k8s.datalust.co";

        /// <summary>
        /// Maximum length of a finalizer identifier, as enforced by KubeOps.
        /// </summary>
        const int MaxIdentifierLength = 63;

        /// <summary>
        /// The current and retired finalizer identifiers of a single entity kind.
        /// </summary>
        /// <param name="Current"></param>
        /// <param name="Retired"></param>
        record struct Registration(string Current, ImmutableHashSet<string> Retired);

        /// <summary>
        /// The finalizer identifiers of each entity kind. Identifiers are derived from the finalizer types themselves
        /// so that renaming a finalizer class cannot silently desynchronize this table from what KubeOps registers.
        /// </summary>
        static readonly Dictionary<Type, Registration> _registrations = new()
        {
            [typeof(V1alpha1Alert)] = Register<AlertFinalizer>(
                Identifier<Legacy.V1alpha1AlertFinalizer>()),
            [typeof(V1alpha1ApiKey)] = Register<ApiKeyFinalizer>(
                Identifier<Legacy.V1alpha1ApiKeyFinalizer>()),
            [typeof(V1alpha1Instance)] = Register<InstanceFinalizer>(
                Identifier<Legacy.V1alpha1InstanceFinalizer>()),
            [typeof(V1alpha1RetentionPolicy)] = Register<RetentionPolicyFinalizer>(
                Identifier<Legacy.V1alpha1RetentionPolicyFinalizer>()),
            [typeof(V1alpha1Signal)] = Register<SignalFinalizer>(
                Identifier<Legacy.V1alpha1SignalFinalizer>()),
        };

        /// <summary>
        /// Builds the registration for an entity kind whose current finalizer is <typeparamref name="TFinalizer"/>.
        /// </summary>
        /// <typeparam name="TFinalizer"></typeparam>
        /// <param name="retired"></param>
        /// <returns></returns>
        static Registration Register<TFinalizer>(params string[] retired)
        {
            return new Registration(Identifier<TFinalizer>(), retired.ToImmutableHashSet());
        }

        /// <summary>
        /// Reproduces the identifier KubeOps derives for the given finalizer type. Kept in lockstep with
        /// <c>KubeOps.Generator.Generators.FinalizerRegistrationGenerator.FinalizerName</c>.
        /// </summary>
        /// <typeparam name="TFinalizer"></typeparam>
        /// <returns></returns>
        static string Identifier<TFinalizer>()
        {
            var name = typeof(TFinalizer).Name.ToLowerInvariant();
            if (name.EndsWith("finalizer") == false)
                name += "finalizer";

            var identifier = $"{Group}/{name}";
            if (identifier.Length > MaxIdentifierLength)
                identifier = identifier.Substring(0, MaxIdentifierLength);

            return identifier;
        }

        /// <summary>
        /// Gets the finalizer identifier that should be attached to entities of the given kind, or <c>null</c> when
        /// the kind has no finalizer.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static string? Current<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            return _registrations.TryGetValue(typeof(TEntity), out var registration) ? registration.Current : null;
        }

        /// <summary>
        /// Gets the finalizer identifiers previous releases attached to entities of the given kind, which are still
        /// registered so that entities carrying them can drain, but which are no longer attached.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static IReadOnlyCollection<string> Retired<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            return _registrations.TryGetValue(typeof(TEntity), out var registration) ? registration.Retired : ImmutableHashSet<string>.Empty;
        }

    }

}
