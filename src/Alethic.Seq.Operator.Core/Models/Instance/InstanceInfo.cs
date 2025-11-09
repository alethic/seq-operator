using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Models.Instance
{

    public partial class InstanceInfo
    {

        [JsonPropertyName("enabledFeatures")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? EnabledFeatures { get; set; }

    }

}
