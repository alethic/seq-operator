using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator
{

    public class V1alpha1Condition
    {

        /// <summary>
        /// Type of condition for a component.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Status of the condition.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Condition error code for a component.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// Message about the condition for a component.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

    }

}
