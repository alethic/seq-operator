using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    public class V1alpha1InstanceStatus : V1alpha1EntityStatus<InstanceInfo>
    {

        /// <summary>
        /// Information about the associated deployment.
        /// </summary>
        [JsonPropertyName("deployment")]
        public InstanceDeploymentStatus? Deployment { get; set; }

        [JsonPropertyName("info")]
        public InstanceInfo? Info { get; set; }

        [JsonPropertyName("conditions")]
        public IList<V1alpha1Condition> Conditions { get; set; } = new List<V1alpha1Condition>();

    }

}
