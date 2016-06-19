using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseEngine.Errors
{
    public interface IErrorData
    {
        string getExpandedString();
        string getTitle();
    }

    public interface IExDataException
    {
        IErrorData getData();
    }
    public interface IMultiExDataException
    {
        IErrorData[] getData();
    }

    public class ParsingException: Exception
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

    class UnformatableObjectException : ParsingException, IExDataException
    {
        public UnformatableObjectException(string message) : base(message)
        {
            dat = null;
        }

        IErrorData dat;

        public UnformatableObjectException(string message, List<string> data): base(message)
        {
            dat = new UnfData(data);
        }

        public IErrorData getData()
        {
            return dat;
        }
    }

    class UnfData : IErrorData
    {
        List<String> exParts;

        public UnfData(List<string> exParts)
        {
            this.exParts = exParts;
        }

        public string getExpandedString()
        {
            return exParts.Aggregate((a, b)=>a + "\n" + b);
        }

        public string getTitle()
        {
            return exParts[0];
        }
    }

    class IDMismatchException: ParsingException
    {
        public IDMismatchException(string message) : base(message)
        {

        }
        public IDMismatchException(int linenumber, string message): base(linenumber, message)
        {

        }
    }

    class TypeMismatchException: ParsingException
    {
        public TypeMismatchException(string message): base(message)
        {

        }
        public TypeMismatchException(int linenumber, string message): base(linenumber, message)
        {

        }
    }

    class NumberOutOfRangeException: ParsingException
    {
        public NumberOutOfRangeException(string s): base(s)
        {

        }
    }

    class IndentationError: ParsingException
    {
        public IndentationError(string s) : base(s)
        {

        }
        public IndentationError(): base("unexpected indent")
        {

        }
    }

    class InvalidIncreaseIndent: IndentationError
    {
        public InvalidIncreaseIndent(string s): base(s)
        {

        }
    }
    class InvalidDecreaseIndent: IndentationError
    {
        public InvalidDecreaseIndent(string s): base(s){

        }
    }

    public class ErrorStack: ParsingException
    {
        List<ParsingException> internalErrors;

        public ErrorStack(IEnumerable<ParsingException> internalErrors): base("Multiple Errors")
        {
            this.internalErrors = internalErrors.ToList();
        }

        public List<ParsingException> getErrors()
        {
            return internalErrors;
        }


    }

    class ReservedWordError:ParsingException
    {
        public ReservedWordError(string message) : base(message)
        {

        }
    }
}
