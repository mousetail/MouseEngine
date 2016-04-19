using System;
using System.Runtime.Serialization;

namespace MouseGlulx
{
    [Serializable]
    internal class StackError : Exception
    {
        public StackError()
        {
        }

        public StackError(string message) : base(message)
        {
        }

        public StackError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected StackError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}