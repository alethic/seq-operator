namespace Alethic.Seq.Operator
{

    public interface V1InstanceEntity<TSpec, TStatus, TConf, TInfo> : V1Entity<TSpec, TStatus, TConf, TInfo>
        where TSpec : V1InstanceEntitySpec<TConf>
        where TStatus : V1InstanceEntityStatus<TInfo>
        where TConf : class
        where TInfo : class
    {

    }

}
