namespace Alethic.Seq.Operator.Instance
{

    public enum InstancePermissionNamespaceFrom
    {

        /// <summary>
        /// Entities in all namespaces may be attached to this Instance.
        /// </summary>
        All,

        /// <summary>
        /// Only entities in namespaces selected by the selector may be attached to this Instnace.
        /// </summary>
        Selector,

        /// <summary>
        /// Only entities in the same namespace as the Instance may be attached to this Instance.
        /// </summary>
        Same,

        /// <summary>
        /// No entities may be attached to this Instance.
        /// </summary>
        None,

    }

}
