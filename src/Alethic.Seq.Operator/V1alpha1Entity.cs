using System.Linq;

namespace Alethic.Seq.Operator
{

    public interface V1alpha1Entity<TSpec, TStatus, TConf, TInfo>
        where TSpec : V1alpha1EntitySpec<TConf>
        where TStatus : V1alpha1EntityStatus<TInfo>
        where TConf : class
        where TInfo : class
    {

        /// <summary>
        /// Gets the specification of the entity.
        /// </summary>
        TSpec Spec { get; }

        /// <summary>
        /// Gets the current status of the entity.
        /// </summary>
        TStatus Status { get; }

    }

}
