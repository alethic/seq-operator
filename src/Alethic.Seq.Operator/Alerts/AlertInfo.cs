using System.Collections.Generic;
using System.Text.Json.Serialization;

using Alethic.Seq.Operator.Shared;

namespace Alethic.Seq.Operator.Alerts
{

    public class AlertInfo
    {

        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        [JsonPropertyName("ownerId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OwnerId { get; set; }

        [JsonPropertyName("protected")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Protected { get; set; }

        [JsonPropertyName("disabled")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Disabled { get; set; }

        [JsonPropertyName("where")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Where { get; set; }

        [JsonPropertyName("groupBy")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<AlertGroupingColumn>? GroupBy { get; set; }

        [JsonPropertyName("timeGrouping")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TimeGrouping { get; set; }

        [JsonPropertyName("select")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList< AlertSelectColumn>? Select { get; set; }

        [JsonPropertyName("having")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Having { get; set; }

        [JsonPropertyName("notificationLevel")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LogEventLevel? NotificationLevel { get; set; }

        [JsonPropertyName("notificationProperties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string?>? NotificationProperties { get; set; }

        [JsonPropertyName("suppressionTime")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SuppressionTime { get; set; }

    }

}
