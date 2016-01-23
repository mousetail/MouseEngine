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
    }
    enum MemoryType : byte
    {
        ROM,
        RAM
    }

    struct Substitution
    {
        public int position;
        public int? data;
        public substitutionRank rank;
        public substitutionType type;
        public Substitution(int position, substitutionType t, substitutionRank r, int? data)
        {
            type = t;
            rank = r;
            this.data = data;
            this.position = position;
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
    }

    

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
        public bool intersects(WriterComponent comp)
        {
            return (comp.memoryPosition >= memoryPosition && comp.memoryPosition<=memoryPosition+getSize()) ||
                    (memoryPosition >= comp.memoryPosition && comp.memoryPosition + comp.getSize() >= memoryPosition);
        }

        public int GetPosition()
        {
            return this.memoryPosition;
        }

        public virtual int getID()
        {
            return -1;
        }
    }

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
            tmp.Add((byte)func.numargs);
            tmp.Add(0x00);
            tmp.Add(0x00);

            int start = tmp.Count;
            IUnsubstitutedBytes b = new UnsubstitutedBytes(tmp.ToArray(), substitutions.ToArray());
            foreach (SubstitutedPhrase sf in func.getBlock())
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
            get { return func.name == "start game"; }
        }

        public override int getID()
        {
            return func.getID();
        }

       
    }

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
    }


    internal class Writer
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
                    Components.Add(new FunctionWriter((Function)f, ((Function)f).name));
                    if (((Function)f).name=="start game")
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

        public void Substitute(IUnsubstitutedBytes input, Substitution what)
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
            byte[] tmp = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                tmp[i] = (byte)(k / pow(256, 3 - i));
                k = k % (pow(256, 3 - i));
            }
            return tmp;
        }

        static public int toInt(byte[] k)
        {
            int tmp=0;
            for (int i=0; i<4; i++)
            {
                tmp += pow(256, (3 - i)) * k[i];
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
    }


}