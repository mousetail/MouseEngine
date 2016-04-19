using System;
using System.Runtime.Serialization;

namespace MouseEngine.Errors
{
    [Serializable]
    internal class ItemMatchException : ParsingException, IMultiExDataException
    {
        IErrorData[] data;

        public ItemMatchException():base("Error")
        {
        }

        public ItemMatchException(string message) : base(message)
        {
#if DEBUG
            Console.WriteLine(data.toAdvancedString());
#endif
        }

        public ItemMatchException(string message, params IErrorData[] data):base(message)
        {
            this.data = data;
#if DEBUG
            Console.WriteLine(data.toAdvancedString());
#endif
        }

        public IErrorData[] getData()
        {
            return data;
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