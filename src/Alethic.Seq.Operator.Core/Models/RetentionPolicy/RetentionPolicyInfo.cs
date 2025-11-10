using System;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Models.RetentionPolicy
{

    public class RetentionPolicyInfo
    {

        [JsonPropertyName("retentionTime")]
        public string? RetentionTime { get; set; }

    }

}
