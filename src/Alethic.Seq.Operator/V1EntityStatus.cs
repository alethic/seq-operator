using System.Collections.Generic;
using System.Text.Json.Serialization;

using k8s.Models;

namespace Alethic.Seq.Operator
{

    public interface V1EntityStatus<TInfo>
        where TInfo : class
    {

        TInfo? Info { get; set; }

        IList<V1EntityCondition> Conditions { get; set; }

    }

}
