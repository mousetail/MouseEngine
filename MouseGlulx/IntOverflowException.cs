using System;
using System.Runtime.Serialization;

namespace MouseGlulx
{
    [Serializable]
    internal class IntOverflowException : Exception
    {
        public IntOverflowException()
        {
        }

        public IntOverflowException(string message) : base(message)
        {
        }

        public IntOverflowException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IntOverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}