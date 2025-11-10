using System;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Models.RetentionPolicy
{

    public class RetentionPolicyConf
    {

        [JsonPropertyName("retentionTime")]
        public string? RetentionTime { get; set; }

    }

}
