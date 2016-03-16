using System;
using System.Runtime.Serialization;

namespace MouseGlulx.Glk
{
    [Serializable]
    internal class glkTypeError : Exception
    {
        public glkTypeError()
        {
        }

        public glkTypeError(string message) : base(message)
        {
        }

        public glkTypeError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected glkTypeError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}