using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes how to deploy a new instance of Seq.
    /// </summary>
    public class InstanceDeploymentStatus
    {

        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; set; }

    }

}
