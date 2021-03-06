﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        conditionDestination,
        localID,
        numArguments,
        attrID
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
            return new Range(memoryPosition, memoryPosition + getSize() - 1);
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
            //The f above will fail if a function uses over 256 (approx) locals 
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
            return "Function: " + func.ToString().shorten(80);
        }


    }


    /// <summary>
    /// Writes a object
    /// </summary>
    class ObjectWriter : WriterComponent
    {
        //A object data block consists of first a 0x7b
        //byte. The next 3 bytes are not important, they might be used later on by the garbage collector, but for now
        //they are never checked at this point.
        //Next follow all the attributes of the object, in the order of obj.getpossibleattributes()
        //so attributes from parent classes come before attributes of child classes.
        //The first attribibute is the base/type
        Prototype obj;
        int size;

        public ObjectWriter(Prototype obj)
        {
            this.obj = obj;
            obj.setWriter(this);
        }

        public override int getID()
        {
            return obj.getID();
        }

        public override int getSize()
        {
            if (size==0) {
                size=obj.getPossibleAttributes().Count * 4 + 8;
            }

            return size;
        }

        public override IUnsubstitutedBytes tobytes()
        {
            DynamicUnsubstitutedBytes bit = new DynamicUnsubstitutedBytes();

            bit.WriteSlice(0, new byte[] { 0x7B, 0xFF, 0xEE, 0xBB,
                                              0,    0,    0,    0});

            bit.addSubstitution(new Substitution(4, substitutionType.WriterRef, substitutionRank.Normal,
                obj.getParent().getID()));


            foreach (var b in obj.getPossibleAttributes().Values)
            {
                I32Convertable value = obj.getSpecificAttribute(b.name);
                bit.Combine(value.to32bits(), (int)b.pos*4);
            }

            return bit;
        }

        public override MemoryType getMemoryType()
        {
            return MemoryType.RAM;
        }
    }

    class TypeWriter: WriterComponent
    {
        KindPrototype parent;
        KindPrototype kind;
        Databases dtbs;
        int stringID;

        int size;

        public override int getID()
        {
            return parent.getID();
        }

        public TypeWriter(KindPrototype parent, KindPrototype kind, Databases dtbs)
        {
            this.parent = parent;
            this.kind = kind;
            this.dtbs = dtbs;
            var name = dtbs.sdtb.getStr(parent.getName());
            stringID = name.getID();
        }

        public override int getSize()
        {
            if (size == 0)
            {
                size= 16 + 4 * parent.getFunctions().Count;
            }
            return size;
        }

        public override IUnsubstitutedBytes tobytes()
        {
            //A type needs to store the following attributes:
            //PARENT
            //A ref to the type "type"
            //name (ref to string)
            //It's usefull to have a ref to type first, so it can be implemented
            //like a class in other instances.
            //(that's really the only reason I have it)

            DynamicUnsubstitutedBytes bytes = new DynamicUnsubstitutedBytes();
            bytes.WriteSlice(0, new byte[] { 0x7A, 0, 0, 0,
                                             0,    0, 0, 0,
                                             0,    0, 0, 0,
                                             0,    0, 0, 0});
            bytes.addSubstitution(new Substitution(4, substitutionType.WriterRef, substitutionRank.Normal, kind.getID()));
            if (parent.getParent() != null)
            {
                bytes.addSubstitution(new Substitution(8, substitutionType.WriterRef, substitutionRank.Normal, parent.getParent().getID()));
            }
           
            bytes.addSubstitution(new Substitution(12, substitutionType.WriterRef, substitutionRank.Normal, stringID));

            List<LocalFunction> localFuncs = parent.getFunctions();

            foreach (var b in localFuncs)
            {
                //Assure there is actually free space where the subsitution goes
                bytes.WriteSlice(b.localID * 4, new byte[4]);
                bytes.addSubstitution(new Substitution(b.localID * 4, substitutionType.WriterRef, substitutionRank.Normal, b.getID()));
            }

            size = 0;

            return bytes;
        }

        public override string ToString()
        {
            return "typeWriter for "+parent.getName();
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
        Databases dtbs;

        public Writer(Databases dtbs)
        {
            cdtb = dtbs.cdtb;
            fdtb = cdtb.functionDatabase;
            sdtb = dtbs.sdtb;
            this.dtbs = dtbs;
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
            

            KindPrototype kind=null;

            foreach (KindPrototype b in cdtb.existingTypes.Values)
            {
                if (b.getName().Equals( "kind", StringComparison.CurrentCultureIgnoreCase))
                {
                    kind = b;
                }
                else if (kind == null)
                {
                    throw new Errors.OpcodeFormatError("The type 'kind' should be the first kind in the list, was "+b.getName());
                }
                Components.Add(new TypeWriter(b, kind, dtbs));
            }

            foreach (Phrase f in fdtb)
            {
                if (f is Function)
                {
                    Components.Add(new FunctionWriter((Function)f, ((Function)f).ToString()));
                    if (((Function)f).match("start game"))
                    {
                        startfunction = (FunctionWriter)Components[Components.Count - 1];
                    }
                }
            }

            foreach (ItemPrototype b in cdtb.existingObjects.Values)
            {
                Components.Add(new ObjectWriter(b));
            }
            /*
            foreach (KindPrototype b in cdtb.existingTypes.Values)
            {
                Components.Add(new ObjectWriter(b));
            }*/
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
#if DEBUG
            Debug.Assert(prepared);
#endif
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
                case substitutionType.attrID:
                    bool sucess=false;
                    foreach (KeyValuePair<string, KindPrototype> p in cdtb.existingTypes)
                    {
                        KAttribute attribute = p.Value.getAttributeByID((int)what.data);
                        if (attribute != null)
                        {
                            input.WriteSlice(what.position, toBytes((int)attribute.pos));
                            Console.WriteLine("sucessuflly wrote attrID at " + what.position.ToString()+
                                " attr="+attribute.pos.ToString())
                                ;
                            sucess = true;
                        }
                    }
                    if (!sucess)
                    {
                        throw new Errors.IDMismatchException("No attribute with id " + what.data.ToString());
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