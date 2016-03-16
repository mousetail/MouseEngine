using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace MouseGlulx
{
    public class ListStack<T> : List<T>
    {
        public T pop()
        {
            T tmp = this[Count - 1];
            RemoveAt(Count-1);
            return tmp;
        }

        public void push(T value)
        {
            Add(value);
        }

        public void pushRange(IEnumerable<T> value)
        {
            AddRange(value);
        }
    }

    [Serializable]
    internal class ByteAligningException : Exception
    {
        public ByteAligningException()
        {
        }

        public ByteAligningException(string message) : base(message)
        {
        }

        public ByteAligningException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ByteAligningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}