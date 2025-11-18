using System.Collections.Generic;
using System.Text.Json.Serialization;

using Alethic.Seq.Operator.Shared;

namespace Alethic.Seq.Operator.Signals
{

    public class SignalInfo
    {

        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        [JsonPropertyName("explicitGroupName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ExplicitGroupName { get; set; }

        [JsonPropertyName("isIndexSuppressed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsIndexSuppressed { get; set; }

        [JsonPropertyName("isProtected")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsProtected { get; set; }

        [JsonPropertyName("grouping")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SignalGrouping? Grouping { get; set; }

        [JsonPropertyName("filters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<DescriptiveFilter>? Filters { get; set; }

        [JsonPropertyName("columns")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SignalColumn>? Columns { get; set; }

    }

}
