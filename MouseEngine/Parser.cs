using System;
using System.Collections;
using System.Collections.Generic;
using MouseEngine;
using System.Linq;
using MouseEngine.Lowlevel;


namespace MouseEngine
{
    enum pStatus: byte {Working, Finished, SyntaxError, RelationError}
    enum ObjType: byte { Object, Kind}
    enum ProcessingInternal: byte { GlobalFunction, LocalFunction }
    enum BlockKind: byte { Loop=21, Condition=45} //These number are NOT actually important, I just need to avoid any being 0

    public class Parser
    {
        BlockParser currentBlock;
        Databases databases;
        public Parser()
        {
            databases = Databases.getDefault();

            currentBlock = new BlockParser(databases);
        }
        public void Parse(string line)
        {
            if (line[0] != '#' && line[0] != '/' && !StringUtil.isBlank(line))
            {
                pStatus result = currentBlock.Parse(line);
                while (result == pStatus.Finished)
                {
                    currentBlock = new BlockParser(databases);
                    result=currentBlock.Parse(line);
                }
            }
        }
        public Databases getDatabases()
        {
            return databases;
        }
    }
    class BlockParser
    {
        static string[] nameKind = { "Name", "kind" };
        static string[] functionname = { "Name" };

        static Matcher ObjectFirstLine = new MultiStringMatcher(nameKind, "The ", " is a ", ":");
        static Matcher PropertyDefinition = new MultiStringMatcher(new string[] { "property", "value" }, "", " is ", "");
        static Matcher KindDefinition = new orMatcher(new MultiStringMatcher(nameKind, "A ", " is a ", ":"),
                                                          new MultiStringMatcher(nameKind, "An ", " is a ", ""));
        static Matcher NewAttribute = new MultiStringMatcher(nameKind, "Make property ", " of kind ", "");
        static Matcher GlobalFunction = new MultiStringMatcher(functionname, "To ", ":");
        bool started;
        Databases dtbs;

        ClassDatabase dtb;

        int indentation;
        bool indented;

        ObjType tobj;

        Prototype eobj;

        ProcessingInternal func;
        string funcName;

        CodeParser internalParser;

        public BlockParser(Databases dtbs)
        {
            started = false;
            this.dtbs = dtbs;
            dtb = dtbs.cdtb;
        }
        public pStatus Parse(string line)
        {
            Dictionary<string, string> v;
            if (internalParser != null)
            {
                pStatus w = internalParser.parse(line);
                if (w == pStatus.Finished)
                {
                    if (func == ProcessingInternal.GlobalFunction)
                    {
                        dtb.functionDatabase.AddGlobalFunction(internalParser.getBlock(), funcName, internalParser.getNumArgs());
                    }
                    internalParser = null;
                }
                else
                {
                    return w;
                }
            }
            if (!started)
            {
                if (ObjectFirstLine.match(line))
                {
                    
                    v = ObjectFirstLine.getArgs();
                    eobj = dtb.getObject((string)v["Name"],true);
                    eobj.setParent(dtb.getKind((string)v["kind"],false));
                    tobj = ObjType.Object;
                    started = true;
                }
                else if (KindDefinition.match(line))
                {
                    v = KindDefinition.getArgs();
                    if (v != null)
                    {
                        eobj = dtb.getKind((string)v["Name"],true);
                        eobj.setParent(dtb.getKind((string)v["kind"],false));
                        tobj = ObjType.Kind;
                        started = true;
                    }
                    else
                    {
                        Console.Write("StringError: Object ");
                        Console.Write(KindDefinition.GetType());
                        Console.Write(" returned invalid value after get args, should fail on match if args not valid.");
                    }

                }
                else
                {
                    Console.WriteLine("No match");
                }
            }
            else
            {
                int newindentation = StringUtil.getIndentation(line);
                
                if (!indented)
                {
                    indented = true;
                    indentation = newindentation;
                }
                else if (newindentation == 0)
                {
                    return pStatus.Finished;
                }

                string strippedLine = line.Substring(indentation);

                if (PropertyDefinition.match(strippedLine))
                {
                    v = PropertyDefinition.getArgs();
                    eobj.setAttr(v["property"], dtb.ParseAnything(v["value"]));
                }
                else if (tobj == ObjType.Kind && NewAttribute.match(strippedLine))
                {
                    Console.WriteLine("This is an attribute");
                    v = NewAttribute.getArgs();
                    ((KindPrototype)eobj).MakeAttribute((string)v["Name"], dtb.getKindAny( (string)v["kind"], false), true);

                }
                else if (GlobalFunction.match(strippedLine))
                {
                    internalParser = new CodeParser(dtbs, indentation);
                    func = ProcessingInternal.GlobalFunction;
                    funcName = (string)GlobalFunction.getArgs()["Name"];
                }
            }
            return pStatus.Working;


        }
        
    }
    class CodeParser
    {
        //List<byte> data;

        static Matcher localVariableMathcer = new MultiStringMatcher(new[] { "name", "expression" }, "let ", " be ", "");
        static Matcher ifMatcher = new MultiStringMatcher(new[] { "condition" }, "if ", ":");
        static Matcher whileMatcher = new MultiStringMatcher(new[] { "condition" }, "while ", ":" );
        static Matcher elseMatcher = new MultiStringMatcher(new[] { "condition" }, "else", "");


        /// <summary>
        /// A internal parser to redict output to if it exists.
        /// </summary>
        CodeParser nested;

        /// <summary>
        /// The block things are written to.
        /// </summary>
        CodeBlock block;


        /// <summary>
        /// The various databeses that are needed for methods
        /// </summary>
        FunctionDatabase fdtb;
        ClassDatabase cdtb;
        StringDatabase sdtb;


        /// <summary>
        /// Used for recovery after conditions or loops
        /// </summary>
        ifElseCodeBlock internalBlock;
        BlockKind lastBlockKind;

        /// <summary>
        /// Used to check the when the function is done or indentation has become invalid
        /// </summary>
        int indentation;
        bool indented;
        int oldindentation;
        
        /// <summary>
        /// The local variables for this function
        /// </summary>
        Dictionary<string, LocalVariable> locals=new Dictionary<string, LocalVariable>();

        int getFramePos(int varindex)
        {
            return varindex*4;
        }

        public CodeParser(Databases dtbs, int indentation)
        {
            fdtb = dtbs.fdtb;
            cdtb = dtbs.cdtb;
            sdtb = dtbs.sdtb;
            oldindentation = indentation;
            indented = false;
            block = new CodeBlock();
        }

        private CodeParser(ClassDatabase cdtb, FunctionDatabase fdtb, StringDatabase sdtb, int indentation, Dictionary<string, LocalVariable> loc)
        {
            this.cdtb = cdtb;
            this.sdtb = sdtb;
            this.fdtb = fdtb;
            oldindentation = indentation;
            locals = loc;
            indented = false;
            block = new CodeBlock();
        }


        public void setUpCondition(string condition)
        {
            setUpCondition();
            SubstitutedCondition d = EvalCondition(condition);
            d.invert();
            internalBlock.add(d);
        }

        public void setUpCondition()
        {
            if (internalBlock == null)
            {
                internalBlock = new ifElseCodeBlock();
            }
            nested = new CodeParser(cdtb, fdtb, sdtb, indentation, locals);
            internalBlock.addIfRange(internalBlock.Count, internalBlock.Count);

        }

        public pStatus parse(string line)
        {
            bool finishInternal = false;
            bool doEsle = false;

            if (nested != null)
            {
                pStatus stat= nested.parse(line);
                if (stat==pStatus.Working || stat == pStatus.SyntaxError)
                {
                    return stat;
                }
                else if (stat == pStatus.Finished)
                {
                    finishInternal = true;

                }
            }
            else
            {
                //short f=0;
            }
            int newindentation = StringUtil.getIndentation(line);
            if (!indented && newindentation > oldindentation)
            {
                indentation = newindentation;
            }
            else if (newindentation > indentation)
            {
                throw new Errors.IndentationError("unexpected indent");
            }
            else if (newindentation == oldindentation)
            {
                Console.WriteLine("Finished parsing function, because indentation is "+oldindentation.ToString());
                Console.WriteLine("\tLine: " + line);
                DictUtil.SayDict(locals);
                return pStatus.Finished;
            }
            else if (newindentation < indentation)
            {
                throw new Errors.IndentationError("unexpected dedent (old indentation was "+oldindentation.ToString()+" but new indentation is "+newindentation.ToString()+")");
            }

            string shortString = line.Substring(newindentation).Trim(StringUtil.whitespace);

            if (finishInternal)
            {
                CodeBlock b = nested.getBlock();
                doEsle = elseMatcher.match(shortString);
                if (doEsle)
                {
                    shortString = elseMatcher.getArgs()["condition"];
                    if (shortString.StartsWith(" "))
                    {
                        shortString = shortString.Substring(1);
                    }
                }

                if (lastBlockKind == BlockKind.Condition)
                {
                    if (doEsle)
                    {
                        b.add(new Opcode(opcodeType.jump, new ArgumentValue(addressMode.constint, substitutionType.EndIf, ClassDatabase.integer)));
                    }
                }
                else
                {
                    b.add(new Opcode(opcodeType.jump, new ArgumentValue(addressMode.constint, substitutionType.BlockStart, ClassDatabase.integer)));
                }
                internalBlock.add(b);
                nested = null;

                if (!doEsle)
                {
                    block.add(internalBlock);
                    internalBlock = null;
                }
                else
                {

                }

            }

            if (localVariableMathcer.match(shortString))
            {
                Dictionary<string, string> args = localVariableMathcer.getArgs();
                if (locals.ContainsKey(args["name"]))
                {
                    ArgumentValue argva = EvalExpression(args["expression"]);
                    LocalVariable lvar = locals[args["name"]];
                    if (lvar.kind.isParent(argva.getKind()))
                    {
                        block.add(Phrase.assign.toSubstituedPhrase(new[] { argva, new ArgumentValue(addressMode.frameint, getFramePos(lvar.index)) },
                            null));
                    }
                }
                else
                {
                    ArgumentValue k = EvalExpression(args["expression"]);
                    block.add(Phrase.assign.toSubstituedPhrase(new[] { k,
                    new ArgumentValue(addressMode.frameint, getFramePos(locals.Count)),
                    }, null));
                    locals.Add(args["name"], new LocalVariable(locals.Count, k.getKind()));
                }
            }
            else if (ifMatcher.match(shortString))
            {

                lastBlockKind = BlockKind.Condition;
                setUpCondition(ifMatcher.getArgs()["condition"]);
            }
            else if (whileMatcher.match(shortString))
            {
                lastBlockKind = BlockKind.Loop;
                setUpCondition(whileMatcher.getArgs()["condition"]);
            }
            else if (doEsle && shortString == ":")
            {
                setUpCondition();
            }
            else if (doEsle)
            {
                throw new Errors.SyntaxError("else without a valid condition");
            }
            else
            {

                EvalExpression(shortString, false);
            }
            return pStatus.Working;
        }

        private SubstitutedCondition EvalCondition(string shortString)
        {
            Exception lastEx=null;

            foreach (Condition cond in fdtb.globalConditions)
            {
                if (cond.Match(shortString))
                {
                    try {
                        Dictionary<string, string> MatcherArgs = cond.getMatcherArgs();
                        Argument[] VArgs = cond.getArgs();
                        List<ConditionArgument> tmpArguments = new List<ConditionArgument>();
                        foreach (Argument ar in VArgs)
                        {
                            if (ar.type == ClassDatabase.condition)
                            {
                                tmpArguments.Add(new ConditionArgument(EvalCondition(MatcherArgs[ar.name]), null));
                            }
                            else
                            {
                                CodeBlock conditionBlock = new CodeBlock();
                                ArgumentValue val = EvalExpression(MatcherArgs[ar.name], conditionBlock);
                                tmpArguments.Add(new ConditionArgument(conditionBlock, val));
                            }
                        }
                        return cond.toSubstitutedCondition(new ArgumentValue(addressMode.constint, substitutionType.NextElse, ClassDatabase.integer), tmpArguments.ToArray());
                    }
                    catch (Errors.ParsingException x)
                    {
                        lastEx = x;
                    }
                }
            }
            if (lastEx == null)
            {
                throw new Errors.UnformatableObjectException("Can't find a condition to match " + shortString);
            }
            else
            {
                throw lastEx;
            }
        }

        /// <summary>
        /// Note that this function will add phrases to the internal code block, the order iterations of this function are called
        /// matters.
        /// </summary>
        /// <param name="expression">the expression te be evaluated. This should be stripped of all indentation and extra
        /// spaces</param>
        /// <returns></returns>
        /// 
        public ArgumentValue EvalExpression(string expression)
        {
            return EvalExpression(expression, block, true);
        }

        public ArgumentValue EvalExpression(string expression, CodeBlock placeToWriteTo)
        {
            return EvalExpression(expression, placeToWriteTo, true);
        }

        public ArgumentValue EvalExpression(string expression, bool useOutput)
        {
            return EvalExpression(expression, block, useOutput);
        }

        public ArgumentValue EvalExpression(string expression, CodeBlock placeToWriteTo, bool useOutput)
        {
            if (locals.ContainsKey(expression))
            {
                return new ArgumentValue(addressMode.frameint, getFramePos((int)locals[expression]), locals[expression].kind);
            }
            object b = cdtb.ParseAnything(expression);
            if (b != null)
            {
                return toValue(b);
            }
            else
            {
                ArgumentValue? returnValue=null;
                Exception lastEx = null;
                foreach (Phrase f in fdtb)
                {
                    if (f.match(expression, cdtb))
                    {
                        try
                        {
                            returnValue=evalPhrase(f, placeToWriteTo, useOutput);
                            break;
                        }
                        catch (Errors.ParsingException ex)
                        {
                            lastEx = ex;
                            returnValue = null;
                        }
                    }
                }
                if (returnValue == null)
                {
                    if (lastEx == null)
                    {
                        throw new Errors.SyntaxError("No code way found to match \"" + expression + "\"");
                    }
                    else
                    {
                        throw lastEx;
                    }
                }
                return (ArgumentValue)returnValue;
            }
        }

        public ArgumentValue evalPhrase(Phrase matchedExpression, CodeBlock placeToWriteTo, bool useOutput)
        {
            ArgumentValue returnValue;
            if (matchedExpression.getReturnType()== null || !useOutput){
                returnValue = ArgumentValue.Zero;
            }
            else
            {
                returnValue = ArgumentValue.getPull(matchedExpression.getReturnType());
            }
            Dictionary<string, string> args = matchedExpression.lastMatchArgs();
            Stack<ArgumentValue> argValues = new Stack<ArgumentValue>();
            foreach (Argument v in matchedExpression.arguments.Reverse())
            {
                ArgumentValue t = EvalExpression(args[v.name]);

                if (!v.type.isParent(t.getKind()))
                {
                    throw new Errors.TypeMismatchException("type " + v.type.ToString() + " is incompatable with " + t.getKind().ToString());
                }

                if (v.isStackArgument)
                {


                    if (t.getMode() != addressMode.stack)
                    {
                        placeToWriteTo.add(Phrase.push.toSubstituedPhrase(new ArgumentValue[] { t }, null));
                    }
                }
                else
                {
                    argValues.Push(t);
                }
            }
            placeToWriteTo.add(matchedExpression.toSubstituedPhrase(argValues, returnValue));
            return returnValue;
        }

        public CodeBlock getBlock()
        {
            return block;
        }

        private ArgumentValue toValue(object v)
        {
            if (v is int)
            {
                if (v.Equals(0))
                {
                    return new ArgumentValue(addressMode.zero, ClassDatabase.integer) ;
                }
                else
                {
                    return new ArgumentValue(addressMode.constint, (int)v);
                }
            }
            else if (v is string)
            {
                StringItem l = sdtb.getStr((string)v);
                return (ArgumentValue)l;
            }
            //TODO: Add options for kind and item prototypes
            throw new Errors.UnformatableObjectException("object \"" + v.ToString() + "\" of type \""+v.GetType().ToString()+" has no possible format");
        }

        internal int getNumArgs()
        {
            return locals.Count;
        }
    }

    

    interface IArgItem
    {
    }

    struct ArgItemReturnValue: IArgItem
    {
    }

    struct ArgItemFromArguments: IArgItem, IConditionArgValue
    {
    }

    struct ArgumentValue: IArgItem
    {
        static public ArgumentValue Zero = new ArgumentValue(addressMode.zero);
        static public ArgumentValue Push = new ArgumentValue(addressMode.stack);
        static public ArgumentValue Pull = Push;

        static public ArgumentValue getPull(IValueKind kind)
        {
            ArgumentValue t = Pull;
            t.kind = kind;
            return t;
        }

        addressMode mode;
        byte[] data;
        substitutionType? substitutionType;
        IValueKind kind;
        int substitutionData;

        public ArgumentValue(addressMode mode)
        {
            this.mode = mode;
            this.data = new byte[0];
            substitutionType = null;
            substitutionData = 0;
            kind = ClassDatabase.nothing; 
        }

        public ArgumentValue(addressMode mode, IValueKind kind):this(mode)
        {
            this.kind = kind;
        }

        public ArgumentValue(addressMode mode, int value)
        {
            this.mode = mode;
            this.substitutionType = null;
            this.substitutionData = 0;
            bool shorten = false; ;
            if (mode == addressMode.constint)
            {
                data = Writer.toBytes(value, true, false);
                shorten = true;
            }
            else if ((mode==addressMode.frameint) || (mode==addressMode.ramint) || (mode == addressMode.addrint))
            {
                data = Writer.toBytes(value, true, false);
                shorten = true;
            }
            else
            {
                this.data = Writer.toBytes(value);
                shorten = false;
            }

            if (shorten) {
                switch (data.Length)
                {
                    case 4:
                        break;
                    case 2:
                        this.mode = (addressMode)((int)mode - 1);
                        break;
                    case 1:
                        this.mode = (addressMode)((int)mode - 2);
                        break;
                    default:
                        throw new Errors.NumberOutOfRangeException("address mode should return a either 4,2, or 1");
                }
            }

            kind = ClassDatabase.integer;
        }

        public ArgumentValue(addressMode mode, substitutionType type, IValueKind kind)
        {
            this.mode = mode;
            substitutionType = type;
            data = new byte[4];
            substitutionData = 0;
            this.kind = kind;
            
        }

        public ArgumentValue(addressMode mode, substitutionType type, int subdata, IValueKind kind)
        {
            this.mode = mode;
            substitutionType = type;
            data = new byte[4];
            substitutionData = subdata;
            this.kind = kind;
        }

        public ArgumentValue(addressMode mode, int value, IValueKind kind) : this(mode, value)
        {
            this.kind = kind;
        }

        public addressMode getMode()
        {
            return mode;
        }

        public IValueKind getKind()
        {
            return kind;
        }

        public Substitution? getSubstitution(int position)
        {
            if (substitutionType != null)
            {
                return new Substitution(position, (substitutionType)substitutionType, substitutionRank.Normal, substitutionData);
            }
            return null;
        }

        public byte[] getData()
        {
            
            return data;

        }

        internal substitutionType? getSubstitutionKind()
        {
            return substitutionType;
        }
    }

    struct LocalVariable
    {
        public int index;
        public IValueKind kind;
        public static explicit operator int (LocalVariable a)
        {
            return a.index;
        }

        public LocalVariable(int index, IValueKind kind)
        {
            this.index = index;
            this.kind = kind;
        }

        public override string ToString()
        {
            return index.ToString() + " of kind " + kind.ToString();
        }
    }
}
