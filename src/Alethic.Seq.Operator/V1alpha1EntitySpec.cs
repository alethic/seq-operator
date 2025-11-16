namespace Alethic.Seq.Operator
{

    public interface V1alpha1EntitySpec<TConf>
        where TConf : class
    {

        /// <summary>
        /// Configuration to be applied to the entity.
        /// </summary>
        TConf? Conf { get; set; }

    }

}
