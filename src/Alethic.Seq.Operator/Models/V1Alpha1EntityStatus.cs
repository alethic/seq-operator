namespace Alethic.Seq.Operator.Models
{

    public interface V1Alpha1EntityStatus<TInfo>
        where TInfo : class
    {

        TInfo? Info { get; set; }

    }

}
