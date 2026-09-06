using System;

namespace Alethic.Seq.Operator.Options
{

    /// <summary>
    /// Configuration for reconciliation behavior.
    /// </summary>
    public class ReconciliationOptions
    {

        /// <summary>
        /// The interval between periodic reconciliation cycles.
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The interval between retries when reconcillation fails.
        /// </summary>
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The maximum number of resources reconciled in parallel. Bounds the burst of Seq API calls issued when many
        /// resources reconcile at once, such as on operator startup.
        /// </summary>
        public int MaxParallelReconciliations { get; set; } = 4;

    }

}