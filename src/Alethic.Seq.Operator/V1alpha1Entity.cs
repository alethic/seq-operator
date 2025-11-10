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
        /// Gets the policy set on the entity.
        /// </summary>
        /// <returns></returns>
        public V1alpha1EntityPolicyType[] GetPolicy() => Spec.Policy ?? [
            V1alpha1EntityPolicyType.Create,
            V1alpha1EntityPolicyType.Update,
        ];

        /// <summary>
        /// Gets whether or not this entity has this policy applied.
        /// </summary>
        /// <param name="policy"></param>
        /// <returns></returns>
        public bool HasPolicy(V1alpha1EntityPolicyType policy)
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
