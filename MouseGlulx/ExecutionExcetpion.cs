using System;
using System.Runtime.Serialization;

namespace MouseGlulx
{
    

    [Serializable]
    internal class ExecutionException : Exception
    {
        public ExecutionException()
        {
        }

        public ExecutionException(string message) : base(message)
        {
        }

        public ExecutionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExecutionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class TypeCodeException : ExecutionException
    {
        public TypeCodeException()
        {
        }

        public TypeCodeException(string message) : base(message)
        {
        }

        public TypeCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TypeCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}