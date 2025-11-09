using System.Text.Json.Serialization;

using Alethic.Seq.Operator.Core.Models.Instance;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Models
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "k8s.seq.datalust.co", ApiVersion = "v1alpha1", Kind = "Instance")]
    [KubernetesEntityShortNames("seqinstance")]
    public partial class V1Alpha1Instance :
        CustomKubernetesEntity<V1Alpha1Instance.SpecDef, V1Alpha1Instance.StatusDef>,
        V1Alpha1Entity<V1Alpha1Instance.SpecDef, V1Alpha1Instance.StatusDef, InstanceConf, InstanceInfo>
    {

        public class SpecDef : V1Alpha1EntitySpec<InstanceConf>
        {

            /// <summary>
            /// Describes information to authenticate with Seq using a username and password.
            /// </summary>
            public class LoginAuthentication
            {

                [JsonPropertyName("secretRef")]
                [Required]
                public V1SecretReference? SecretRef { get; set; }

            }

            /// <summary>
            /// Describes information to authenticate with Seq using an ApiKey.
            /// </summary>
            public class ApiKeyAuthentication
            {

                [JsonPropertyName("secretRef")]
                [Required]
                public V1SecretReference? SecretRef { get; set; }

            }

            /// <summary>
            /// Describes connection information to reach a remote Seq instance.
            /// </summary>
            public class ConnectionDef
            {

                /// <summary>
                /// Endpoint of the Seq instance in URI format.
                /// </summary>
                [JsonPropertyName("endpoint")]
                [Required]
                public string? Endpoint { get; set; }

                /// <summary>
                /// If specified, indicates login by username/password should be attempted.
                /// </summary>
                [JsonPropertyName("login")]
                public LoginAuthentication? Login { get; set; }

                /// <summary>
                /// If specified, indicates operations should use ApiKey authentication.
                /// </summary>
                [JsonPropertyName("apiKey")]
                public ApiKeyAuthentication? ApiKey { get; set; }

            }

            /// <summary>
            /// Describes how to deploy a new instance of Seq.
            /// </summary>
            public class DeploymentDef
            {

                /// <summary>
                /// Secret to use for the management API key.
                /// </summary>
                [JsonPropertyName("secretRef")]
                [Required]
                public V1SecretReference? SecretRef { get; set; }

            }

            /// <summary>
            /// Describes the permitted operations on the entity.
            /// </summary>
            [JsonPropertyName("policy")]
            public V1Alpha1EntityPolicyType[]? Policy { get; set; }

            /// <summary>
            /// Connection information for the instance.
            /// </summary>
            [JsonPropertyName("connection")]
            public ConnectionDef? Connection { get; set; }

            /// <summary>
            /// Connection information for the instance.
            /// </summary>
            [JsonPropertyName("deployment")]
            public DeploymentDef? Deployment { get; set; }

            /// <summary>
            /// Configuration to apply when initializing the entity for the first time.
            /// </summary>
            [JsonPropertyName("init")]
            public InstanceConf? Init { get; set; }

            /// <summary>
            /// Configuration to apply when reconcilling the entity.
            /// </summary>
            [JsonPropertyName("conf")]
            [Required]
            public InstanceConf? Conf { get; set; }

        }

        public class StatusDef : V1Alpha1EntityStatus<InstanceInfo>
        {

            /// <summary>
            /// Describes the managed deployment.
            /// </summary>
            public class DeploymentStatus
            {



            }

            /// <summary>
            /// Information about the associated deployment.
            /// </summary>
            [JsonPropertyName("deployment")]
            public DeploymentStatus? Deployment { get; set; }

            [JsonPropertyName("info")]
            public InstanceInfo? Info { get; set; }

        }

    }

}
