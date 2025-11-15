namespace Alethic.Seq.Operator
{

    public interface V1alpha1InstanceEntity<TSpec, TStatus, TConf, TInfo> : V1alpha1Entity<TSpec, TStatus, TConf, TInfo>
        where TSpec : V1alpha1InstanceEntitySpec<TConf>
        where TStatus : V1alpha1InstanceEntityStatus<TInfo>
        where TConf : class
        where TInfo : class
    {

    }

}
