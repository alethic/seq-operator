using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Models.ApiKey
{

    public class ApiKeyInputSettings
    {

        /// <summary>
        /// Properties that will be automatically added to all events ingested using the key. These will override any properties with
        /// the same names already present on the event.
        /// </summary>
        [JsonPropertyName("appliedProperties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiKeyEventProperty[]? AppliedProperties { get; set; }

        /// <summary>
        /// A filter that selects events to ingest. If <c>null</c>, all events received using the key will be ingested.
        /// </summary>
        [JsonPropertyName("filter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiKeyDescriptiveFilter? Filter { get; set; }

        /// <summary>
        /// A minimum level at which events received using the key will be ingested. The level hierarchy understood by Seq is fuzzy
        /// enough to handle most common leveling schemes. This value will be provided to callers so that they can dynamically
        /// filter events client-side, if supported.
        /// </summary>
        [JsonPropertyName("minimumLevel")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiKeyLogEventLevel? MinimumLevel { get; set; }

        /// <summary>
        /// If <c>true</c>, timestamps already present on the events will be ignored, and server timestamps used instead. This is not
        /// recommended for most use cases.
        /// </summary>
        [JsonPropertyName("useServerTimestamps")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? UseServerTimestamps { get; set; }

    }

}