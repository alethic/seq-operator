namespace Alethic.Seq.Operator
{

    public interface V1alpha1InstanceEntitySpec<TConf> : V1alpha1EntitySpec<TConf>
        where TConf : class
    {

        V1alpha1InstanceReference? InstanceRef { get; set; }

    }

}
