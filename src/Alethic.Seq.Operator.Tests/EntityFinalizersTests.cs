using System;
using System.Collections.Generic;
using System.Linq;

using Alethic.Seq.Operator.Alerts;
using Alethic.Seq.Operator.ApiKey;
using Alethic.Seq.Operator.Finalizers;
using Alethic.Seq.Operator.Instance;
using Alethic.Seq.Operator.RetentionPolicy;
using Alethic.Seq.Operator.Signals;

using KubeOps.Abstractions.Reconciliation.Finalizer;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alethic.Seq.Operator.Tests
{

    /// <summary>
    /// Pins the exact strings this operator writes into <c>metadata.finalizers</c>.
    /// </summary>
    /// <remarks>
    /// These are not cosmetic. KubeOps derives a finalizer's identifier from its class name, and the reconciler
    /// inspects <c>Finalizers()[0]</c> only, returning without removing it when nothing is registered under that
    /// identifier. Renaming a finalizer, or deleting one of the legacy shims, therefore wedges every live entity
    /// carrying the old identifier in <c>Terminating</c> forever. A failure here means an upgrade would strand
    /// objects in the cluster.
    /// </remarks>
    [TestClass]
    public class EntityFinalizersTests
    {

        const string Group = "seq.k8s.datalust.co";

        /// <summary>
        /// Maximum length of a finalizer identifier, as enforced by KubeOps.
        /// </summary>
        const int MaxIdentifierLength = 63;

        /// <summary>
        /// The identifier attached to new entities of each kind. Changing one of these is a breaking change for
        /// every object already in a cluster.
        /// </summary>
        static IEnumerable<(Type Entity, string Identifier)> CurrentIdentifiers()
        {
            yield return (typeof(V1alpha1Alert), "seq.k8s.datalust.co/alertfinalizer");
            yield return (typeof(V1alpha1ApiKey), "seq.k8s.datalust.co/apikeyfinalizer");
            yield return (typeof(V1alpha1Instance), "seq.k8s.datalust.co/instancefinalizer");
            yield return (typeof(V1alpha1RetentionPolicy), "seq.k8s.datalust.co/retentionpolicyfinalizer");
            yield return (typeof(V1alpha1Signal), "seq.k8s.datalust.co/signalfinalizer");
        }

        /// <summary>
        /// Identifiers written by previous releases. They are no longer attached, but stay registered so that
        /// entities already carrying them can drain.
        /// </summary>
        static IEnumerable<(Type Entity, string Identifier)> RetiredIdentifiers()
        {
            yield return (typeof(V1alpha1Alert), "seq.k8s.datalust.co/v1alpha1alertfinalizer");
            yield return (typeof(V1alpha1ApiKey), "seq.k8s.datalust.co/v1alpha1apikeyfinalizer");
            yield return (typeof(V1alpha1Instance), "seq.k8s.datalust.co/v1alpha1instancefinalizer");
            yield return (typeof(V1alpha1RetentionPolicy), "seq.k8s.datalust.co/v1alpha1retentionpolicyfinalizer");
            yield return (typeof(V1alpha1Signal), "seq.k8s.datalust.co/v1alpha1signalfinalizer");
        }

        /// <summary>
        /// Invokes the generic <see cref="EntityFinalizers.Current{TEntity}"/> for a runtime type.
        /// </summary>
        /// <param name="entity"></param>
        static string? Current(Type entity)
        {
            return (string?)typeof(EntityFinalizers)
                .GetMethod(nameof(EntityFinalizers.Current))!
                .MakeGenericMethod(entity)
                .Invoke(null, null);
        }

        /// <summary>
        /// Invokes the generic <see cref="EntityFinalizers.Retired{TEntity}"/> for a runtime type.
        /// </summary>
        /// <param name="entity"></param>
        static IReadOnlyCollection<string> Retired(Type entity)
        {
            return (IReadOnlyCollection<string>)typeof(EntityFinalizers)
                .GetMethod(nameof(EntityFinalizers.Retired))!
                .MakeGenericMethod(entity)
                .Invoke(null, null)!;
        }

        /// <summary>
        /// Reproduces the identifier KubeOps derives from a finalizer's class name.
        /// </summary>
        /// <param name="finalizer"></param>
        static string Identifier(Type finalizer)
        {
            var name = finalizer.Name.ToLowerInvariant();
            if (name.EndsWith("finalizer") == false)
                name += "finalizer";

            var identifier = $"{Group}/{name}";
            return identifier.Length > MaxIdentifierLength ? identifier.Substring(0, MaxIdentifierLength) : identifier;
        }

        /// <summary>
        /// Every concrete finalizer in the operator assembly, paired with the entity it finalizes.
        /// </summary>
        static IEnumerable<(Type Finalizer, Type Entity)> RegisteredFinalizers()
        {
            foreach (var type in typeof(EntityFinalizers).Assembly.GetTypes())
            {
                if (type.IsClass == false || type.IsAbstract)
                    continue;

                foreach (var iface in type.GetInterfaces())
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEntityFinalizer<>))
                        yield return (type, iface.GetGenericArguments()[0]);
            }
        }

        [TestMethod]
        public void CurrentIdentifiersAreExactlyAsReleased()
        {
            foreach (var (entity, identifier) in CurrentIdentifiers())
                Assert.AreEqual(identifier, Current(entity), $"current finalizer identifier for {entity.Name} changed");
        }

        [TestMethod]
        public void RetiredIdentifiersAreStillRegistered()
        {
            foreach (var (entity, identifier) in RetiredIdentifiers())
                Assert.IsTrue(Retired(entity).Contains(identifier), $"retired finalizer identifier {identifier} is no longer registered; entities carrying it could never finish deleting");
        }

        [TestMethod]
        public void CurrentIdentifierIsNeverAlsoRetired()
        {
            foreach (var (entity, identifier) in CurrentIdentifiers())
                Assert.IsFalse(Retired(entity).Contains(identifier), $"{identifier} is both attached and stripped, so reconciliation would never settle");
        }

        [TestMethod]
        public void IdentifiersFitTheKubernetesLimit()
        {
            foreach (var identifier in CurrentIdentifiers().Concat(RetiredIdentifiers()).Select(i => i.Identifier))
                Assert.IsTrue(identifier.Length <= MaxIdentifierLength, $"{identifier} is {identifier.Length} characters, over the {MaxIdentifierLength} KubeOps allows");
        }

        [TestMethod]
        public void EveryFinalizerClassIsAccountedForInTheTable()
        {
            foreach (var (finalizer, entity) in RegisteredFinalizers())
            {
                var identifier = Identifier(finalizer);
                var known = Current(entity) == identifier || Retired(entity).Contains(identifier);
                Assert.IsTrue(known, $"{finalizer.Name} resolves to {identifier}, which EntityFinalizers does not list for {entity.Name}");
            }
        }

        [TestMethod]
        public void EveryTableEntryHasAFinalizerClass()
        {
            var registered = RegisteredFinalizers()
                .Select(i => (i.Entity, Identifier: Identifier(i.Finalizer)))
                .ToHashSet();

            foreach (var (entity, identifier) in CurrentIdentifiers().Concat(RetiredIdentifiers()))
                Assert.IsTrue(registered.Contains((entity, identifier)), $"EntityFinalizers lists {identifier} for {entity.Name}, but no finalizer class produces it, so KubeOps could never resolve it");
        }

    }

}
