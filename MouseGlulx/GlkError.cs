using System;
using System.Runtime.Serialization;

namespace MouseGlulx.Glk
{
    [Serializable]
    internal class GlkError : Exception
    {
        public GlkError()
        {
        }

        public GlkError(string message) : base(message)
        {
        }

        public GlkError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GlkError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}