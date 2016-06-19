using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MouseEngine.Lowlevel;

namespace MouseEngine
{
    class StringDatabase: IEnumerable<StringItem>
    {
        List<StringItem> strs=new List<StringItem>();

        public StringDatabase()
        {

        }

        public IEnumerator<StringItem> GetEnumerator()
        {
            return ((IEnumerable<StringItem>)strs).GetEnumerator();
        }

        public StringItem getStr(string s)
        {
            foreach (StringItem st in strs)
            {
                if (st.String.Equals(s))
                {
                    return st;
                }
            }
            StringItem f = new StringItem(s, Databases.ids++);
            strs.Add(f);
            return f;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<StringItem>)strs).GetEnumerator();
        }
    }

    class StringItem: IReferable, IByteable
    {
        static Encoding unicode = new UTF32Encoding(true, false, true);
        //static Encoding latin = Encoding.GetEncoding(20924); //IBM00924 or IBM latin 1 (A different IBM latin one also exists)
        static Encoding latin = (Encoding)Encoding.GetEncoding(858).Clone(); //OEM Multilingual Latin I

        public StringItem(string str, int identity)
        {
            String = str;
            id = identity;
            writer = null;
            latin.EncoderFallback = new EncoderExceptionFallback();
            
            
        }
        public string String;

        public override string ToString()
        {
            return String;
        }

        public int id;
        public static implicit operator string(StringItem forWhat)
        {
            return forWhat.String;
        }
        public static implicit operator int(StringItem forWhat)
        {
            return forWhat.id;
        }

        public static explicit operator ArgumentValue(StringItem forWhat)
        {
            return new ArgumentValue(addressMode.constint, substitutionType.WriterRef, forWhat.id, ClassDatabase.str);
        }

        WriterComponent writer;

        public WriterComponent getWriter()
        {
            return writer;
        }

        public void setWriter(WriterComponent w)
        {
            writer = w;
        }

        public IUnsubstitutedBytes toBytes()
        {
            byte[] strType= { 0xE0 };
            byte[] bytes;
            byte[] endbytes = { 0x00 };
            try {
                bytes=latin.GetBytes(String);
            }
            //catch (EncoderFallbackException) {
            catch (EncoderFallbackException) {
                bytes = unicode.GetBytes(String);
                strType =new byte[]{ 0xE2,0,0,0};
                endbytes = new byte[] { 0, 0, 0, 0 };
            }

            IUnsubstitutedBytes b = new DynamicUnsubstitutedBytes(strType);
            b.Combine(new UnsubstitutedBytes(bytes));
            b.Combine(new UnsubstitutedBytes(endbytes));
            return b;

        }

        public int getID()
        {
            return id;
        }

        public IUnsubstitutedBytes to32bits()
        {
            return new UnsubstitutedBytes(new byte[] { 0, 0, 0, 0 },
                new Substitution[]
                {
                    new Substitution(0,substitutionType.WriterRef,substitutionRank.Normal,id)
                }
            );
        }
    }
}
