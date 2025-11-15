using System.Text.Json.Serialization;

using k8s.Models;

namespace Alethic.Seq.Operator.Instance
{

    public class InstancePermissionNamespaces
    {

        /// <summary>
        /// Specifies the set of namespaces this permission applies to.
        /// </summary>
        [JsonPropertyName("from")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InstancePermissionNamespaceFrom? From { get; set; }

        /// <summary>
        /// Specifies how to select the namespaces. Selector must be specified when From is set to "Selector". In that
        /// case, only entities in Namespaces matching this Selector will be selected by this Instance. This field is
        /// ignored for other values of "From".
        /// </summary>
        [JsonPropertyName("selector")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public V1LabelSelector? Selector { get; set; }

    }

}
