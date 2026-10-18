using System.Linq;

using k8s;
using k8s.Models;

namespace Alethic.Seq.Operator
{

    /// <summary>
    /// Ownership checks that survive an API version change of the owner.
    /// </summary>
    /// <remarks>
    /// The Kubernetes client's <c>IsOwnedBy</c> compares an owner reference's <c>apiVersion</c> as well as its UID, so
    /// objects created while their owner was read as <c>v1alpha1</c> are not recognised once it is read as <c>v1</c>.
    /// Ownership is decided by UID here, as Kubernetes' own garbage collector does.
    /// </remarks>
    public static class OwnershipExtensions
    {

        /// <summary>
        /// Returns whether <paramref name="obj"/> has an owner reference to <paramref name="owner"/>, whatever API
        /// version that reference names.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static bool IsOwnedByEntity(this IMetadata<V1ObjectMeta> obj, IKubernetesObject<V1ObjectMeta> owner)
        {
            return owner.Uid() is { Length: > 0 } uid && obj.Metadata?.OwnerReferences?.Any(i => i.Uid == uid) == true;
        }

        /// <summary>
        /// Rewrites any owner reference to <paramref name="owner"/> to name the API version the owner was read as, so
        /// references written under an older version do not outlive it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static T RefreshOwnerReference<T>(this T obj, IKubernetesObject<V1ObjectMeta> owner)
            where T : IMetadata<V1ObjectMeta>
        {
            if (owner.ApiVersion is { Length: > 0 } apiVersion && owner.Uid() is { Length: > 0 } uid && obj.Metadata?.OwnerReferences is { } references)
                foreach (var reference in references)
                    if (reference.Uid == uid)
                        reference.ApiVersion = apiVersion;

            return obj;
        }

    }

}
