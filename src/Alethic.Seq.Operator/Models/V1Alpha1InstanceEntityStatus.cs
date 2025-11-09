namespace Alethic.Seq.Operator.Models
{

    public interface V1Alpha1InstanceEntityStatus<TConf> : V1Alpha1EntityStatus<TConf>
        where TConf : class
    {

        string? Id { get; set; }

    }

}
