using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using MouseEngine;

namespace MouseEngine.Lowlevel
{
    /// <summary>
    /// A interface to link a object that will be writen to the binary, like a function or a string, to
    /// it's writer.
    /// </summary>
    interface IReferable: I32Convertable
    {
        WriterComponent getWriter();
        void setWriter(WriterComponent w);
        int getID();
    }
    
    /// <summary>
    /// The function database basically cointains a list of all the functions and phrases in the program.
    /// In debug mode, this function makes sure only one instance exist. These 2 lines can just be taken out
    /// if you have a good reason.
    /// </summary>
    internal class FunctionDatabase: IEnumerable<Phrase>
    {
        List<Phrase> globalFunctions;
        public Condition[] globalConditions;
#if DEBUG
        static int instances = 0;
#endif
        //public static Phrase 

        public FunctionDatabase()
        {
#if DEBUG
            if (instances > 0)
            {
                throw new Errors.OpcodeFormatError("trying to make a new instance of functiondatabase. If done on purpose, change FDB");
            }
            instances += 1;
#endif
            globalFunctions = new List<Phrase>() { Phrase.returnf,  Phrase.add,Phrase.makeWindow, Phrase.setIOSystem,
                Phrase.IOprintNum,
            Phrase.setIOWindow, Phrase.GiveError, Phrase.GlkPoll, Phrase.IOprint,
            Phrase.MathDivide, Phrase.DebugCheckStack, Phrase.MathSubtract};
            globalConditions = new[]
            {
                Condition.CondNot,
                Condition.CondAtLeast,

                Condition.CondEquals,
                Condition.CondAnd,
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

        public Function AddGlobalFunction(Prototype parent, Matcher m, IEnumerable<Argument> args, IValueKind returnValue)
        {
            Function tmp = new Function(parent, null, m, returnValue, args, Databases.ids++);
            globalFunctions.Add(tmp);
#if DEBUG
            Console.WriteLine(tmp);
#endif
            return tmp;
        }
    }

    /// <summary>
    /// A argument is a borning struct that represents everything you can know about an argument to a function or
    /// phrase, which is, the name and the kind.
    /// </summary>
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

        public override string ToString()
        {
            return name + "<" + type.ToString() + ">";
        }


    }

    /// <summary>
    /// A phrase, the most important pard of the code parsing process. A phrase represents a single
    /// typable command. All phrases in the function databases are tested for a match. A phrase can
    /// be turned into a substituted phrase, which is a phrase with arguments frozen, which in turn
    /// can be turned into bytes for the file. 
    /// </summary>
    internal partial class Phrase
    {

        

        internal Argument[] arguments;
        Matcher matcher;
        internal Opcode[] codes;
    
        //int stackArguments = 0;

        IValueKind returnType;

        public IValueKind getReturnType()
        {
            return returnType;
        }

        /// <summary>
        /// The phrase construction is a bit complicated. It's better to look at some examples, probably.
        /// </summary>
        /// <param name="args">a array of arguments the function takes</param>
        /// <param name="returnType">the type of value the function return, can be classdatabase.nothing</param>
        /// <param name="matcher">What matcher is used to test wheter the phrase applies? Ususally a multi string
        /// matcher, with argnames matching the names of the arguments.</param>
        /// <param name="opcodes">The opcode objects that make up the body of the phrase.</param>
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

        public bool match(string s)
        {
            return matcher.match(s);
        }

        public virtual SubstitutedPhrase toSubstituedPhrase(IEnumerable<ArgumentValue> arguments, ArgumentValue? returnValue)
        {
            return new SubstitutedPhrase(this, arguments.ToList(), returnValue);
        }

        public override string ToString()
        {
            return GetType().Name + ": " + matcher.ToString();
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
    /// <summary>
    /// A phrase with argument values and other stuff built in, a phrase with all the information needed to be turned into
    /// bytes!
    /// </summary>
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


    /// <summary>
    /// A code block is just a collection of substituted phrases, and some other code objects like conditions.
    /// </summary>
    internal class CodeBlock: IEnumerable<ICodeByteable>, ICodeByteable
    {
        
        List<ICodeByteable> content=new List<ICodeByteable>();
        internal Dictionary<string, LocalVariable> locals;

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
    
    /// <summary>
    /// I am really unsure whether this should be a class. This is a code block that keeps track of where the
    /// else's are, and substitutes them afterwards.
    /// </summary>
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

    /// <summary>
    /// A function is a phrase that is written by the user. A function is a phrase, but all the phrase does is use
    /// the call opcode with a substitution to find where the actual function is stored. 
    /// </summary>
    class Function: Phrase, IReferable
    {
        Prototype parent;

        int id;

        public Function(Prototype parent, CodeBlock code, Matcher matcher, IValueKind returnValue, IEnumerable<Argument> arguments, int id)
            :base(arguments.Select((x=>Argument.fromStack(x.name, x.type))).ToArray(), returnValue, matcher, new Opcode(opcodeType.call,new IArgItem[] { new ArgumentValue(addressMode.constint,substitutionType.WriterRef,id,ClassDatabase.integer), new ArgumentValue(addressMode.constint,arguments.Count()),new ArgItemReturnValue() } ))
        {
            inside = code;
            this.id = id;
            this.parent = parent;
        }
        CodeBlock inside;
        public CodeBlock getBlock()
        {
            return inside;
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

        internal void setBlock(CodeBlock codeBlock)
        {
            inside = codeBlock;
        }

        internal int getLocalsLength()
        {
            return inside.locals.Count;
        }

        public IUnsubstitutedBytes to32bits()
        {
            return new UnsubstitutedBytes(new byte[] { 0, 0, 0, 0 }, new Substitution[]
            {
                new Substitution(0, substitutionType.WriterRef, substitutionRank.Normal, getWriter().getID())
            });
        }
    }

    interface IOpcode
    {
        IUnsubstitutedBytes toBytes(Queue<ArgumentValue> input, ref ArgumentValue? returnValue);
        
    }


    /// <summary>
    /// A opcode is the second most important part of code parsing. A opcode represents a opcode and all relevant arguments
    /// in glulx. 
    /// </summary>
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
