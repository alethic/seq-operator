using System.Linq;

namespace Alethic.Seq.Operator.Models
{

    public interface V1Alpha1Entity<TSpec, TStatus, TConf, TInfo>
        where TSpec : V1Alpha1EntitySpec<TConf>
        where TStatus : V1Alpha1EntityStatus<TInfo>
        where TConf : class
        where TInfo : class
    {

        /// <summary>
        /// Gets the policy set on the entity.
        /// </summary>
        /// <returns></returns>
        public V1Alpha1EntityPolicyType[] GetPolicy() => Spec.Policy ?? [
            V1Alpha1EntityPolicyType.Create,
            V1Alpha1EntityPolicyType.Update,
        ];

        /// <summary>
        /// Gets whether or not this entity has this policy applied.
        /// </summary>
        /// <param name="policy"></param>
        /// <returns></returns>
        public bool HasPolicy(V1Alpha1EntityPolicyType policy)
        {
            return GetPolicy().Contains(policy);
        }

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
