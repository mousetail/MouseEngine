using System;
using System.Collections.Generic;

namespace MouseEngine.Lowlevel
{
    interface IByteable
    {
        byte[] toBytes();
    }

    enum substitutionRank : byte { FunctionOrder, First, Normal, Later, Last, Reserved }
    enum substitutionType
    {
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
        argumentN,
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
    }

    class UnsubstitutedBytes {

        public byte[] bytes;
        public Substitution[] substitutions;

        public UnsubstitutedBytes(byte[] bytes, Substitution[] substitutions)
        {
            this.bytes = bytes;
            this.substitutions = substitutions;
        }
    }

    abstract class WriterComponent
    {
        int identifyer;
        int memoryPosition;

        public abstract UnsubstitutedBytes tobytes();
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
    }

    class Header : WriterComponent
    {
        public override UnsubstitutedBytes tobytes()
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

        public override UnsubstitutedBytes tobytes()
        {
            return new UnsubstitutedBytes( new byte[] { 0xff, 0xff, 0xff, 0xff }, new Substitution[0]);
        }
    }

    class FunctionWriter: WriterComponent
    {
        Function func;
        int size;

        public FunctionWriter(Function f, string name)
        {
            func = f;
            tobytes();
        }
        public override MemoryType getMemoryType()
        {
            return MemoryType.ROM;
        }
        public override UnsubstitutedBytes tobytes()
        {
            List<byte> tmp = new List<byte>(64);
            List<Substitution> substitutions = new List<Substitution>();
            tmp.Add(0xC1);
            tmp.Add(0x04);
            tmp.Add((byte)func.arguments.Length);
            tmp.Add(0x00);
            tmp.Add(0x00);

            foreach (SubstitutedPhrase f in func.getBlock())
            {
                tmp.AddRange(f.toBytes());
            }
            size = tmp.Count;
            return new UnsubstitutedBytes(tmp.ToArray(),substitutions.ToArray());
        }
        public override int getSize()
        {
            return size;
        }

        bool isStart
        {
            get { return func.name == "start game"; }
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

        public Writer(ClassDatabase cdtb)
        {
            this.cdtb = cdtb;
            fdtb = cdtb.functionDatabase;
        }

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
                        Console.WriteLine(f);
                    }
                }
            }
            h.place(0);
        }

        public byte[] write()
        {
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
                    Console.WriteLine(p);
                }

                ExistingComponents.Add(p);
                ExistingComponents.Sort(b);
            }

            int TotalLength = ExistingComponents[ExistingComponents.Count - 1].GetPosition() + ExistingComponents[ExistingComponents.Count - 1].getSize();

            byte[] data = new byte[TotalLength];

            List<Substitution> Subs = new List<Substitution>();

            foreach (WriterComponent w in ExistingComponents)
            {
                UnsubstitutedBytes unsbit = w.tobytes();
                ArrayUtil.WriteSlice(data, w.GetPosition(), w.getSize(), unsbit.bytes);
                foreach (Substitution sub in unsbit.substitutions)
                {
                    Substitution f = sub;
                    f.position += w.GetPosition();
                    Subs.Add(f);
                }
            }

            Subs.Sort((x, y) => (int)x.rank - (int)y.rank);

            foreach (Substitution sub in Subs)
            {
                Substitute(data, sub);
            }

            return data;



        }

        int ramstart = 0;

        public void Substitute(byte[] data, Substitution what)
        {
            switch (what.type)
            {
                case (substitutionType.FileSize):
                    data.WriteSlice(what.position, 4, toBytes(data.Length));
                    break;
                case (substitutionType.Ramstart):
                    data.WriteSlice(what.position, 4, toBytes(ramstart));
                    break;
                case (substitutionType.MagicNumber):
                    data.WriteSlice(what.position, 4, new byte[] { 0x47, 0x6C, 0x75, 0x6C });
                    break;
                case (substitutionType.DecodingTable):
                    data.WriteSlice(what.position, 4, new byte[4]);
                    break;
                case (substitutionType.Version):
                    data.WriteSlice(what.position, new byte[] { 0x00, 0x03, 0x01, 0xFF });
                    break;
                case (substitutionType.maxMemory):
                    Console.WriteLine(data.Length);
                    Console.WriteLine(what.position);
                    data.WriteSlice(what.position, toBytes(data.Length));
                    break;
                case (substitutionType.StartFunction):
                    data.WriteSlice(what.position, toBytes(startFunctionDefinition));
                    break;
                case substitutionType.StackSize:
                    data.WriteSlice(what.position, toBytes(1024));
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