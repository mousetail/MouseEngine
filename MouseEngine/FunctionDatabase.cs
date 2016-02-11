using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseEngine.Lowlevel
{
    /// <summary>
    /// A interface to link a object that will be writen to the binary, like a function or a string, to
    /// it's writer.
    /// </summary>
    interface IReferable
    {
        WriterComponent getWriter();
        void setWriter(WriterComponent w);
        int getID();
    }
    

    internal class FunctionDatabase: IEnumerable<Phrase>
    {
        List<Phrase> globalFunctions;
        public Condition[] globalConditions;

        //public static Phrase 

        public FunctionDatabase()
        {
            globalFunctions = new List<Phrase>() { Phrase.returnf,Phrase.makeWindow, Phrase.setIOSystem,
                Phrase.IOprintNum,
            Phrase.setIOWindow, Phrase.GiveError, Phrase.GlkPoll, Phrase.IOprint,
            Phrase.MathDivide, Phrase.DebugCheckStack,  Phrase.add};
            globalConditions = new[]
            {
                Condition.CondNot,
                
                Condition.CondEquals,
                Condition.CondAnd
            };
        }

        public IEnumerator<Phrase> GetEnumerator()
        {
            return ((IEnumerable<Phrase>)globalFunctions).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Phrase>)globalFunctions).GetEnumerator();
        }

        public void AddGlobalFunction(CodeBlock b, string name, int numargs)
        {
            globalFunctions.Add(new Function(b, name, numargs, Databases.ids++));
        }
    }
    internal struct Argument
    {
        public string name;
        public IValueKind type;
        public bool isStackArgument;
        

        public Argument(string name, IValueKind kind)
        {
            this.name = name;
            type = kind;
            isStackArgument = false;
        }
        
        internal static Argument fromStack(string name, IValueKind kind)
        {
            Argument b = new Argument(name, kind);
            b.isStackArgument = true;
            return b;
        }

        
    }


    internal partial class Phrase
    {
        //static Function print = new Function(new Argument[] { new Argument("text",ClassDatabase.str) },);

        

        internal Argument[] arguments;
        Matcher matcher;
        internal Opcode[] codes;
        //int stackArguments = 0;

        IValueKind returnType;

        public IValueKind getReturnType()
        {
            return returnType;
        }


        public Phrase(Argument[] args, IValueKind returnType, Matcher matcher, params Opcode[] opcodes)
        {
            arguments = args;
            this.matcher = matcher;
            codes = opcodes;
            this.returnType = returnType;
        }
        
        public Dictionary<string, string> lastMatchArgs()
        {
            return matcher.getArgs();
        }

        public bool match(string s, ClassDatabase dtb)
        {
            return matcher.match(s);
        }

        public virtual SubstitutedPhrase toSubstituedPhrase(IEnumerable<ArgumentValue> arguments, ArgumentValue? returnValue)
        {
            return new SubstitutedPhrase(this, arguments.ToList(), returnValue);
        }

        public override string ToString()
        {
            return base.ToString() + " defined by " + matcher.ToString();
        }
    }

    interface IPhraseSub: IByteable
    {

    }

    class ArbitrarySubstitutedPhrase : IByteable, IPhraseSub
    {
        Opcode[] codes;

        public ArbitrarySubstitutedPhrase(params Opcode [] codes)
        {
            this.codes = codes;
        }

        public IUnsubstitutedBytes toBytes()
        {
            IUnsubstitutedBytes tmp = new DynamicUnsubstitutedBytes();
            Queue<ArgumentValue> v = new Queue<ArgumentValue>();
            ArgumentValue? returnType = null;
            foreach (Opcode c in codes)
            {
                tmp.Combine(c.toBytes(v, ref returnType));
            }
            return tmp;
        }
    }

    internal class SubstitutedPhrase: IByteable, IPhraseSub, ICodeByteable {


        protected List<ArgumentValue> argValues;
        protected Phrase parent;
        protected ArgumentValue? returnValue;
        
        internal SubstitutedPhrase(Phrase f, List<ArgumentValue> values, ArgumentValue? returnValue)
        {
            parent = f;
            argValues = values;
            this.returnValue = returnValue;
        }

        public virtual IUnsubstitutedBytes toBytes()
        {
            //THERE IS AN APPROXIMATE COPY OF THIS CODE IN IFMATCHER.CS, all changes should be made in both files.

            Queue<ArgumentValue> argQue = new Queue<ArgumentValue>(argValues);
            
            List<byte> tmp=new List<byte>();
            List<Substitution> substitutions=new List<Substitution>();
            ArgumentValue? returnValue = this.returnValue;
            foreach (Opcode a in parent.codes)
            {
                IUnsubstitutedBytes b = a.toBytes(argQue,ref returnValue);
                foreach (Substitution sub in b.substitutions)
                {
                    Substitution nsub = sub;
                    nsub.position += tmp.Count;
                    substitutions.Add(nsub);
                }
                tmp.AddRange(b.bytes);
            }
            byte[] final = tmp.ToArray();
         
            if (returnValue!=null && returnValue.Value.getMode() != addressMode.zero)
            {
                throw new Errors.ReturnTypeViolationError("Attempt to use the return value of " + ToString() + " which has no return value");
            }

            return new UnsubstitutedBytes(final, substitutions.ToArray());
        }

        public override string ToString()
        {
            return parent.ToString();
        }
    }



    internal class CodeBlock: IEnumerable<ICodeByteable>, ICodeByteable
    {
        
        List<ICodeByteable> content=new List<ICodeByteable>();
        public void addRange(IEnumerable<ICodeByteable> num)
        {
            content.AddRange(num);
        }
        public void add(ICodeByteable num)
        {
            content.Add(num);
        }
        public void add(Phrase p, IEnumerable<ArgumentValue> args, ArgumentValue returnValue)
        {
            content.Add(p.toSubstituedPhrase(args, returnValue));
        }
        public override string ToString()
        {
            return base.ToString()+" of lenght "+content.Count.ToString();
        }
        /*public void add(Phrase p)
        {
            content.Add(p.toSubstituedPhrase());
        }*/

        public IEnumerator<ICodeByteable> GetEnumerator()
        {
            return ((IEnumerable<ICodeByteable>)content).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ICodeByteable>)content).GetEnumerator();
        }

        public virtual IUnsubstitutedBytes toBytes()
        {
            IUnsubstitutedBytes tmp = new DynamicUnsubstitutedBytes();
            List<int> phrasePositions = new List<int>();
            phrasePositions.Add(0);
            foreach (ICodeByteable s in this)
            {
                tmp.Combine(s.toBytes());
                phrasePositions.Add(tmp.Count);
            }
            if (this is ifElseCodeBlock)
            {
                ((ifElseCodeBlock)this).phrasePositions = phrasePositions.ToArray();
            }

            return tmp;
        }

        /*public int getLength()
        {
            int length = 0;
            for (IPhraseSub phr in content)
            {

            }
        }*/

        public int Count
        {
            get
            {
                return content.Count;
            }
        }
    }
    

    class ifElseCodeBlock: CodeBlock
    {
        public int[] phrasePositions;

        List<Range> conditionBLocks=new List<Range>();

        public override IUnsubstitutedBytes toBytes()
        {
            IUnsubstitutedBytes tmp= base.toBytes();

            List<Substitution> toRemove = new List<Substitution>();



            foreach (Substitution t in tmp.substitutions)
            {
                bool worked = true;
                if (t.type==substitutionType.NextElse) {
                    worked = false;
                    foreach (Range r in conditionBLocks)
                    {
                        if (phrasePositions[r.start] > t.position)
                        {
                            worked = true;
                            tmp.WriteSlice(t.position, Writer.toBytes(phrasePositions[r.start] - t.position - 2));
                        }
                    }

                    if (worked)
                    {
                        toRemove.Add(t);
                    }
                }
                else if (t.type == substitutionType.BlockStart)
                {
                    tmp.WriteSlice(t.position, Writer.toBytes(-t.position - 2));
                }

                if (t.type == substitutionType.EndIf || !worked)
                {
                    tmp.WriteSlice(t.position, Writer.toBytes(tmp.Count - t.position-2));
                    toRemove.Add(t);
                }
            }

            foreach (Substitution t in toRemove)
            {
                tmp.Complete(t);
            }



            return tmp;
        }

        public void addIfRange(Range r)
        {
            conditionBLocks.Add(r);
            conditionBLocks.Sort();
        }

        public void addIfRange(int start, int end)
        {
            addIfRange(new Range(start, end));
        }
    }

    class Function: Phrase, IReferable
    {
        public string name;
        public int numargs;
        int id;

        public Function(CodeBlock code, string name, int numargs, int id):base(new Argument[] { }, null, new StringMatcher(name), new Opcode(opcodeType.call,new IArgItem[] { new ArgumentValue(addressMode.addrint,substitutionType.WriterRef,id,ClassDatabase.integer), new ArgumentValue(addressMode.constint,0),new ArgItemReturnValue() } ))
        {
            inside = code;
            this.name = name;
            this.numargs = numargs;
            this.id = id;
        }
        CodeBlock inside;
        public CodeBlock getBlock()
        {
            return inside;
        }

        public override string ToString()
        {
            return "\""+name+"\"";
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

        public int getID()
        {
            return id;
        }
    }

    interface IOpcode
    {
        IUnsubstitutedBytes toBytes(Queue<ArgumentValue> input, ref ArgumentValue? returnValue);
        
    }



    class Opcode: IOpcode, ICodeByteable
    {

        static byte[] makeaddrict(ArgumentValue[] modes)
        {
            byte[] tmp = new byte[(modes.Length + 1) / 2];

            for (int i=0; i<modes.Length; i++)
            {
                if ((i % 2) == 0)
                {
                    tmp[i / 2] += (byte)modes[i].getMode();
                }
                else
                {
                    tmp[i / 2] += (byte)(16 * (byte)modes[i].getMode());
                }
            }
            return tmp;
        }

        IArgItem[] existingValues;
        opcodeType type;
        public Opcode (opcodeType type, params IArgItem[] existingValues)
        {
            this.type = type;
            this.existingValues = existingValues;
            if (existingValues == null)
            {
                throw new NullReferenceException("existing values can't be null");
            }
        }
        /// <summary>
        /// A toBytes implementation for opcodes that take no arguments, will crash for opcodes that require arguments. Can be
        /// used in contexts where passing arguments is not realistic.
        /// </summary>
        /// <returns></returns>
        public IUnsubstitutedBytes toBytes()
        {
            ArgumentValue? f=null;
            return toBytes(new Queue<ArgumentValue>(), ref f);
        }
        /// <summary>
        /// The normal inplementation for toBytes, any required arguments are taken from the que, and the return value,
        /// if any, is writen to return. The method is designed to be applied repeatedly to a list of opcodes, without having to
        /// change the variables. The function automatically crashes if you try to assign to the return value is writen to a
        /// second time, for example.
        /// </summary>
        /// <param name="values">The arguments que from which to take any required arguments</param>
        /// <param name="returnValue">The argument value that return value will be writen to.</param>
        /// <returns></returns>
        public IUnsubstitutedBytes toBytes(Queue<ArgumentValue> values, ref ArgumentValue? returnValue)
        {
            List<byte> bytes= new List<byte>();
            List<Substitution> subs=new List<Substitution>();
            bytes.AddRange(type.toBytes());
            List<ArgumentValue> args = new List<ArgumentValue>();
            ArgumentValue current;

            for (int i=0; i<existingValues.Length; i++)
            {
                if (existingValues[i] is ArgumentValue)
                {
                    current = (ArgumentValue)existingValues[i];
                }
                else if (existingValues[i] is ArgItemReturnValue)
                {
                    if (returnValue != null)
                    {
                        current = (ArgumentValue)returnValue;
                        returnValue = null;
                    }
                    else
                    {
                        throw new Errors.OpcodeFormatError(
                            "You either didn't give a return value to a object that required it, or 2 opcodes requested one.");
                    }
                }
                else
                {

                    current = values.Dequeue();

                }
                args.Add(current);
            }
            bytes.AddRange(makeaddrict(args.ToArray()));
            for (int i=0; i<args.Count; i++)
            {
                Substitution? sub=args[i].getSubstitution(bytes.Count);
                if (sub != null)
                {
                    subs.Add((Substitution)sub);
                }
                bytes.AddRange(args[i].getData());
            }

            return new UnsubstitutedBytes(bytes.ToArray(), subs.ToArray());


        }

        int[] jumpValues;

        public void setJumpTo(ArgumentValue newValue)
        {
            if (jumpValues == null)
            {
                List<int> jumpValues = new List<int>();
                for (int i=0; i<existingValues.Length; i++)
                {
                    if (existingValues[i] is ArgItemJumpTo)
                    {
                        jumpValues.Add(i);
                    }
                }
                this.jumpValues = jumpValues.ToArray();
            }
            foreach (int i in jumpValues)
            {
                existingValues[i] = newValue;
            }
        }
    }
}
