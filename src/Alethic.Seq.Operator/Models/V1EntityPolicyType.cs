namespace Alethic.Seq.Operator.Models
{

    public enum V1EntityPolicyType
    {

        /// <summary>
        /// Allows the operator to create the associated entity in the tenant.
        /// </summary>
        Create,

        /// <summary>
        /// Allows the operator to update the associated entity in the tenant.
        /// </summary>
        Update,

        /// <summary>
        /// Allows the operator to delete the associated entity in the tenant.
        /// </summary>
        Delete,

    }

}
