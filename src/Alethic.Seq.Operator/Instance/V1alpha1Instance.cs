using System;

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
        /// Default permission set if none specified.
        /// </summary>
        readonly static InstancePermission DefaultPermissions = new InstancePermission()
        {
            Namespaces = new InstancePermissionNamespaces
            {
                From = InstancePermissionNamespaceFrom.Same,
            },
            Alerts = new InstanceAlertPermissions()
            {
                Attach = true,
                Create = true,
            },
            ApiKeys = new InstanceApiKeyPermissions()
            {
                Attach = true,
                Create = true,
                SetTitle = true,
                SetIngest = true,
                SetOrganization = true,
                SetProject = true,
                SetPublic = true,
                SetRead = true,
                SetSystem = true,
                SetWrite = true,
            },
            RetentionPolicies = new InstanceRetentionPolicyPermissions()
            {
                Attach = true,
                Create = true,
            }
        };

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
        /// Gets the first <see cref="InstancePermission"/> that apply to the specified namespace.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        InstancePermission? MatchPermissions(V1Namespace ns)
        {
            foreach (var p in Spec.Permissions ?? [DefaultPermissions])
                if (MatchPermission(ns, p))
                    return p;

            return null;
        }

        /// <summary>
        /// Checks the given permission boolean. 
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="defaultValue"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public bool CheckPermission(V1Namespace ns, bool defaultValue, Func<InstancePermission, bool?> func)
        {
            return MatchPermissions(ns) is { } p ? func(p) ?? defaultValue : defaultValue;
        }

    }

}
