using System.Text.Json.Serialization;

using Seq.Api.Model.Shared;

namespace Alethic.Seq.Operator.Shared
{

    public class DescriptiveFilter
    {

        /// <summary>
        /// Transforms a <see cref="DescriptiveFilterPart"/> to a <see cref="DescriptiveFilter"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DescriptiveFilter FromApi(DescriptiveFilterPart source) => new DescriptiveFilter()
        {
            DescriptionIsExcluded = source.DescriptionIsExcluded,
            Description = source.Description,
            Filter = source.Filter,
            FilterNonStrict = source.FilterNonStrict,
        };

        /// <summary>
        /// A friendly, human-readable description of the filter.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        /// <summary>
        /// If <c>true</c>, the description identifies events excluded by the filter. The
        /// Seq UI uses this to show the description in strikethrough.
        /// </summary>
        [JsonPropertyName("descriptionIsExcluded")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? DescriptionIsExcluded { get; set; }

        /// <summary>
        /// The strictly-valid expression language filter that identifies matching events.
        /// </summary>
        [JsonPropertyName("filter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Filter { get; set; }

        /// <summary>
        /// The original ("fuzzy") text entered by the user into the filter bar when
        /// creating the filter. This may not be syntactically valid, e.g. it may be
        /// interpreted as free text - hence while it's displayed in the UI and forms the
        /// basis of user editing of the filter, the <see cref="Filter"/> value is executed.
        /// </summary>
        [JsonPropertyName("filterNonStrict")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FilterNonStrict { get; set; }

    }

}
