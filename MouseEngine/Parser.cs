using System;
using System.Collections;
using System.Collections.Generic;
using MouseEngine;
using System.Linq;
using MouseEngine.Lowlevel;


namespace MouseEngine
{
    public enum pStatus: byte {Working, Finished, SyntaxError, RelationError}
    enum ObjType: byte { Object, Kind}
    enum ProcessingInternal: byte { GlobalFunction, LocalFunction }
    enum BlockKind: byte { Loop=1, Condition=2} //These number are NOT actually important, I just need to avoid any being 0

    interface IParser
    {
        pStatus Parse(string line, int linenumber);
    }

    public class Parser: IParser
    {
        BlockParser currentBlock;
        Databases databases;
        Dictionary<int, Function> startLines=new Dictionary<int, Function>();
        public Parser()
        {
            databases = Databases.getDefault();

            currentBlock = new BlockParser(databases);
        }
        public pStatus Parse(string line, int linenumber)
        {
            if (!isComment(line))
            {
                pStatus result = currentBlock.Parse(line, linenumber);
                while (result == pStatus.Finished)
                {
                    foreach (KeyValuePair<int, Function> pair in currentBlock.startLines)
                    {
                        startLines[pair.Key] = pair.Value;
                    }
                    currentBlock = new BlockParser(databases);
                    result=currentBlock.Parse(line, linenumber);
                }
            }
            return pStatus.Working;
        }
        public Databases getDatabases()
        {
            return databases;
        }
        /// <summary>
        /// Checks if a given line should be ignored because it's blank or starts
        /// with a comment character.
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns></returns>
        bool isComment(string line)
        {
            line = line.TrimStart(StringUtil.whitespace);
            return line.StartsWith("#")||line.StartsWith("/")||StringUtil.isBlank(line);
        }


        /// <summary>
        /// Preforms some stuff that needs to be done in between code parsing and class parsing.
        /// The things are:
        /// *Calling Class Database index checking func
        /// </summary>
        public void prepareForSecondStage()
        {
            databases.cdtb.doIndexChecking(); //2 ms

            foreach (KeyValuePair<int, Function> pair in currentBlock.startLines)
            {
                startLines[pair.Key] = pair.Value;
            }
            currentBlock = null;
        }

        //DATA USED FOR PARSE 2
        Function key;
        CodeParser Phate2Block;
        int lastindentation = 0;

        public pStatus Parse2(string line, int linenumber)
        {
            if (isComment(line))
            {
                return pStatus.Working;
            }
            if (Phate2Block != null)
            {
                pStatus result= Phate2Block.Parse(line, linenumber);
                if (result == pStatus.Finished)
                {
                    key.setBlock(Phate2Block.getBlock());
                    Phate2Block = null;

                }
                else
                {
                    return result;
                }
            }

            if (startLines.ContainsKey(linenumber))
            {
                Phate2Block = new CodeParser(databases, startLines[linenumber], lastindentation);
                key = startLines[linenumber];

            }
            else
            {
                lastindentation = StringUtil.getIndentation(line);
            }
            return pStatus.Working;
        }

        /// <summary>
        /// Should be called after EOF in stage 2
        /// This F makes sure all data is saved when a function
        /// does not end naturally because of the EOF,
        /// or does nothing if there is no such function
        /// </summary>
        public void finishStage2()
        {
            if (Phate2Block != null)
            {
                key.setBlock(Phate2Block.getBlock());

            }
        }
    }

    class IndentationManager
    {
        /// <summary>
        /// Used to check the when the function is done or indentation has become invalid
        /// </summary>
        int indentation;
        bool indented = false;
        int oldindentation;

        public IndentationManager(IndentationManager indentation1)
        {
            oldindentation = indentation1.indentation;
            indentation = oldindentation;
        }

        public IndentationManager(int lastIndentation)
        {
            oldindentation = lastIndentation;
            indentation = lastIndentation;
        }

        internal string doIndentation(string line)
        {
            int newindentation = StringUtil.getIndentation(line);
            if (!indented && newindentation > oldindentation)
            {
                indentation = newindentation;
                indented = true;
            }
            else if (newindentation > indentation)
            {
                throw new Errors.InvalidIncreaseIndent("unexpected indent");
            }
            else if (!indented)
            {
                throw new Errors.InvalidDecreaseIndent("expected an indent");
            }
            else if (newindentation == oldindentation || !indented)
            {
                return null;
            }
            else if (newindentation < indentation)
            {
                throw new Errors.InvalidDecreaseIndent("a dedent didn't match the level of a previous indent of " + oldindentation.ToString());
            }
            else if (indentation == newindentation)
            {
                goto loopend;
            }
            else
            {
                throw new Errors.IndentationError("Some error, I don't know ecactly");
            }
            loopend:
            return line.Substring(newindentation).Trim(StringUtil.whitespace);
        }
    }
    class BlockParser: IParser
    {

        #region static phrases
        static string[] nameKind = { "Name", "kind" };
        static string[] functionname = { "Name" };

        static Matcher ObjectFirstLine = new MultiStringMatcher(nameKind, "The ", " is a ", ":");
        static Matcher PropertyDefinition = new MultiStringMatcher(new string[] { "property", "value" }, "", " is ", "");
        static Matcher KindDefinition = new orMatcher(new MultiStringMatcher(nameKind, "A ", " is a ", ":"),
                                                          new MultiStringMatcher(nameKind, "An ", " is a ", ""));
        static Matcher NewAttribute = new MultiStringMatcher(nameKind, "Make property ", " of kind ", "");
        static Matcher GlobalFunction = new MultiStringMatcher(functionname, "To ", ":");
        static Matcher GlobalReturnFunctin = new MultiStringMatcher(new[] { "kind", "args" },
            "To decide what ", " is ",":");
        static Matcher ArgumentMatcher = new orMatcher(
            new MultiStringMatcher(nameKind, "", ", a", ""),
            new MultiStringMatcher(nameKind, "", ", an", "")
            );

        #endregion
        bool started;
        Databases dtbs;

        ClassDatabase dtb;

        IndentationManager indent;

        ObjType tobj;

        Prototype eobj;

        

        internal Dictionary<int, Function> startLines=new Dictionary<int, Function>(); 

        IParser internalParser;

        Function parseFunctionDefinition(string definition)
        {
            bool isGlobal = true;

            IValueKind returnType;
            string argsStr;
            if (GlobalReturnFunctin.match(definition))
            {
                Dictionary<string, string> ar = GlobalReturnFunctin.getArgs();
                returnType = dtb.getKindAny(ar["kind"], false);
                argsStr = ar["args"];
            }
            else if (GlobalFunction.match(definition))
            {
                returnType = null;
                argsStr = GlobalFunction.getArgs()["Name"];
            }
            else
            {
                throw new InvalidOperationException("Can't call this function if no global functions");
            }


            Range[] matcherRanges = StringUtil.getProtectedParts(argsStr).ToArray();

            string[] argumentParts = StringUtil.getInsideStrings(matcherRanges, argsStr);

            string[] matcherStrings = StringUtil.getInsideStrings(StringUtil.getUnprotectedParts(argsStr).ToArray(), argsStr);


            List<Argument> arguments = new List<Argument>();

            foreach (string b in argumentParts)
            {
#if DEBUG
                Console.Write("argument part: ");
                Console.WriteLine(b);
#endif

                if (b.Equals("(this)") || b.Equals("(me)"))
                {
                    if (!isGlobal) //If I'm allready global, there shouldn't be another this/me
                    {
                        throw new Errors.SyntaxError("A function def can only contain one me or this");
                    }

                    isGlobal = false; //If it contains this or me, it's a local function

                    arguments.Add(Argument.getSelf((KindPrototype)eobj));
                }
                else
                {

                    if (!ArgumentMatcher.match(b))
                    {
                        throw new Errors.SyntaxError("a argument should be in the form (name), a (kind), was \"" + b + "\"");
                    }
                    Dictionary<string, string> argumentArgs = ArgumentMatcher.getArgs();
                    arguments.Add(new Argument(argumentArgs["Name"], dtb.getKindAny(argumentArgs["kind"], false)));
                }
            }

            if (isGlobal)
            {
                return dtbs.fdtb.AddGlobalFunction(
                    eobj,
                    new MultiStringMatcher(arguments.Select(x => x.name).ToArray(), matcherStrings),
                    arguments, returnType);
            }
            else
            {
                if (eobj is KindPrototype)
                {
                    return dtbs.fdtb.AddLocalFunction(
                        (KindPrototype)eobj,
                        new MultiStringMatcher(arguments.Select(x => x.name).ToArray(), matcherStrings),
                        arguments, returnType);
                }
                else
                {
                    throw new Errors.ParsingException("Can't define a local function in a object");
                }
            }
        }

        public BlockParser(Databases dtbs)
        {
            started = false;
            this.dtbs = dtbs;
            dtb = dtbs.cdtb;
            indent = new IndentationManager(0);
        }
        public pStatus Parse(string line, int linenumber)
        {
            Dictionary<string, string> v;
            if (internalParser != null)
            {
                pStatus w = internalParser.Parse(line, linenumber);
                if (w == pStatus.Finished)
                {
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
                    throw new Errors.ItemMatchException("Expected either a kind of object definition, got " + line,
                        ObjectFirstLine.getLastError(),
                        KindDefinition.getLastError());
                }
            }
            else
            {
                string strippedLine = indent.doIndentation(line);
                if (strippedLine == null)
                {
                    return pStatus.Finished;
                }

                else if (GlobalFunction.match(strippedLine))
                {
                    startLines[linenumber] = parseFunctionDefinition(strippedLine);
                    internalParser = new DummyParser(indent);
                }
                else if (PropertyDefinition.match(strippedLine))
                {
                    v = PropertyDefinition.getArgs();
                    eobj.setAttr(v["property"], dtb.ParseAnything(v["value"]));
                }
                else if (tobj == ObjType.Kind && NewAttribute.match(strippedLine))
                {
                    v = NewAttribute.getArgs();
                    ((KindPrototype)eobj).MakeAttribute((string)v["Name"], dtb.getKindAny( (string)v["kind"], false), true);

                }
                else
                {
                    
                    throw new Errors.ItemMatchException("Don't know what to do with line: " + line,
                        GlobalFunction.getLastError(),
                        PropertyDefinition.getLastError(),
                        NewAttribute.getLastError());

                }
            }
            return pStatus.Working;


        }
        
    }
    class CodeParser: IParser
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

        IndentationManager indent;
        
        /// <summary>
        /// The local variables for this function
        /// </summary>
        Dictionary<string, LocalVariable> locals=new Dictionary<string, LocalVariable>();

        int getFramePos(int varindex)
        {
            return varindex*4;
        }

        public CodeParser(Databases dtbs, IndentationManager oldIndentation)
        {
            fdtb = dtbs.fdtb;
            cdtb = dtbs.cdtb;
            sdtb = dtbs.sdtb;
            indent = new IndentationManager(oldIndentation);
            block = new CodeBlock();
        }

        private CodeParser(ClassDatabase cdtb, FunctionDatabase fdtb, StringDatabase sdtb, IndentationManager indentation, Dictionary<string, LocalVariable> loc)
        {
            this.cdtb = cdtb;
            this.sdtb = sdtb;
            this.fdtb = fdtb;
            indent = new IndentationManager(indentation);
            locals = loc;
            block = new CodeBlock();
        }

        public CodeParser(Databases databases, Function f, int lastindentation)
        {
            fdtb = databases.fdtb;
            cdtb = databases.cdtb;
            sdtb = databases.sdtb;
            indent = new IndentationManager(lastindentation);

            int offset = f is LocalFunction ? 1 : 0;

            foreach (Argument a in f.arguments)
            {
                if (!a.isSelfArgument())
                {
                    locals[a.name] = new LocalVariable(locals.Count+offset, a.type);
                }
            }

            if (f is LocalFunction) {
                Argument selfArgument = f.arguments.First((x => x.isSelfArgument()));
                locals[selfArgument.name]=new LocalVariable(0, selfArgument.type);
            }
#if DEBUG
            Console.WriteLine("Locals are: " + locals.toAdvancedString());
#endif
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
            nested = new CodeParser(cdtb, fdtb, sdtb, indent, locals);
            internalBlock.addIfRange(internalBlock.Count, internalBlock.Count);
        }

        List<string> parseParts=new List<string>();
        int parsePartsIndent=0;

        public void RegisterParsePart(string Item)
        {
            parseParts.Add(StringUtil.repeat("|", parsePartsIndent) + Item);
            parsePartsIndent++;
        }

        public void UnregisterParsePart(string Item)
        {
            parsePartsIndent -= 1;
        }

        public pStatus Parse(string line, int linenumber)
        {
            bool finishInternal = false;
            bool doEsle = false;
            
            if (nested != null)
            {
                pStatus stat= nested.Parse(line, linenumber);
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


            string shortString = indent.doIndentation(line);
            if (shortString == null)
            {
                return pStatus.Finished;
            }

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

            if (parseParts.Count > 0)
            {
                parseParts.Clear();
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

        static int numEvalConditionCalled = 0;

        private SubstitutedCondition EvalCondition(string shortString)
        {
            RegisterParsePart(shortString);

            numEvalConditionCalled++;

            List<Errors.ParsingException> lastEx=new List<Errors.ParsingException>();

            foreach (Condition cond in fdtb.globalConditions)
            {
                if (cond.Match(shortString))
                {
                    try
                    {
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
                        UnregisterParsePart(shortString);
                        return cond.toSubstitutedCondition(new ArgumentValue(addressMode.constint, substitutionType.NextElse, ClassDatabase.integer), tmpArguments.ToArray());
                    }
                    catch (Errors.ParsingException x)
                    {
                        lastEx.Add(x);
                    }
                }
            }
            if (lastEx.Count==0)
            {
                throw new Errors.UnformatableObjectException("Can't find a condition to match \"" + shortString+"\"", parseParts);
            }
            else if (lastEx.Count == 1)
            {
                throw lastEx[0];
            }
            else
            {
                throw new Errors.ErrorStack( lastEx);
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
            expression = expression.Trim(StringUtil.whitespace);

            RegisterParsePart(expression);

            if (locals.ContainsKey(expression))
            {
                UnregisterParsePart(expression);
                return new ArgumentValue(addressMode.frameint, getFramePos((int)locals[expression]), locals[expression].kind);
            }
            object b = cdtb.ParseAnything(expression);
            if (b != null)
            {
                UnregisterParsePart(expression);
                return toValue(b);
            }
            else
            {
                ArgumentValue? returnValue=null;
                List<Errors.ParsingException> lastEx = new List<Errors.ParsingException>();
                foreach (Phrase f in fdtb)
                {
                    if (f.match(expression))
                    {
                        try
                        {
                            returnValue=evalPhrase(f, placeToWriteTo, useOutput);
                            break;
                        }
                        catch (Errors.ParsingException ex)
                        {
                            lastEx.Add(ex);
                            returnValue = null;
                        }
                    }
                }
                if (returnValue == null)
                {
                    if (lastEx.Count==0)
                    {
                        throw new Errors.SyntaxError("No code way found to match \"" + expression + "\"");
                    }
                    else if (lastEx.Count == 1)
                    {
                        throw lastEx[0];
                    }
                    else
                    {
                        throw new Errors.ErrorStack( lastEx);
                    }
                }
                UnregisterParsePart(expression);
                return (ArgumentValue)returnValue;
            }
        }

        public ArgumentValue evalPhrase(Phrase matchedExpression, CodeBlock placeToWriteTo, bool useOutput)
        {
            //I store this all mods in a special block,
            //because I can't predict failure
            //and in case types arn't correct
            //I need to not do anything and return
            //so I store in tmpBlock, and add it
            //to the main block on sucess
            CodeBlock tmpBlock = new CodeBlock();
            
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
                ArgumentValue t = EvalExpression(args[v.name], tmpBlock);

                if (!v.type.isParent(t.getKind()))
                {
                    throw new Errors.TypeMismatchException("function requres a " + v.type.ToString() +
                        " but instead got incompatable type  " + t.getKind().ToString()+"("
                        +args[v.name]+ ")");
                }

                if (v.isStackArgument())
                {


                    if (t.getMode() != addressMode.stack)
                    {
                        tmpBlock.add(Phrase.push.toSubstituedPhrase(new ArgumentValue[] { t }, null));
                    }
                }
                else
                {
                    argValues.Push(t);
                }
            }
            tmpBlock.add(matchedExpression.toSubstituedPhrase(argValues, returnValue));

            placeToWriteTo.add(tmpBlock);

            return returnValue;
        }

        public CodeBlock getBlock()
        {
            block.locals = locals;
            return block;
        }

        private ArgumentValue toValue(object v)
        {
            
            if (v is int || v is I32COnvertibleWrapper)
            {
                if (v.Equals(0))
                {
                    return new ArgumentValue(addressMode.zero, ClassDatabase.integer) ;
                }
                else if (v is I32COnvertibleWrapper)
                {
                    return new ArgumentValue(addressMode.constint, (int)(I32COnvertibleWrapper)v);
                }
            }
            else if (v is string)
            {
                StringItem l = sdtb.getStr((string)v);
                return (ArgumentValue)l;
            }
            else if (v is I32Convertable)
            {
                I32Convertable b = (I32Convertable)v;
                IUnsubstitutedBytes sub = b.to32bits();
                IEnumerable<Substitution> subs = sub.substitutions;
                if (subs.isEmpty())
                {
                    return new ArgumentValue(addressMode.constint, Writer.toInt(sub.bytes.ToArray()));
                }
                else
                {
                    Substitution s = subs.First();
                    return new ArgumentValue(addressMode.constint, s.type, s.data, ClassDatabase.getKind(v));
                }

            }
            //TODO: Add options for kind and item prototypes
            throw new Errors.UnformatableObjectException("object \"" + v.ToString() + "\" of type \""+v.GetType().ToString()+" has no possible format");
        }


        internal int getNumArgs()
        {
            return locals.Count;
        }
    }

    #region structs

    interface IArgItem
    {
    }

    struct ArgItemReturnValue: IArgItem
    {
    }

    struct ArgItemFromArguments: IArgItem, IConditionArgValue
    {
        public bool noPop;

        /// <summary>
        /// Special function that can be used te decide whether to
        /// remove a argument from the arguments list at compile
        /// time.
        /// never set noPop for stack arguments, since they are
        /// allways invalidiated after a single use.
        /// </summary>
        /// <param name="noPop"></param>
        public ArgItemFromArguments(bool noPop)
        {
            this.noPop = noPop;
        }
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

        public ArgumentValue(addressMode mode, substitutionType type, int? subdata, IValueKind kind)
        {
            this.mode = mode;
            substitutionType = type;
            data = new byte[4];
            if (subdata == null)
            {
                substitutionData = 0;
            }
            else
            {
                substitutionData = (int)subdata;
            }
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

    #endregion

    class DummyParser: IParser
    {
        Stack<IndentationManager> indent;

        public DummyParser(IndentationManager oldIndent)
        {
            indent = new Stack<IndentationManager>();
            indent.Push(new IndentationManager(oldIndent));
        }

        public pStatus Parse(string line, int linenumber)
        {
            
            string s = "";
            try
            {
                IndentationManager m = indent.Peek();
                s = m.doIndentation(line);
            }
            catch (Errors.InvalidIncreaseIndent)
            {
                indent.Push(new IndentationManager(indent.Peek()));
            }
            if (s == null)
            {
                indent.Pop();
                if (indent.Count == 0)
                {
                    return pStatus.Finished;
                }
            }
            return pStatus.Working;
        }
    }

    class parsingErrorData: Errors.IErrorData
    {


        string description;
        string ShortDescription;
        string line;
        string strippedLine;
        string parserDescription;

        public parsingErrorData(string description,
            string shortDescription,
            string line,
            string strippedLine,
            string parserDescription)
        {
            this.ShortDescription = shortDescription;
            this.description = description;
            this.line = line;
            this.strippedLine = strippedLine;
            this.parserDescription = parserDescription;

        }

        public string getExpandedString()
        {
            return getTitle() + "\n" + description + "\nat: " + line.Trim(StringUtil.whitespace) + ((strippedLine != line) ? ("(" + strippedLine + ")") : "");
        }

        public string getTitle()
        {
            return "\""+parserDescription+"\": "+ShortDescription;
        }


    }
}
