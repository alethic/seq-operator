using Alethic.Seq.Operator.Instance;

using k8s.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alethic.Seq.Operator.Tests
{

    /// <summary>
    /// Covers the permissions an Instance grants when it does not set <c>spec.permissions</c>.
    /// </summary>
    [TestClass]
    public class InstancePermissionTests
    {

        static readonly V1Instance Instance = new()
        {
            Metadata = new V1ObjectMeta() { NamespaceProperty = "seq", Name = "seq" },
            Spec = new V1InstanceSpec(),
        };

        static V1Namespace Namespace(string name) => new() { Metadata = new V1ObjectMeta() { Name = name } };

        [TestMethod]
        public void DefaultsAllowEveryKindInTheSameNamespace()
        {
            var ns = Namespace("seq");
            Assert.IsTrue(Instance.CheckPermission(ns, false, p => p.ApiKeys?.Create));
            Assert.IsTrue(Instance.CheckPermission(ns, false, p => p.Alerts?.Create));
            Assert.IsTrue(Instance.CheckPermission(ns, false, p => p.RetentionPolicies?.Create));
            Assert.IsTrue(Instance.CheckPermission(ns, false, p => p.Signals?.Create));
            Assert.IsTrue(Instance.CheckPermission(ns, false, p => p.Signals?.Attach));
        }

        [TestMethod]
        public void DefaultsAllowNothingFromOtherNamespaces()
        {
            var ns = Namespace("app");
            Assert.IsFalse(Instance.CheckPermission(ns, false, p => p.ApiKeys?.Create));
            Assert.IsFalse(Instance.CheckPermission(ns, false, p => p.Signals?.Create));
        }

    }

}
