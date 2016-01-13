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

        public static Phrase returnf = new Phrase(new Argument[] { new Argument("value", ClassDatabase.integer) }, new MultiStringMatcher(new string[1] { "value" }, "return", ""), new Opcode(opcodeType.returnf, 0, new Argument("value",ClassDatabase.integer)));

        internal Argument[] arguments;
        Matcher matcher;
        internal Opcode[] codes;

        IValueKind returnType;

        public IValueKind getReturnType()
        {
            return returnType;
        }


        public Phrase(Argument[] args, Matcher matcher, params Opcode[] opcodes)
        {
            arguments = args;
            this.matcher = matcher;
            codes = opcodes;
        }

        public Dictionary<string, string> lastMatchArgs()
        {
            return matcher.getArgs();
        }

        public bool match(string s, ClassDatabase dtb)
        {
            return matcher.match(s, dtb);
        }

        public SubstitutedPhrase toSubstituedPhrase(Dictionary<string, string> arguments)
        {
            return new SubstitutedPhrase(this, arguments);
        }

        public override string ToString()
        {
            return base.ToString() + " defined by " + matcher.ToString();
        }
    }
    internal class SubstitutedPhrase: IByteable {


        Dictionary<string, string> argValues;
        Phrase parent;
        
        internal SubstitutedPhrase(Phrase f, Dictionary<string, string> arguments)
        {
            parent = f;
            argValues = arguments;
        }

        public virtual byte[] toBytes()
        {
            List<byte> tmp=new List<byte>();
            List<Substitution> substitutions=new List<Substitution>();
            foreach (Opcode a in parent.codes)
            {
                UnsubstitutedBytes b = a.getBytecode(argValues);
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

            return final;
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
        public void add(Phrase p, Dictionary<string, string> arguments)
        {
            content.Add(p.toSubstituedPhrase(arguments));
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

        public Function(CodeBlock code, string name):base(new Argument[] { }, new StringMatcher(name), new Opcode(opcodeType.call,0,new Argument("Value",ClassDatabase.integer)) )
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

    class PhraseWithBody: Phrase
    {
        CodeBlock inside;
        public PhraseWithBody(Argument[] args, Matcher m, params Opcode[] opcodes):base(args, m, opcodes)
        {

        }

    }

    interface IOpcode
    {
        UnsubstitutedBytes getBytecode(Dictionary<string, string> args);
        
    }



    class Opcode: IOpcode
    {
        opcodeType type;
        int[] addressmodes;
        int[] arguments;
        Substitution?[] substitutions;
        public Opcode(opcodeType type, int[] addressmodes, int[] arguments, Substitution?[] substitutions)
        {
            this.type = type;
            this.addressmodes = addressmodes;
            this.arguments = arguments;
            this.substitutions = substitutions;
        }
        /*
        opcodeType code;
        Argument[] arguments;
        int argindex;
        public Opcode(opcodeType code, int argindex, params Argument[] arguments)
        {
            this.code = code;
            this.arguments = arguments;
            this.argindex = argindex;
            Console.Write("length of arguments: ");
            Console.WriteLine(arguments.Length);
        }

        public UnsubstitutedBytes getBytecode(Dictionary<string, string> arguments)
        {
            List<byte> start = new List<byte>();
            List<addressMode> modes=new List<addressMode>();
            start.AddRange(code.toBytes());
            List<byte> argumentsBytes = new List<byte>();
            List<Substitution> substitutions=new List<Substitution>();
            int index = 0;
            Console.Write("This is the number of arguments ");
            Console.WriteLine(this.arguments.Length);
            foreach (Argument b in this.arguments)
            {
                
                if (b.type is IntValueKind)
                {
                    Console.WriteLine("I have an int value argument");
                    modes.Add(addressMode.constint);
                    substitutions.Add(new Substitution(arguments.Count, substitutionType.argumentN, substitutionRank.FunctionOrder, argindex + index)); //scary dinosour
                    argumentsBytes.AddRange(Writer.toBytes(255255));

                }
                else
                {
                    Console.WriteLine("CAn't find");
                    Console.WriteLine(b);
                    throw new InvalidProgramException("The program is wrong");
                }
            }
            start.AddRange(makeaddrict(modes));
            start.AddRange(argumentsBytes);
            return new UnsubstitutedBytes(start.ToArray(),substitutions.ToArray());

        }*/

        static byte[] makeaddrict(List<addressMode> modes)
        {
            byte[] tmp = new byte[(modes.Count + 1) / 2];

            for (int i=0; i<modes.Count; i++)
            {
                if ((i % 2) == 0)
                {
                    tmp[i / 2] += (byte)modes[i];
                }
                else
                {
                    tmp[i / 2] += (byte)(16 * (byte)modes[i]);
                }
                Console.Write(tmp[i / 2]);
                Console.WriteLine(" is the last value of TMP");
            }
            return tmp;
        }
    }
}
