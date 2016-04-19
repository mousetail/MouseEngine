using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace MouseEngine.Lowlevel
{
    interface IByteable
    {
        IUnsubstitutedBytes toBytes();
    }

    enum substitutionRank : byte { FunctionOrder, First, Normal, Later, Last, Reserved }
    enum substitutionType
    {
        None,
        MemAdress,
        Length,
        MagicNumber,
        Version,
        Ramstart,
        FileSize,
        StackSize,
        StartFunction,
        DecodingTable,
        Checksum,
        maxMemory,
        WriterRef,
        NextElse,
        EndIf,
        BlockStart,
        endCondition,
        conditionDestination
    }
    enum MemoryType : byte
    {
        ROM,
        RAM
    }
    /// <summary>
    /// A substitution is my way of dealing with byte series of which the value can not be determined until after the
    /// smace is reserved, things like references. Various substitution are handled in diffrent places. A reference is
    /// handled last, by the writer. A endif substitution is handled when the end of the if is decided.
    /// </summary>
    struct Substitution
    {
        public int position;
        public int? data;
        public substitutionRank rank;
        public substitutionType type;
        public bool completed;
        public Substitution(int position, substitutionType t, substitutionRank r, int? data)
        {
            type = t;
            rank = r;
            this.data = data;
            this.position = position;
            completed = false;
        }
        public Substitution(int position, substitutionType t, substitutionRank r) : this(position, t, r, null)
        {

        }
        public Substitution(int position, substitutionType t): this(position, t, substitutionRank.Normal, null)
        {

        }
        public Substitution moveTo(int position)
        {
            Substitution b = this;
            b.position = position;
            return b;
        }

        public override int GetHashCode()
        {
            return position + (int)type<<2 + (int)rank<<4 + GetType().GetHashCode()<<8;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Substitution))
            {
                return false;
            }
            else
            {
                Substitution s = (Substitution)obj;
                return (s.position == position && s.type == type && s.rank == rank);
            }
        }
    }

    
    /// <summary>
    /// A writer component is anything that can be written at any place in the file. Some sublasses write
    /// a function, others strings, others write whatever. 
    /// </summary>
    abstract class WriterComponent
    {
        int identifyer;
        int memoryPosition;

        public abstract IUnsubstitutedBytes tobytes();
        public virtual MemoryType getMemoryType()
        {
            return MemoryType.RAM;
        }
        public abstract int getSize();

        public void place(int position)
        {
            memoryPosition = position;
        }
        

        public int GetPosition()
        {
            return this.memoryPosition;
        }

        public virtual int getID()
        {
            return -1;
        }

        internal Range getRange()
        {
            return new Range(memoryPosition, memoryPosition + getSize());
        }

        public bool intersects(WriterComponent other)
        {
            return getRange().intersects(other.getRange());
        }
    }
    /// <summary>
    /// Writes the header. The header of a glulx file contains some metadata,
    /// this function returns them in substitutions, since it has no way of
    /// knowing the actual information, which isn't even available yet.
    /// </summary>
    class Header : WriterComponent
    {
        public override IUnsubstitutedBytes tobytes()
        {
            byte[] f = new byte[36];
            return new UnsubstitutedBytes(f, new Substitution[] {
                new Substitution(0,substitutionType.MagicNumber),
                new Substitution(4,substitutionType.Version),
                new Substitution(8,substitutionType.Ramstart),
                new Substitution(12,substitutionType.FileSize),
                new Substitution(16,substitutionType.maxMemory),
                new Substitution(20,substitutionType.StackSize),
                new Substitution(24,substitutionType.StartFunction),
                new Substitution(28,substitutionType.DecodingTable),
                new Substitution(32,substitutionType.Checksum)

                });
        }
        public override MemoryType getMemoryType()
        {
            return MemoryType.ROM;
        }
        public override int getSize()
        {
            return 36;
        }
    }
    /// <summary>
    /// Writes -1 over 4 bytes to the file. Used to make sure at least 4 bytes exist in RAM.
    /// </summary>
    class RandomWord : WriterComponent
    {
        public override int getSize()
        {
            return 4;
        }

        public override IUnsubstitutedBytes tobytes()
        {
            return new UnsubstitutedBytes( new byte[] { 0xff, 0xff, 0xff, 0xff }, new Substitution[0]);
        }
    }
    /// <summary>
    /// Writes a function to a file,
    /// mostly works by calling toBytes on all the phrases
    /// in the function. it also writes type codes, argument types
    /// etc.
    /// </summary>
    class FunctionWriter: WriterComponent
    {
        Function func;
        int size;
        int id;

        public FunctionWriter(Function f, string name)
        {
            func = f;
            tobytes();
            id = f.getID();
        }
        public override MemoryType getMemoryType()
        {
            return MemoryType.ROM;
        }
        public override IUnsubstitutedBytes tobytes()
        {
            List<byte> tmp = new List<byte>(64);
            List<Substitution> substitutions = new List<Substitution>();
            tmp.Add(0xC1);
            tmp.Add(0x04);
            tmp.Add((byte)func.getLocalsLength()); //probably should do something to prevent overflow.
            tmp.Add(0x00);
            tmp.Add(0x00);

            int start = tmp.Count;
            IUnsubstitutedBytes b = new UnsubstitutedBytes(tmp.ToArray(), substitutions.ToArray());
            foreach (ICodeByteable sf in func.getBlock())
            {
                b.Combine(sf.toBytes());
            }
            size = b.Count;
            return b;
        }
        public override int getSize()
        {
            return size;
        }

        bool isStart
        {
            get { return func.match("start game"); }
        }

        public override int getID()
        {
            return func.getID();
        }

        public override string ToString()
        {
            return "Function: " + func.ToString().shorten(60);
        }


    }
    /// <summary>
    /// A class that turns a string into a writeable object, incuding
    /// deciding on encoding, terminating characters, etc.
    /// </summary>
    class StringWriter: WriterComponent
    {
        StringItem parent;
        int size;

        public StringWriter (StringItem parent)
        {
            size = parent.toBytes().Count;
            parent.setWriter(this);
            this.parent = parent;
        }

        public override IUnsubstitutedBytes tobytes()
        {
            IUnsubstitutedBytes b = parent.toBytes();
            size = b.Count;
            return b;
        }

        public override int getSize()
        {
            return size;
        }

        public override int getID()
        {
            return parent.getID();
        }

        public override MemoryType getMemoryType()
        {
            return MemoryType.ROM;


        }

        public override string ToString()
        {
            return "StringWriter for \"" + parent.ToString().shorten(30) + "\"";
        }
    }


    public class Writer
    {
        List<WriterComponent> Components;
        List<WriterComponent> ExistingComponents;
        ClassDatabase cdtb;
        FunctionDatabase fdtb;
        int startFunctionDefinition;
        FunctionWriter startfunction;
        StringDatabase sdtb;

        public Writer(Databases dtbs)
        {
            cdtb = dtbs.cdtb;
            fdtb = cdtb.functionDatabase;
            sdtb = dtbs.sdtb;
        }

        bool prepared;
        /// <summary>
        /// Assembles and orderes all components.
        /// </summary>
        public void prepare()
        {
            Components = new List<WriterComponent>() { new RandomWord() };
            ExistingComponents = new List<WriterComponent>();
            Header h = new Header();
            ExistingComponents.Add(h);
            foreach (Phrase f in fdtb)
            {
                if (f is Function)
                {
                    Components.Add(new FunctionWriter((Function)f, ((Function)f).ToString()));
                    if (((Function)f).match("start game"))
                    {
                        startfunction = (FunctionWriter)Components[Components.Count-1];
                    }
                }
            }
            foreach (StringItem f in sdtb)
            {
                Components.Add(new StringWriter(f));
            }

            h.place(0);
            prepared = true;
        }
        /// <summary>
        /// Formats all the data to a byte array.
        /// Prepare should be called first.
        /// </summary>
        /// <returns>The array of all the bytes to be written</returns>
        public byte[] write()
        {
            Debug.Assert(prepared);
            Comparison<WriterComponent> b = (x, y) => x.GetPosition() - y.GetPosition();
            Components.Sort((x, y) => (int)x.getMemoryType() - (int)y.getMemoryType());

            ExistingComponents.Sort(b);

            int? dangerousComponent = null;
            if (ExistingComponents.Count > 0)
            {
                dangerousComponent = 0;
            }

            bool touchedDangerous = false;

            ramstart = 0;

            int position = 0;
            foreach (WriterComponent p in Components)
            {
                if (p.getMemoryType() == MemoryType.RAM && ramstart == 0)
                {
                    ramstart = position.RoundUp(256);
                    position = ramstart;
                }
                p.place(position);
                while (dangerousComponent != null && p.intersects(ExistingComponents[(int)dangerousComponent]))
                {
                    position += 1;
                    touchedDangerous = true;
                    p.place(position);
                }
                if (dangerousComponent == null)
                {
                    dangerousComponent = 0;
                }
                if (touchedDangerous)
                {
                    dangerousComponent += 1;
                }
                if (p.Equals( startfunction))
                {
                    startFunctionDefinition = position;
                }

                ExistingComponents.Add(p);
                ExistingComponents.Sort(b);
            }

            int TotalLength = ExistingComponents[ExistingComponents.Count - 1].GetPosition() + ExistingComponents[ExistingComponents.Count - 1].getSize();
            
            DynamicUnsubstitutedBytes data = new DynamicUnsubstitutedBytes(new byte[TotalLength]);

            List<Substitution> Subs = new List<Substitution>();

            Console.WriteLine("MEMORY MAP:");
            foreach (WriterComponent p in ExistingComponents)
            {
                Console.WriteLine(p.GetPosition().ToString() + ":\t" + p.ToString());
            }
            Console.WriteLine("END MEMORY MAP");

            foreach (WriterComponent w in ExistingComponents)
            {
                IUnsubstitutedBytes unsbit = w.tobytes();
                data.Combine(unsbit,w.GetPosition());
            }

            while (data.Count % 256 != 0)
            {
                data.Combine(new UnsubstitutedBytes(new byte[1]));
            }

            Subs = data.substitutions.ToList();

            Subs.Sort((x, y) => (int)x.rank - (int)y.rank);

            foreach (Substitution sub in Subs)
            {
                Substitute(data, sub);
            }

            return data.bytes.ToArray();



        }

        int ramstart = 0;

        /// <summary>
        /// Puts in a single substitution. This function takes the highest and last types of substitutions, and puts them where they belong.
        /// </summary>
        /// <param name="input">The bytest to which to sustitute</param>
        /// <param name="what">The substitution to put in</param>
        void Substitute(IUnsubstitutedBytes input, Substitution what)
        {
            switch (what.type)
            {
                case (substitutionType.FileSize):
                    input.WriteSlice(what.position, toBytes(input.Count));
                    break;
                case (substitutionType.Ramstart):
                    input.WriteSlice(what.position, toBytes(ramstart));
                    break;
                case (substitutionType.MagicNumber):
                    input.WriteSlice(what.position, new byte[] { 0x47, 0x6C, 0x75, 0x6C }); //Glul
                    break;
                case (substitutionType.DecodingTable):
                    input.WriteSlice(what.position, new byte[4]);
                    break;
                case (substitutionType.Version):
                    input.WriteSlice(what.position, new byte[] { 0x00, 0x03, 0x01, 0xFF });
                    break;
                case (substitutionType.maxMemory):
                    input.WriteSlice(what.position, toBytes(input.Count.RoundUp(256)));
                    break;
                case (substitutionType.StartFunction):
                    input.WriteSlice(what.position, toBytes(startFunctionDefinition));
                    break;
                case substitutionType.StackSize:
                    input.WriteSlice(what.position, toBytes(1024));
                    break;
                case substitutionType.WriterRef:
                    WriterComponent wrtcmp=null;
                    foreach (WriterComponent p in ExistingComponents)
                    {
                        if (p.getID() == what.data)
                        {
                            wrtcmp = p;
                            break;
                        }
                    }
                    if (wrtcmp != null)
                    {
                        input.WriteSlice(what.position, toBytes(wrtcmp.GetPosition()));
                    }
                    else
                    {
                        throw new Errors.IDMismatchException("ID " + what.data.ToString() + " refered to, but not set to any object");
                    }

                    break;
            }
        }
        static public byte[] toBytes(int k)
        {
            return toBytes(k, true, true);
        }
        static public byte[] toBytes(int k, bool signed, bool force32)
        {
            unchecked
            {
                if (force32 || !signed)
                {
                    return toBytesN((uint)k, 4);
                }

                if (signed)
                {
                    if (k < 0x7F && k > -0x80)
                    {
                        return toBytesN((uint)k, 1);
                    }
                    else if (k < 0x7FFF && k > -0x8000)
                    {
                        return toBytesN((uint)k, 2);
                    }
                    else if (k < 0x7FFFFFFF && k > -0x80000000)
                    {
                        return toBytesN((uint)k, 4);
                    }
                    else
                    {
                        throw new Errors.NumberOutOfRangeException("tried to convert " + k.ToString() + "to a signed integer");
                    }
                }
                else
                {
                    if (k < 0x100)
                    {
                        return toBytesN((uint)k, 1);
                    }
                    else if (k < 0x10000)
                    {
                        return toBytesN((uint)k, 2);
                    }
                    else
                    {
                        return toBytesN((uint)k, 4);
                    }
                }
            }
        }

        static byte[] toBytesN(uint k, int n)
        {
            unchecked
            {
                if (n != 4) {
                    k = (uint)(k % (pow(256, n)));
                }
                byte[] tmp = new byte[n];
                for (int i = 0; i < n; i++)
                {
                    tmp[i] = (byte)(k / pow(256, n - 1 - i));
                    k = (uint)(k % (pow(256, n - 1 - i)));
                }
                return tmp;
            }
        }

        static public int toInt(byte[] k)
        {
            int tmp=0;
            for (int i=0; i<k.Length; i++)
            {
                tmp += pow(256, (k.Length-1 - i)) * k[i];
            }
            return tmp;
        }


        static public int pow(int num, int pow)
        {
            int f = 1;
            for (int i=0; i< pow; i++)
            {
                f *= num;
            }
            return f;
        }

        public static byte[] toBytes(int value, int size)
        {
            return toBytesN((uint)value, size);
            
        }
    }


}