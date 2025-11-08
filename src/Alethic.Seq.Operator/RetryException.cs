using System;

namespace Alethic.Seq.Operator
{

    /// <summary>
    /// Represents an exception that indicates a retry can be attempted at a later time.
    /// </summary>
    public class RetryException : Exception
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="message"></param>
        public RetryException(string? message) :
            base(message)
        {

        }

    }

}
