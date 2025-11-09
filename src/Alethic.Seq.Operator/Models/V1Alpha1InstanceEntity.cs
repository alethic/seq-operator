namespace Alethic.Seq.Operator.Models
{

    public interface V1Alpha1InstanceEntity<TSpec, TStatus, TConf, TInfo> : V1Alpha1Entity<TSpec, TStatus, TConf, TInfo>
        where TSpec : V1Alpha1InstanceEntitySpec<TConf>
        where TStatus : V1Alpha1InstanceEntityStatus<TInfo>
        where TConf : class
        where TInfo : class
    {



    }

}
