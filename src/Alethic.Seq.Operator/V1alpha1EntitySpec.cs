namespace Alethic.Seq.Operator
{

    public interface V1alpha1EntitySpec<TConf>
        where TConf : class
    {

        /// <summary>
        /// Set of operations allowed with the entity.
        /// </summary>
        V1alpha1EntityPolicyType[]? Policy { get; set; }

        /// <summary>
        /// Version of configuration used for initial creation.
        /// </summary>
        TConf? Init { get; set; }

        /// <summary>
        /// Version of configuration used for periodic reconciliation.
        /// </summary>
        TConf? Conf { get; set; }

    }

}
