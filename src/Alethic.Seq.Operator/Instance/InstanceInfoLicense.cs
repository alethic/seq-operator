using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Instance
{

    public class InstanceInfoLicense
    {

        [JsonPropertyName("automaticallyRefresh")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? AutomaticallyRefresh { get;  set; }

        [JsonPropertyName("clustered")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Clustered { get;  set; }

        [JsonPropertyName("canRenewOnlineNow")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? CanRenewOnlineNow { get;  set; }

        [JsonPropertyName("storageLimitGigabytes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? StorageLimitGigabytes { get;  set; }

        [JsonPropertyName("isValid")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsValid { get;  set; }

        [JsonPropertyName("isWarning")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsWarning { get;  set; }

        [JsonPropertyName("isSingleUser")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsSingleUser { get;  set; }

        [JsonPropertyName("subscriptionId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SubscriptionId { get;  set; }

        [JsonPropertyName("status")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Status { get;  set; }
    }

}
