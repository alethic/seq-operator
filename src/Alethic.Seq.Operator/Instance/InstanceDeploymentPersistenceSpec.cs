using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes how to deploy a new instance of Seq.
    /// </summary>
    public class InstanceDeploymentPersistenceSpec
    {

        [JsonPropertyName("storageClassName")]
        public string? StorageClassName { get; set; }

        [JsonPropertyName("capacity")]
        public string? Capacity { get; set; }

    }

}
