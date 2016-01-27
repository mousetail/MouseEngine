using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseEngine.Lowlevel
{
    class BlockPhrase: Phrase
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

    class BlockSubstitutedPhrase: SubstitutedPhrase
    {
        BlockPhrase par;
        CodeBlock inter;

        public BlockSubstitutedPhrase(BlockPhrase f, List<ArgumentValue> values, ArgumentValue? ret, CodeBlock inter):base(f, values, ret)
        {
            par = f;
            this.inter = inter;
        }

        public override IUnsubstitutedBytes toBytes()
        {

            Queue<ArgumentValue> argQue = new Queue<ArgumentValue>(argValues);

            DynamicUnsubstitutedBytes tmp=new DynamicUnsubstitutedBytes();
            ArgumentValue? returnValue = this.returnValue;

            foreach (Opcode a in parent.codes)
            {
                tmp.Combine(a.getBytecode(argQue, ref returnValue));
            }

            foreach (IPhraseSub s in inter)
            {
                tmp.Combine(s.toBytes());
            }

            foreach (Opcode a in par.getEnd())
            {
                tmp.Combine(a.getBytecode(argQue, ref returnValue));
            }

            List<Substitution> subsToRemove=new List<Substitution>();

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
    
}
