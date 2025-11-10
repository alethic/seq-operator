using System.Collections.Generic;
using System.Text.Json.Serialization;

using k8s.Models;

namespace Alethic.Seq.Operator.Models
{

    public interface V1Alpha1EntityStatus<TInfo>
        where TInfo : class
    {

        TInfo? Info { get; set; }

        IList<V1Alpha1Condition> Conditions { get; set; }

    }

}
