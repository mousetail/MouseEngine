using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseEngine.Lowlevel
{
    class BlockPhrase : Phrase
    {
        bool HasElse;
        Opcode[] after;
        public BlockPhrase(bool canElse, Argument[] args, Matcher mat, Opcode[] after, params Opcode[] before) : base(args, null, mat, before)
        {
            this.after = after;
            HasElse = canElse;
        }



        public Opcode[] getEnd()
        {
            return after;
        }

        public bool canDoElse()
        {
            return HasElse;
        }

        public override SubstitutedPhrase toSubstituedPhrase(IEnumerable<ArgumentValue> arguments, ArgumentValue? returnValue)
        {
            throw new Errors.OpcodeFormatError("trying to call ordinary form of toSubstitutedBytes on a phrase that requires a block");
        }

        public SubstitutedPhrase toSubstituedPhrase(IEnumerable<ArgumentValue> arguments, CodeBlock inside)
        {
            return new BlockSubstitutedPhrase(this, arguments.ToList(), null, inside);
        }
    }

    class BlockSubstitutedPhrase : SubstitutedPhrase
    {
        BlockPhrase par;
        CodeBlock inter;

        public BlockSubstitutedPhrase(BlockPhrase f, List<ArgumentValue> values, ArgumentValue? ret, CodeBlock inter) : base(f, values, ret)
        {
            par = f;
            this.inter = inter;
        }

        public override IUnsubstitutedBytes toBytes()
        {

            Queue<ArgumentValue> argQue = new Queue<ArgumentValue>(argValues);

            DynamicUnsubstitutedBytes tmp = new DynamicUnsubstitutedBytes();
            ArgumentValue? returnValue = this.returnValue;

            foreach (Opcode a in parent.codes)
            {
                tmp.Combine(a.toBytes(argQue, ref returnValue));
            }

            foreach (IPhraseSub s in inter)
            {
                tmp.Combine(s.toBytes());
            }

            foreach (Opcode a in par.getEnd())
            {
                tmp.Combine(a.toBytes(argQue, ref returnValue));
            }

            List<Substitution> subsToRemove = new List<Substitution>();

            foreach (Substitution s in tmp.substitutions)
            {
                if (s.type == substitutionType.NextElse)
                {
                    subsToRemove.Add(s);
                    tmp.WriteSlice(s.position, Writer.toBytes(tmp.Count - s.position - 2));
                }
                if (s.type == substitutionType.BlockStart)
                {
                    subsToRemove.Add(s);
                    tmp.WriteSlice(s.position, Writer.toBytes(-s.position - 2));
                }
            }

            foreach (Substitution s in subsToRemove)
            {
                tmp.Complete(s);
            }

            if (returnValue != null && returnValue.Value.getMode() != addressMode.zero)
            {
                throw new Errors.ReturnTypeViolationError("Attempt to use the return value of " + ToString() + " which has no return value");
            }

            return tmp;
        }
    }

    interface ICodeByteable: IByteable, IConditionArgValue
    {

    }

    interface IConditionArgValue: IArgItem
    {

    }

    struct ArgValueConditionFromArgument: IConditionArgValue
    {
        internal bool invert;
        internal ArgumentValue goTo;

        public ArgValueConditionFromArgument(bool invert)
        {
            this.invert = invert;
            goTo = new ArgumentValue(addressMode.constint, substitutionType.conditionDestination, 2, ClassDatabase.integer);
        }

        public ArgValueConditionFromArgument(bool invert, ArgumentValue goTo)
        {
            this.invert = invert;
            this.goTo = goTo;
        }
    }

    struct ConditionArgument: IByteable
    {
        public ICodeByteable generator;
        public ArgumentValue? result;

        public ConditionArgument(ICodeByteable gener, ArgumentValue? result)
        {
            generator = gener;
            this.result = result;
        }

        public IUnsubstitutedBytes toBytes()
        {
            return generator.toBytes();
        }
    }
    
    class Condition{

        Argument[] args;
        IConditionArgValue[] PosCodes;
        IConditionArgValue[] NegCodes;
        Matcher mat;

        public Condition(Argument[] args, Matcher mat, IConditionArgValue[] negCodes, params IConditionArgValue[] codes)
        {
            this.args = args;
            PosCodes = codes;
            this.mat = mat;
            NegCodes = negCodes;
        }

        public bool Match(string line)
        {
            return mat.match(line);
        }
        
        public Dictionary<string,string> getMatcherArgs()
        {
            return mat.getArgs();
        }

        public SubstitutedCondition toSubstitutedCondition(ArgumentValue JumpTo, ConditionArgument[] parts)
        {
            return new SubstitutedCondition(this, JumpTo, parts);
        }
        /// <summary>
        /// Return the arguments required so substitute this expression, not the arguments from the most recent match.
        /// </summary>
        /// <returns></returns>
        internal Argument[] getArgs()
        {
            return args;
        }
        internal IConditionArgValue[] getCodes(bool inverted)
        {
            if (inverted)
            {
                return NegCodes;
            }
            else
            {
                return PosCodes;
            }

        }

        public static Condition CondEquals = new Condition(
            new Argument[] { new Argument("c1", ClassDatabase.integer),
            new Argument("c2",ClassDatabase.integer)},
            new MultiStringMatcher(new[] { "c1", "c2" }, "", " is ", ""),
            new[]
            {
                new Opcode(opcodeType.jne, new ArgItemFromArguments(), new ArgItemFromArguments(), new ArgItemJumpTo())
            },
            new Opcode(opcodeType.jeq, new ArgItemFromArguments(), new ArgItemFromArguments(), new ArgItemJumpTo()
            ));

        public static Condition CondNot = new Condition(
            new Argument[]
            {
                new Argument("c1",ClassDatabase.condition)
            },
            new MultiStringMatcher(new[] { "c1" }, "not ", ""),
            new IConditionArgValue[] { new ArgValueConditionFromArgument(false) },
            new ArgValueConditionFromArgument(true)
            );

        public static Condition CondAnd = new Condition(
            new Argument[]
            {
                new Argument("c1",ClassDatabase.condition),
                new Argument("c2",ClassDatabase.condition)
            },
            new MultiStringMatcher(new[] { "c1", "c2" }, "", " and ", ""),
            new IConditionArgValue[]
            {
                new ArgValueConditionFromArgument(true),
                new ArgValueConditionFromArgument(true)
            }, new ArgValueConditionFromArgument(true, new ArgumentValue(addressMode.constint, substitutionType.endCondition, 1,
                ClassDatabase.integer)),
            new ArgValueConditionFromArgument(false)
            
            
        );
    }

    struct ArgItemJumpTo: IConditionArgValue
    {

    }

    class SubstitutedCondition: ICodeByteable
    {
        ConditionArgument[] arguments;
        Condition parent;
        ArgumentValue jumpTo;
        bool inveted;

        internal SubstitutedCondition(Condition parent, ArgumentValue JumpTo, ConditionArgument[] arguments)
        {
            this.arguments = arguments;
            this.parent = parent;
            jumpTo = JumpTo;
        }

        public IUnsubstitutedBytes toBytes()
        {
            int index = 0;
            IUnsubstitutedBytes tmp = new DynamicUnsubstitutedBytes();
            Queue<ArgumentValue> q = new Queue<ArgumentValue>(arguments.Where(x => (x.result!=null)).Select(x=>(ArgumentValue)x.result));
            foreach (IConditionArgValue v in parent.getCodes(inveted))
            {
                if (v is ArgItemFromArguments)
                {
                    tmp.Combine(arguments[index].toBytes());
#if DEBUG
                    if (arguments[index].result != null)
                    {
                        throw new Errors.OpcodeFormatError("some opcode has a result for something that shouldn't have");
                    }
#endif
                    index++;
                }
                else if (v is ArgValueConditionFromArgument)
                {
                    ArgValueConditionFromArgument b = (ArgValueConditionFromArgument)v;
                    if (!(arguments[index].generator is SubstitutedCondition))
                    {
                        throw new Errors.OpcodeFormatError("The arguments field requested a condition, but the opcodes field didn't");
                    }
                    SubstitutedCondition subcond = (SubstitutedCondition)arguments[index].generator;

                    if (b.goTo.getSubstitutionKind() == substitutionType.conditionDestination)
                    {
                        subcond.jumpTo = jumpTo;
                    }
                    else
                    {
                        subcond.jumpTo = b.goTo;
                    }
                    
                    subcond.setInvert(b.invert);

                    tmp.Combine(subcond.toBytes());

                    index++;

                   
                }
                else if (v is Opcode)
                {
                    Opcode b = (Opcode)v;
                    b.setJumpTo(jumpTo);
                    ArgumentValue? returnValue=null;
                    int initalLength = q.Count;
                    IUnsubstitutedBytes bits=b.toBytes(q,ref returnValue);
                    int newLenght = q.Count;
                    for (int i=0; i<(newLenght-initalLength); i++)
                    {
                        tmp.Combine(arguments[index].generator.toBytes());
                        index++;
                    }
                    tmp.Combine(bits);
                }
                else if (v is IByteable)
                {
                    tmp.Combine(((IByteable)v).toBytes());
                }
            }

            List<Substitution> toRemove = new List<Substitution>();
            List<Substitution> toAdd = new List<Substitution>();

            foreach (Substitution s in tmp.substitutions)
            {
                if (s.type == substitutionType.endCondition)
                {
                    if (s.data == 0)
                    {
                        tmp.WriteSlice(s.position, Writer.toBytes(tmp.Count - s.position - 2));
                        toRemove.Add(s);
                    }
                    else
                    {
                        Substitution d = s;
                        d.data -= 1;
                        toAdd.Add(d);
                        toRemove.Add(s);
                    }
                }
            }
            foreach (Substitution s in toRemove)
            {
                tmp.Complete(s);
            }


            foreach (Substitution s in toAdd)
            {
                tmp.addSubstitution(s);
            }
            return tmp;
        }

        internal void invert()
        {
            inveted ^= true;
        }

        internal void setInvert(bool invert)
        {
            inveted = invert;
        }
    }
    
}
