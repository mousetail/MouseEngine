using System;
using System.Runtime.Serialization;

namespace MouseEngine.Errors
{
    [Serializable]
    internal class ItemMatchException : ParsingException
    {
        public ItemMatchException():base("Error")
        {
        }

        public ItemMatchException(string message) : base(message)
        {
        }
        /*
        public ItemMatchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ItemMatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        */
    }
}