namespace Alethic.Seq.Operator
{

    public interface V1InstanceEntitySpec<TConf> : V1EntitySpec<TConf>
        where TConf : class
    {

        V1InstanceReference? InstanceRef { get; set; }

    }

}
