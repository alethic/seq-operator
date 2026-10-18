namespace Alethic.Seq.Operator
{

    public interface V1InstanceEntityStatus<TConf> : V1EntityStatus<TConf>
        where TConf : class
    {

        string? Id { get; set; }

    }

}
