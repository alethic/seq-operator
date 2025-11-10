namespace Alethic.Seq.Operator
{

    public interface V1alpha1InstanceEntityStatus<TConf> : V1alpha1EntityStatus<TConf>
        where TConf : class
    {

        string? Id { get; set; }

    }

}
