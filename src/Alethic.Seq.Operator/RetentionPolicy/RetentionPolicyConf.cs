using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.RetentionPolicy
{

    public class RetentionPolicyConf
    {

        [JsonPropertyName("retentionTime")]
        public string? RetentionTime { get; set; }

    }

}
