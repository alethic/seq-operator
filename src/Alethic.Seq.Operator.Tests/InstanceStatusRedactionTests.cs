using Alethic.Seq.Operator.Instance;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alethic.Seq.Operator.Tests
{

    /// <summary>
    /// Covers the redaction of SSO secrets read back from Seq before they are written to an Instance's status, which
    /// anyone with the view role can read.
    /// </summary>
    [TestClass]
    public class InstanceStatusRedactionTests
    {

        [TestMethod]
        public void RedactsEntraClientKey()
        {
            var info = new InstanceInfo()
            {
                Auth = new InstanceConfAuthenticationSpec()
                {
                    Entra = new InstanceConfAuthenticationSpec.EntraAuth() { ClientId = "client", ClientKey = "hunter2" },
                },
            };

            V1alpha1InstanceController.RedactSecrets(info);

            Assert.AreEqual(V1alpha1InstanceController.RedactedValue, info.Auth.Entra.ClientKey);
            Assert.AreEqual("client", info.Auth.Entra.ClientId);
        }

        [TestMethod]
        public void RedactsOidcClientSecret()
        {
            var info = new InstanceInfo()
            {
                Auth = new InstanceConfAuthenticationSpec()
                {
                    Oidc = new InstanceConfAuthenticationSpec.OidcAuth() { ClientId = "client", ClientSecret = "hunter2", Authority = "https://login.example.com" },
                },
            };

            V1alpha1InstanceController.RedactSecrets(info);

            Assert.AreEqual(V1alpha1InstanceController.RedactedValue, info.Auth.Oidc.ClientSecret);
            Assert.AreEqual("client", info.Auth.Oidc.ClientId);
            Assert.AreEqual("https://login.example.com", info.Auth.Oidc.Authority);
        }

        /// <summary>
        /// An unset secret stays unset, so status still shows whether one has been configured.
        /// </summary>
        [TestMethod]
        public void LeavesUnsetSecretsUnset()
        {
            var info = new InstanceInfo()
            {
                Auth = new InstanceConfAuthenticationSpec()
                {
                    Oidc = new InstanceConfAuthenticationSpec.OidcAuth() { ClientId = "client", ClientSecret = "" },
                },
            };

            V1alpha1InstanceController.RedactSecrets(info);

            Assert.AreEqual("", info.Auth.Oidc.ClientSecret);
        }

        [TestMethod]
        public void ToleratesNoAuth()
        {
            var info = new InstanceInfo();
            Assert.AreSame(info, V1alpha1InstanceController.RedactSecrets(info));
            Assert.IsNull(info.Auth);
        }

    }

}
