using k8s.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alethic.Seq.Operator.Tests
{

    /// <summary>
    /// Covers the rule that a Secret reference may only name the namespace of the resource holding it.
    /// </summary>
    [TestClass]
    public class SecretReferenceTests
    {

        [TestMethod]
        public void NoNamespaceIsLocal()
        {
            Assert.IsFalse(LookupService.IsCrossNamespace(new V1SecretReference() { Name = "login" }, "seq"));
            Assert.IsFalse(LookupService.IsCrossNamespace(new V1SecretReference() { Name = "login", NamespaceProperty = "" }, "seq"));
        }

        [TestMethod]
        public void SameNamespaceIsLocal()
        {
            Assert.IsFalse(LookupService.IsCrossNamespace(new V1SecretReference() { Name = "login", NamespaceProperty = "seq" }, "seq"));
        }

        [TestMethod]
        public void OtherNamespaceIsRefused()
        {
            Assert.IsTrue(LookupService.IsCrossNamespace(new V1SecretReference() { Name = "login", NamespaceProperty = "other-team" }, "seq"));
        }

    }

}
