using Alethic.Seq.Operator.Core.Models;

namespace Alethic.Seq.Operator.Models
{

    public interface V1InstanceEntitySpec<TConf> : V1EntitySpec<TConf>
        where TConf : class
    {

        V1InstanceReference? InstanceRef { get; set; }

    }

}
