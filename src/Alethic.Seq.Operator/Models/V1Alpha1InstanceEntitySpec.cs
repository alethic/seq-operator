using Alethic.Seq.Operator.Core.Models;

namespace Alethic.Seq.Operator.Models
{

    public interface V1Alpha1InstanceEntitySpec<TConf> : V1Alpha1EntitySpec<TConf>
        where TConf : class
    {

        V1Alpha1InstanceReference? InstanceRef { get; set; }

    }

}
