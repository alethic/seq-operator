namespace Alethic.Seq.Operator.Models
{

    public interface V1InstanceEntity<TSpec, TStatus, TConf> : V1Entity<TSpec, TStatus, TConf>
        where TSpec : V1InstanceEntitySpec<TConf>
        where TStatus : V1InstanceEntityStatus
        where TConf : class
    {



    }

}
