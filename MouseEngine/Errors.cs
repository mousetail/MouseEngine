using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseEngine.Errors
{
    class ParsingException: Exception
    {
        int linenumber;
        public ParsingException(string s) : base(s)
        {
            linenumber = -1;
        }
        public ParsingException(int linenumber, string s): base(s)
        {
            this.linenumber = linenumber;
        }
    }

    class SyntaxError: ParsingException
    {
        public SyntaxError(string s): base(s)
        {

        }
        public SyntaxError(int linenumber, string s): base(linenumber, s){

        }
    }

    class OpcodeFormatError: ParsingException
    {
        public OpcodeFormatError(string s): base(s)
        {

        }
    }

    class ReturnTypeViolationError: ParsingException
    {
        public ReturnTypeViolationError(string s): base(s)
        {

        }
        public ReturnTypeViolationError(int linenumber, string s): base(linenumber, s)
        {

        }
    }

    class UnformatableObjectException : ParsingException
    {
        public UnformatableObjectException(string message) : base(message)
        {

        }
    }
}
