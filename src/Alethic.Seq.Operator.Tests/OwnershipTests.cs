using System.Collections.Generic;

using Alethic.Seq.Operator.Instance;

using k8s;
using k8s.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alethic.Seq.Operator.Tests
{

    /// <summary>
    /// Objects the operator created before v1 carry owner references naming <c>seq.k8s.datalust.co/v1alpha1</c>. The
    /// operator now reads its Instances as v1, and must still recognise those objects as its own.
    /// </summary>
    [TestClass]
    public class OwnershipTests
    {

        const string Uid = "6a4c2f9e-0000-4000-8000-000000000001";

        static V1Instance Instance() => new V1Instance()
        {
            Metadata = new V1ObjectMeta() { NamespaceProperty = "seq", Name = "seq", Uid = Uid },
        }.Initialize();

        static V1Secret OwnedBy(string apiVersion, string uid) => new()
        {
            Metadata = new V1ObjectMeta()
            {
                NamespaceProperty = "seq",
                Name = "seq-operator-login",
                OwnerReferences = new List<V1OwnerReference>()
                {
                    new() { ApiVersion = apiVersion, Kind = "Instance", Name = "seq", Uid = uid },
                },
            },
        };

        /// <summary>
        /// Documents why <see cref="OwnershipExtensions.IsOwnedByEntity"/> exists: the client's own check fails here.
        /// </summary>
        [TestMethod]
        public void ClientIsOwnedByRejectsAnOlderApiVersion()
        {
            Assert.IsFalse(OwnedBy("seq.k8s.datalust.co/v1alpha1", Uid).IsOwnedBy(Instance()));
        }

        [TestMethod]
        public void RecognisesReferencesWrittenUnderV1alpha1()
        {
            Assert.IsTrue(OwnedBy("seq.k8s.datalust.co/v1alpha1", Uid).IsOwnedByEntity(Instance()));
        }

        [TestMethod]
        public void RecognisesReferencesWrittenUnderV1()
        {
            Assert.IsTrue(OwnedBy("seq.k8s.datalust.co/v1", Uid).IsOwnedByEntity(Instance()));
        }

        [TestMethod]
        public void RejectsAnotherOwner()
        {
            Assert.IsFalse(OwnedBy("seq.k8s.datalust.co/v1", "6a4c2f9e-0000-4000-8000-000000000002").IsOwnedByEntity(Instance()));
            Assert.IsFalse(new V1Secret() { Metadata = new V1ObjectMeta() { Name = "unowned" } }.IsOwnedByEntity(Instance()));
        }

        [TestMethod]
        public void RefreshRewritesTheOwnersReferenceToV1()
        {
            var secret = OwnedBy("seq.k8s.datalust.co/v1alpha1", Uid);
            secret.RefreshOwnerReference(Instance());

            Assert.AreEqual("seq.k8s.datalust.co/v1", secret.Metadata.OwnerReferences[0].ApiVersion);
            Assert.AreEqual(Uid, secret.Metadata.OwnerReferences[0].Uid);
            Assert.IsTrue(secret.IsOwnedBy(Instance()));
        }

        [TestMethod]
        public void RefreshLeavesOtherOwnersAlone()
        {
            var secret = OwnedBy("example.com/v1beta1", "6a4c2f9e-0000-4000-8000-000000000002");
            secret.RefreshOwnerReference(Instance());

            Assert.AreEqual("example.com/v1beta1", secret.Metadata.OwnerReferences[0].ApiVersion);
        }

    }

}
