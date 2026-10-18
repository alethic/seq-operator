using Alethic.Seq.Operator.Alerts;
using Alethic.Seq.Operator.ApiKey;
using Alethic.Seq.Operator.Instance;
using Alethic.Seq.Operator.RetentionPolicy;
using Alethic.Seq.Operator.Signals;

using KubeOps.Abstractions.Reconciliation.Controller;

namespace Alethic.Seq.Operator.Finalizers.Legacy
{

    // Every finalizer identifier this operator has ever written stays registered here. KubeOps derives the identifier
    // from the class name, so these classes keep the names the finalizers had while they were version-qualified. They
    // are never attached to new entities -- ControllerBase attaches only the current identifier and strips these --
    // but an entity created by an older release still carries one, and the reconciler returns without removing a
    // finalizer it cannot resolve, leaving the entity wedged in Terminating forever. Never delete one of these.

    /// <summary>
    /// Drains <c>seq.k8s.datalust.co/v1alpha1alertfinalizer</c>, the identifier written for
    /// <see cref="V1Alert"/> while the finalizer class was named <c>V1alpha1AlertFinalizer</c>.
    /// </summary>
    public class V1alpha1AlertFinalizer : EntityFinalizerBase<V1Alert>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1alpha1AlertFinalizer(IEntityController<V1Alert> controller) :
            base(controller)
        {

        }

    }

    /// <summary>
    /// Drains <c>seq.k8s.datalust.co/v1alpha1apikeyfinalizer</c>, the identifier written for
    /// <see cref="V1ApiKey"/> while the finalizer class was named <c>V1alpha1ApiKeyFinalizer</c>.
    /// </summary>
    public class V1alpha1ApiKeyFinalizer : EntityFinalizerBase<V1ApiKey>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1alpha1ApiKeyFinalizer(IEntityController<V1ApiKey> controller) :
            base(controller)
        {

        }

    }

    /// <summary>
    /// Drains <c>seq.k8s.datalust.co/v1alpha1instancefinalizer</c>, the identifier written for
    /// <see cref="V1Instance"/> while the finalizer class was named <c>V1alpha1InstanceFinalizer</c>.
    /// </summary>
    public class V1alpha1InstanceFinalizer : EntityFinalizerBase<V1Instance>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1alpha1InstanceFinalizer(IEntityController<V1Instance> controller) :
            base(controller)
        {

        }

    }

    /// <summary>
    /// Drains <c>seq.k8s.datalust.co/v1alpha1retentionpolicyfinalizer</c>, the identifier written for
    /// <see cref="V1RetentionPolicy"/> while the finalizer class was named <c>V1alpha1RetentionPolicyFinalizer</c>.
    /// </summary>
    public class V1alpha1RetentionPolicyFinalizer : EntityFinalizerBase<V1RetentionPolicy>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1alpha1RetentionPolicyFinalizer(IEntityController<V1RetentionPolicy> controller) :
            base(controller)
        {

        }

    }

    /// <summary>
    /// Drains <c>seq.k8s.datalust.co/v1alpha1signalfinalizer</c>, the identifier written for
    /// <see cref="V1Signal"/> while the finalizer class was named <c>V1alpha1SignalFinalizer</c>.
    /// </summary>
    public class V1alpha1SignalFinalizer : EntityFinalizerBase<V1Signal>
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="controller"></param>
        public V1alpha1SignalFinalizer(IEntityController<V1Signal> controller) :
            base(controller)
        {

        }

    }

}
