using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Alethic.Seq.Operator.Alerts;
using Alethic.Seq.Operator.ApiKey;
using Alethic.Seq.Operator.Instance;
using Alethic.Seq.Operator.RetentionPolicy;
using Alethic.Seq.Operator.Signals;

using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alethic.Seq.Operator.Tests
{

    /// <summary>
    /// Pins how each kind is versioned. Both versions are served from one CustomResourceDefinition with
    /// <c>conversion: None</c>, which is only safe while their schemas are identical.
    /// </summary>
    [TestClass]
    public class ApiVersionTests
    {

        static IEnumerable<(Type V1, Type V1alpha1)> Kinds()
        {
            yield return (typeof(V1Alert), typeof(V1alpha1Alert));
            yield return (typeof(V1ApiKey), typeof(V1alpha1ApiKey));
            yield return (typeof(V1Instance), typeof(V1alpha1Instance));
            yield return (typeof(V1RetentionPolicy), typeof(V1alpha1RetentionPolicy));
            yield return (typeof(V1Signal), typeof(V1alpha1Signal));
        }

        static KubernetesEntityAttribute Entity(Type type) => type.GetCustomAttribute<KubernetesEntityAttribute>()!;

        [TestMethod]
        public void V1IsTheStorageVersion()
        {
            foreach (var (v1, v1alpha1) in Kinds())
            {
                Assert.AreEqual("v1", Entity(v1).ApiVersion);
                Assert.IsNotNull(v1.GetCustomAttribute<StorageVersionAttribute>(), $"{v1.Name} is not the storage version");
                Assert.IsNull(v1alpha1.GetCustomAttribute<StorageVersionAttribute>(), $"{v1alpha1.Name} is marked as the storage version");
            }
        }

        [TestMethod]
        public void V1alpha1IsStillServedUnderTheSameKind()
        {
            foreach (var (v1, v1alpha1) in Kinds())
            {
                Assert.AreEqual("v1alpha1", Entity(v1alpha1).ApiVersion);
                Assert.AreEqual(Entity(v1).Group, Entity(v1alpha1).Group);
                Assert.AreEqual(Entity(v1).Kind, Entity(v1alpha1).Kind);
                Assert.AreEqual(Entity(v1).PluralName, Entity(v1alpha1).PluralName);
                CollectionAssert.AreEqual(v1.GetCustomAttribute<KubernetesEntityShortNamesAttribute>()!.ShortNames, v1alpha1.GetCustomAttribute<KubernetesEntityShortNamesAttribute>()!.ShortNames);
            }
        }

        /// <summary>
        /// The v1alpha1 types must share v1's spec and status types and add nothing of their own, so that the generated
        /// schemas match and objects round-trip between versions without losing fields.
        /// </summary>
        [TestMethod]
        public void V1alpha1SharesTheV1Schema()
        {
            foreach (var (v1, v1alpha1) in Kinds())
            {
                CollectionAssert.AreEqual(v1.BaseType!.GetGenericArguments(), v1alpha1.BaseType!.GetGenericArguments(), $"{v1alpha1.Name} has different spec or status types from {v1.Name}");
                Assert.AreEqual(0, v1alpha1.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length, $"{v1alpha1.Name} declares properties of its own");
            }
        }

    }

}
