using System;
using System.Runtime.Serialization;

namespace MouseGlulx
{
    [Serializable]
    internal class GlulxTypeError : Exception
    {
        public GlulxTypeError()
        {
        }

        public GlulxTypeError(string message) : base(message)
        {
        }

        public GlulxTypeError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GlulxTypeError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}