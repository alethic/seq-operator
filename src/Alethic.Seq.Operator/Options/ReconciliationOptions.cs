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
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    }

}