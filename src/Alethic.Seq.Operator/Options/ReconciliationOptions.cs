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

    }

}