using System;
using System.Collections.Generic;
using System.Linq;

using Alethic.Seq.Operator.ApiKey;
using Alethic.Seq.Operator.Instance;

using k8s.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alethic.Seq.Operator.Tests
{

    /// <summary>
    /// Covers which Seq permissions an ApiKey may be assigned, as decided by the referenced Instance's permissions.
    /// </summary>
    [TestClass]
    public class ApiKeyPermissionTests
    {

        static readonly ApiKeyPermission[] Sensitive =
        [
            ApiKeyPermission.Read,
            ApiKeyPermission.Write,
            ApiKeyPermission.Project,
            ApiKeyPermission.System,
            ApiKeyPermission.Organization,
        ];

        static V1alpha1Instance Instance(params InstancePermission[]? permissions) => new()
        {
            Metadata = new V1ObjectMeta() { NamespaceProperty = "seq", Name = "seq" },
            Spec = new V1alpha1InstanceSpec() { Permissions = permissions },
        };

        static V1Namespace Namespace(string name, IDictionary<string, string>? labels = null) => new()
        {
            Metadata = new V1ObjectMeta() { Name = name, Labels = labels },
        };

        static InstancePermission FromAll(InstanceApiKeyPermissions apiKeys) => new()
        {
            Namespaces = new InstancePermissionNamespaces() { From = InstancePermissionNamespaceFrom.All },
            ApiKeys = apiKeys,
        };

        static List<ApiKeyPermission> Denied(V1alpha1Instance instance, string ns, params ApiKeyPermission[] permissions) =>
            V1alpha1ApiKeyController.GetDeniedPermissions(instance, Namespace(ns), permissions);

        [TestMethod]
        public void IngestAndPublicAreAllowedWhenNotMentioned()
        {
            var instance = Instance(FromAll(new InstanceApiKeyPermissions() { Create = true }));
            CollectionAssert.AreEqual(Array.Empty<ApiKeyPermission>(), Denied(instance, "app", ApiKeyPermission.Ingest, ApiKeyPermission.Public));
        }

        [TestMethod]
        public void SensitivePermissionsAreDeniedWhenNotMentioned()
        {
            var instance = Instance(FromAll(new InstanceApiKeyPermissions() { Create = true }));
            CollectionAssert.AreEquivalent(Sensitive, Denied(instance, "app", Sensitive));
        }

        [TestMethod]
        public void IngestCanBeDenied()
        {
            var instance = Instance(FromAll(new InstanceApiKeyPermissions() { Create = true, SetIngest = false }));
            CollectionAssert.AreEqual(new[] { ApiKeyPermission.Ingest }, Denied(instance, "app", ApiKeyPermission.Ingest));
        }

        [TestMethod]
        public void SensitivePermissionsCanBeGranted()
        {
            var instance = Instance(FromAll(new InstanceApiKeyPermissions() { Create = true, SetRead = true }));
            CollectionAssert.AreEqual(new[] { ApiKeyPermission.System }, Denied(instance, "app", ApiKeyPermission.Read, ApiKeyPermission.System));
        }

        /// <summary>
        /// Matches the labs configuration: the Instance's own namespace and every other namespace may create keys, with
        /// no assignment flags set. App namespaces asking for Ingest keep working; asking for more is refused.
        /// </summary>
        [TestMethod]
        public void FirstMatchingEntryDecides()
        {
            var instance = Instance(
                new InstancePermission()
                {
                    Namespaces = new InstancePermissionNamespaces() { From = InstancePermissionNamespaceFrom.Same },
                    ApiKeys = new InstanceApiKeyPermissions() { Create = true, SetSystem = true },
                },
                FromAll(new InstanceApiKeyPermissions() { Create = true }));

            CollectionAssert.AreEqual(Array.Empty<ApiKeyPermission>(), Denied(instance, "seq", ApiKeyPermission.System));
            CollectionAssert.AreEqual(Array.Empty<ApiKeyPermission>(), Denied(instance, "sweep-dev1", ApiKeyPermission.Ingest));
            CollectionAssert.AreEqual(new[] { ApiKeyPermission.System }, Denied(instance, "sweep-dev1", ApiKeyPermission.Ingest, ApiKeyPermission.System));
        }

        [TestMethod]
        public void SelectorScopesTheGrant()
        {
            var instance = Instance(new InstancePermission()
            {
                Namespaces = new InstancePermissionNamespaces()
                {
                    From = InstancePermissionNamespaceFrom.Selector,
                    Selector = new V1LabelSelector() { MatchLabels = new Dictionary<string, string>() { ["seq-read"] = "true" } },
                },
                ApiKeys = new InstanceApiKeyPermissions() { Create = true, SetRead = true },
            });

            var labelled = V1alpha1ApiKeyController.GetDeniedPermissions(instance, Namespace("ops", new Dictionary<string, string>() { ["seq-read"] = "true" }), [ApiKeyPermission.Read]);
            CollectionAssert.AreEqual(Array.Empty<ApiKeyPermission>(), labelled);
            CollectionAssert.AreEqual(new[] { ApiKeyPermission.Read }, Denied(instance, "app", ApiKeyPermission.Read));
        }

        /// <summary>
        /// Without spec.permissions, the Instance's own namespace may assign anything, as before.
        /// </summary>
        [TestMethod]
        public void DefaultPermissionsAllowEverythingInTheSameNamespace()
        {
            var instance = Instance(null);
            CollectionAssert.AreEqual(Array.Empty<ApiKeyPermission>(), Denied(instance, "seq", Sensitive));
        }

        [TestMethod]
        public void UndefinedIsAlwaysDenied()
        {
            var instance = Instance(null);
            CollectionAssert.AreEqual(new[] { ApiKeyPermission.Undefined }, Denied(instance, "seq", ApiKeyPermission.Undefined));
        }

#pragma warning disable CS0618
        [TestMethod]
        public void SetupRequiresProjectAndSystem()
        {
            Assert.IsFalse(V1alpha1ApiKeyController.IsPermissionGranted(new InstanceApiKeyPermissions() { SetProject = true }, ApiKeyPermission.Setup));
            Assert.IsTrue(V1alpha1ApiKeyController.IsPermissionGranted(new InstanceApiKeyPermissions() { SetProject = true, SetSystem = true }, ApiKeyPermission.Setup));
        }
#pragma warning restore CS0618

        [TestMethod]
        public void DuplicatesAreReportedOnce()
        {
            var instance = Instance(FromAll(new InstanceApiKeyPermissions() { Create = true }));
            CollectionAssert.AreEqual(new[] { ApiKeyPermission.Read }, Denied(instance, "app", ApiKeyPermission.Read, ApiKeyPermission.Read));
        }

    }

}
