using MouseEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseEngine.Lowlevel
{
    interface IUnsubstitutedBytes
    {
        int Count
        {
            get;
        }
        void Combine(IUnsubstitutedBytes other);

        void Combine(IUnsubstitutedBytes other, int position);

        IEnumerable<byte> bytes
        {
            get;
        }
        IEnumerable<Substitution> substitutions
        {
            get;
        }

        void resize(int newsize);

        void WriteSlice(int position, byte[] what)
        ;
        
    }

    class UnsubstitutedBytes: IUnsubstitutedBytes
    {
        
        public UnsubstitutedBytes(byte[] bytes):this(bytes,new Substitution[0])
        {

        }

        public UnsubstitutedBytes(byte[] bytes, Substitution[] substitutions)
        {
            Bytes = bytes;
            Substitutions = substitutions;
        }

        byte[] Bytes;

        public void resize(int size)
        {
            byte[] newbytes = new byte[size];
            newbytes.WriteSlice(0,Math.Min(size,Bytes.Length),Bytes);
            Bytes = newbytes;
        }

        public IEnumerable<byte> bytes
        {
            get
            {
                return Bytes;
            }
            set
            {
                Bytes = value.ToArray();
            }
        }

        private Substitution[] Substitutions;

        public IEnumerable<Substitution> substitutions
        {
            get
            {
                return Substitutions;
            }
            set
            {
                Substitutions = value.ToArray();
            }
        }

        public int Count
        {
            get
            {
                
                return Bytes.Length;
            }
        }

        

        public void Combine(IUnsubstitutedBytes other)
        {
            Combine(other, Bytes.Length);
        }

        public void Combine(IUnsubstitutedBytes other, int position)
        {
            if (position+other.Count > Count)
            {
                resize(position + other.Count);
            }
            Bytes.WriteSlice(position, other.bytes.ToArray());
            List<Substitution> f = new List<Substitution>(substitutions);
            foreach (Substitution sub in other.substitutions)
            {
                Substitution l = sub;
                l.position += position;
                f.Add(l);
            }
            Substitutions = f.ToArray();
        }

        public void WriteSlice(int position, byte[] what)
        {
            Bytes.WriteSlice(position, what);
        }
    }

    class DynamicUnsubstitutedBytes: IUnsubstitutedBytes
    {
        public DynamicUnsubstitutedBytes(IEnumerable<byte> bytes):this(bytes,new Substitution[0])
        {

        }

        public DynamicUnsubstitutedBytes(IEnumerable<byte> bytes, IEnumerable<Substitution> substitutions)
        {
            Bytes = bytes.ToList();
            Substitutions = substitutions.ToList();
        }

        public DynamicUnsubstitutedBytes(List<byte> bytes, List<Substitution> substitutions)
        {
            Bytes = bytes;
            Substitutions = substitutions;
        }

        public DynamicUnsubstitutedBytes()
        {
            Bytes = new List<byte>();
            Substitutions = new List<Substitution>();
        }



        List<byte> Bytes;
        List<Substitution> Substitutions;
        public IEnumerable<byte> bytes
        {
            get
            {
                return Bytes;
            }
            set
            {
                Bytes = value.ToList();
            }
        }

        public int Count
        {
            get
            {
                return Bytes.Count;
            }
        }

        public IEnumerable<Substitution> substitutions
        {
            get
            {
                return Substitutions;
            }
            set
            {
                substitutions = value.ToList();
            }
        }

        public void Combine(IUnsubstitutedBytes other)
        {
            int index=Bytes.Count;
            Bytes.AddRange(other.bytes);
            foreach (Substitution b in other.substitutions)
            {
                Substitutions.Add(b.moveTo(b.position + index));
            }

        }

        public void Combine(IUnsubstitutedBytes other, int position)
        {
            int index = 0;
            int sum;
            IEnumerator<byte> numerator = other.bytes.GetEnumerator();
            while (index < other.Count)
            {
                sum = index + position;
                if (sum > Count)
                {
                    Bytes.Add(0);
                }
                else if (sum == Count)
                {
                    numerator.MoveNext();
                    Bytes.Add(numerator.Current);
                    index += 1;
                }
                else
                {
                    numerator.MoveNext();
                    Bytes[sum] = numerator.Current;
                    index += 1;
                }
            }

            foreach (Substitution s in other.substitutions)
            {
                Substitutions.Add(s.moveTo(s.position + position));
            }
        }

        public void resize(int newsize)
        {
            while (newsize>Bytes.Count)
            {
                Bytes.Add(0);
            }
            while (newsize < Bytes.Count)
            {
                Bytes.RemoveAt(Bytes.Count - 1);
            }
        }

        public void WriteSlice(int position, byte[] what)
        {
            Combine(new UnsubstitutedBytes(what, new Substitution[0]), position);
        }
    }

}
