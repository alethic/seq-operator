using System;
using System.Collections.Generic;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.Instance
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1alpha1", Kind = "Instance")]
    [KubernetesEntityShortNames("seqinstance")]
    public partial class V1alpha1Instance :
        CustomKubernetesEntity<V1alpha1InstanceSpec, V1alpha1InstanceStatus>,
        V1alpha1Entity<V1alpha1InstanceSpec, V1alpha1InstanceStatus, InstanceConf, InstanceInfo>
    {

        /// <summary>
        /// Evaluates whether the given ns matches the specified label selector.
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        bool EvalLabelSelector(V1Namespace ns, V1LabelSelector selector)
        {
            // evaluate labels
            if (selector.MatchLabels is { } matchLabels)
                foreach (var kvp in matchLabels)
                    if (ns.GetLabel(kvp.Key) is not string s || kvp.Value != s)
                        return false;

            // evaluate expressions
            if (selector.MatchExpressions is { } matchExpressions)
                foreach (var matchExpression in matchExpressions)
                    throw new NotSupportedException("MatchExpressions are not yet supported.");

            return true;
        }

        /// <summary>
        /// Evaluates the given <see cref="InstancePermission"/> against the specified namespace name.
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        bool MatchPermission(V1Namespace ns, InstancePermission permission)
        {
            switch (permission.Namespaces?.From ?? InstancePermissionNamespaceFrom.Same)
            {
                case InstancePermissionNamespaceFrom.All:
                    return true;
                case InstancePermissionNamespaceFrom.Same:
                    return ns.Name() == Metadata.Namespace();
                case InstancePermissionNamespaceFrom.Selector:
                    return permission.Namespaces?.Selector is { } selector && EvalLabelSelector(ns, selector);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the <see cref="InstancePermission"/> that apply to the specified namespace.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        IEnumerable<InstancePermission> MatchPermissions(V1Namespace ns)
        {
            foreach (var p in Spec.Permissions ?? [])
                if (MatchPermission(ns, p))
                    yield return p;
        }

        /// <summary>
        /// Checks the 
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="defaultValue"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public bool CheckPermission(V1Namespace ns, bool defaultValue, Func<InstancePermission, bool?> func)
        {
            var value = defaultValue;

            // check each permission for a deny (false)
            foreach (var p in MatchPermissions(ns))
                if (func(p) == false)
                    return false;

            // for at least one allow permission for an allow (true)
            foreach (var p in MatchPermissions(ns))
                if (func(p) == true)
                    return true;

            // fallback to default value
            return defaultValue;
        }

        /// <summary>
        /// Returns <c>true</c> if the specified <see cref="V1Namespace"/> can attach to existing alerts.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        public bool CanAttachAlert(V1Namespace ns)
        {
            return CheckPermission(ns, false, p => p.Alerts?.Attach);
        }

        /// <summary>
        /// Returns <c>true</c> if the specified <see cref="V1Namespace"/> can create alerts.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        public bool CanCreateAlert(V1Namespace ns)
        {
            return CheckPermission(ns, false, p => p.Alerts?.Create);
        }

        /// <summary>
        /// Returns <c>true</c> if the specified <see cref="V1Namespace"/> can attach to existing API keys.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        public bool CanAttachApiKey(V1Namespace ns)
        {
            return CheckPermission(ns, false, p => p.ApiKeys?.Attach);
        }

        /// <summary>
        /// Returns <c>true</c> if the specified <see cref="V1Namespace"/> can create API keys.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        public bool CanCreateApiKey(V1Namespace ns)
        {
            return CheckPermission(ns, false, p => p.ApiKeys?.Create);
        }

        /// <summary>
        /// Returns <c>true</c> if the specified <see cref="V1Namespace"/> can attach to existing retention policies.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        public bool CanAttachRetentionPolicy(V1Namespace ns)
        {
            return CheckPermission(ns, false, p => p.RetentionPolicies?.Attach);
        }

        /// <summary>
        /// Returns <c>true</c> if the specified <see cref="V1Namespace"/> can create retention policies.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        public bool CanCreateRetentionPolicy(V1Namespace ns)
        {
            return CheckPermission(ns, false, p => p.RetentionPolicies?.Create);
        }

    }

}
