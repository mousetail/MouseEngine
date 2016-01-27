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

        //public static Phrase 

        public FunctionDatabase()
        {
            globalFunctions = new List<Phrase>() { Phrase.returnf,Phrase.makeWindow, Phrase.setIOSystem,
                Phrase.IOprintNum,
            Phrase.setIOWindow, Phrase.GiveError, Phrase.GlkPoll, Phrase.IOprint,
            Phrase.MathDivide, Phrase.CondBasicIf, Phrase.CondBasicWhile,  Phrase.add};
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
            return matcher.match(s, dtb);
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
                tmp.Combine(c.getBytecode(v, ref returnType));
            }
            return tmp;
        }
    }

    internal class SubstitutedPhrase: IByteable, IPhraseSub {


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
                IUnsubstitutedBytes b = a.getBytecode(argQue,ref returnValue);
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



    internal class CodeBlock: IEnumerable<IPhraseSub>
    {
        
        List<IPhraseSub> content=new List<IPhraseSub>();
        public void addRange(IEnumerable<IPhraseSub> num)
        {
            content.AddRange(num);
        }
        public void add(IPhraseSub num)
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

        public IEnumerator<IPhraseSub> GetEnumerator()
        {
            return ((IEnumerable<IPhraseSub>)content).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IPhraseSub>)content).GetEnumerator();
        }

        /*public int getLength()
        {
            int length = 0;
            for (IPhraseSub phr in content)
            {

            }
        }*/
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
        IUnsubstitutedBytes getBytecode(Queue<ArgumentValue> input, ref ArgumentValue? returnValue);
        
    }



    class Opcode: IOpcode
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

        public IUnsubstitutedBytes getBytecode(Queue<ArgumentValue> values, ref ArgumentValue? returnValue)
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
    }
}
