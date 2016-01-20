using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseEngine.Lowlevel
{
    

    internal class FunctionDatabase: IEnumerable<Phrase>
    {
        List<Phrase> globalFunctions;

        //public static Phrase 

        public FunctionDatabase()
        {
            globalFunctions = new List<Phrase>() { Phrase.returnf };
        }

        public IEnumerator<Phrase> GetEnumerator()
        {
            return ((IEnumerable<Phrase>)globalFunctions).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Phrase>)globalFunctions).GetEnumerator();
        }

        public void AddGlobalFunction(CodeBlock b, String name)
        {
            globalFunctions.Add(new Function(b, name));
        }
    }
    internal struct Argument
    {
        public string name;
        public IValueKind type;

        public Argument(string name, IValueKind kind)
        {
            this.name = name;
            type = kind;
        }
        
    }


    internal class Phrase
    {
        //static Function print = new Function(new Argument[] { new Argument("text",ClassDatabase.str) },);

        public static Phrase returnf = new Phrase(new Argument[] { new Argument("value", ClassDatabase.integer) }, null, new MultiStringMatcher(new string[1] { "value" }, "return", ""), 
            new Opcode(opcodeType.returnf, new ArgumentValue?[1] { null } ));

        internal Argument[] arguments;
        Matcher matcher;
        internal Opcode[] codes;

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

        public SubstitutedPhrase toSubstituedPhrase(IEnumerable<ArgumentValue> arguments)
        {
            return new SubstitutedPhrase(this, arguments.ToList());
        }

        public override string ToString()
        {
            return base.ToString() + " defined by " + matcher.ToString();
        }
    }
    internal class SubstitutedPhrase: IByteable {


        List<ArgumentValue> argValues;
        Phrase parent;
        
        internal SubstitutedPhrase(Phrase f, List<ArgumentValue> values)
        {
            parent = f;
            argValues = values;
        }

        public virtual IUnsubstitutedBytes toBytes()
        {
            Queue<ArgumentValue> argQue = new Queue<ArgumentValue>(argValues);
            List<byte> tmp=new List<byte>();
            List<Substitution> substitutions=new List<Substitution>();
            foreach (Opcode a in parent.codes)
            {
                IUnsubstitutedBytes b = a.getBytecode(argQue);
                foreach (Substitution sub in b.substitutions)
                {
                    Substitution nsub = sub;
                    nsub.position += tmp.Count;
                    substitutions.Add(nsub);
                }
                tmp.AddRange(b.bytes);
            }
            byte[] final = tmp.ToArray();
            

            foreach (Substitution sub in substitutions)
            {
                if (sub.rank == substitutionRank.FunctionOrder)
                {
                    switch (sub.type)
                    {
                        case substitutionType.argumentN:
                            if (ClassDatabase.getKind( parent.arguments[(int)sub.data]) == ClassDatabase.integer)
                            {

                            }
                            break;
                    }
                }
            }

            return new UnsubstitutedBytes(final, substitutions.ToArray());
        }
    }



    internal class CodeBlock: IEnumerable<SubstitutedPhrase>
    {
        
        List<SubstitutedPhrase> content=new List<SubstitutedPhrase>();
        public void addRange(IEnumerable<SubstitutedPhrase> num)
        {
            content.AddRange(num);
        }
        public void add(SubstitutedPhrase num)
        {
            content.Add(num);
        }
        public void add(Phrase p, IEnumerable<ArgumentValue> args)
        {
            content.Add(p.toSubstituedPhrase(args));
        }
        public override string ToString()
        {
            return base.ToString()+" of lenght "+content.Count.ToString();
        }
        /*public void add(Phrase p)
        {
            content.Add(p.toSubstituedPhrase());
        }*/

        public IEnumerator<SubstitutedPhrase> GetEnumerator()
        {
            return ((IEnumerable<SubstitutedPhrase>)content).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<SubstitutedPhrase>)content).GetEnumerator();
        }
    }

    class Function: Phrase
    {
        public string name;

        public Function(CodeBlock code, string name):base(new Argument[] { }, null, new StringMatcher(name), new Opcode(opcodeType.call,new ArgumentValue?[1] { null } ))
        {
            inside = code;
            this.name = name;
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
    }

    interface IOpcode
    {
        IUnsubstitutedBytes getBytecode(Queue<ArgumentValue> input);
        
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

        ArgumentValue?[] existingValues;
        opcodeType type;
        public Opcode (opcodeType type, ArgumentValue?[] existingValues)
        {
            this.type = type;
            this.existingValues = existingValues;
            if (existingValues == null)
            {
                throw new NullReferenceException("existing values can't be null");
            }
        }

        public IUnsubstitutedBytes getBytecode(Queue<ArgumentValue> values)
        {
            List<byte> bytes= new List<byte>();
            List<Substitution> subs=new List<Substitution>();
            bytes.AddRange(type.toBytes());
            List<ArgumentValue> args = new List<ArgumentValue>();
            ArgumentValue current;
            for (int i=0; i<existingValues.Length; i++)
            {
                if (existingValues[i] != null)
                {
                    current = (ArgumentValue)existingValues[i];
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
