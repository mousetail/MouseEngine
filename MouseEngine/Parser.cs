﻿using System;
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
    

        
    

    class Parser
    {
        BlockParser currentBlock;
        Databases databases;
        public Parser()
        {
            databases = Databases.getDefault();

            currentBlock = new BlockParser(databases);
        }
        public void Parse(String line)
        {
            if (line[0] != '#' && line[0] != '/')
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
                if (ObjectFirstLine.match(line, dtb))
                {
                    
                    v = ObjectFirstLine.getArgs();
                    eobj = dtb.getObject((string)v["Name"],true);
                    eobj.setParent(dtb.getKind((string)v["kind"],false));
                    tobj = ObjType.Object;
                    started = true;
                }
                else if (KindDefinition.match(line, dtb))
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

                if (PropertyDefinition.match(strippedLine, dtb))
                {
                    v = PropertyDefinition.getArgs();
                    eobj.setAttr(v["property"], dtb.ParseAnything(v["value"]));
                }
                else if (tobj == ObjType.Kind && NewAttribute.match(strippedLine, dtb))
                {
                    Console.WriteLine("This is an attribute");
                    v = NewAttribute.getArgs();
                    ((KindPrototype)eobj).MakeAttribute((string)v["Name"], dtb.getKindAny( (string)v["kind"], false), true);

                }
                else if (GlobalFunction.match(strippedLine, dtb))
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

        CodeParser nested;

        CodeBlock block;

        FunctionDatabase fdtb;

        ClassDatabase cdtb;

        StringDatabase sdtb;

        int indentation;
        bool indented;
        int oldindentation;

        Dictionary<string, LocalVariable> locals=new Dictionary<string, LocalVariable>();

        public CodeParser(Databases dtbs, int indentation)
        {
            fdtb = dtbs.fdtb;
            cdtb = dtbs.cdtb;
            sdtb = dtbs.sdtb;
            oldindentation = indentation;
            indented = false;
            block = new CodeBlock();

        }
        public pStatus parse(string line)
        {
            int newindentation = StringUtil.getIndentation(line);
            if (!indented && newindentation > oldindentation)
            {
                indentation = newindentation;
            }
            else if (newindentation > indentation)
            {
                return pStatus.SyntaxError;
            }
            else if (newindentation == oldindentation)
            {
                return pStatus.Finished;
            }

            string shortString = line.Substring(newindentation).Trim(' ');

            if (localVariableMathcer.match(shortString, cdtb))
            {
                Dictionary<string, string> args = localVariableMathcer.getArgs();
                block.add(Phrase.assign.toSubstituedPhrase(new[] { EvalExpression(args["expression"]),
                    new ArgumentValue(addressMode.frameint, locals.Count*4+12),
                }, null));
                locals.Add(args["name"],new LocalVariable(locals.Count,ClassDatabase.integer));
            }
            else {

                EvalExpression(shortString);
            }
            return pStatus.Working;
        }
        /// <summary>
        /// Note that this function will add phrases to the internal code block, the order iterations of this function are called
        /// matters.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public ArgumentValue EvalExpression(string expression)
        {
            if (locals.ContainsKey(expression))
            {
                return new ArgumentValue(addressMode.frameint, 12 + 4 * (int)locals[expression]);
            }
            object b = cdtb.ParseAnything(expression);
            if (b != null)
            {
                return toValue(b);
            }
            else
            {
                Phrase matchedExpression = null;
                foreach (Phrase f in fdtb)
                {
                    if (f.match(expression, cdtb))
                    {
                        matchedExpression = f;

                        break;
                    }
                }
                if (matchedExpression == null)
                {
                    throw new Errors.SyntaxError("No code way found to match \"" + expression+"\"");
                }
                Dictionary<string, string> args = matchedExpression.lastMatchArgs();
                List<ArgumentValue> argValues = new List<ArgumentValue>();
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
                            block.add(Phrase.push.toSubstituedPhrase(new ArgumentValue[] { t }, null));
                        }
                    }
                    else
                    {
                        argValues.Add(t);
                    }
                }
                argValues.Add(ArgumentValue.Push);
                if (matchedExpression.getReturnType() != null)
                {
                    block.add(matchedExpression.toSubstituedPhrase(argValues, ArgumentValue.Push));
                    return ArgumentValue.Pull;
                }
                else
                {
                    block.add(matchedExpression.toSubstituedPhrase(argValues, ArgumentValue.Zero));
                    return ArgumentValue.Zero;
                }
            }
        }

        public CodeBlock getBlock()
        {
            return block;
        }

        private ArgumentValue toValue(object v)
        {
            if (v is int)
            {
                return new ArgumentValue(addressMode.constint, (int)v);
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

    struct ArgItemFromArguments: IArgItem
    {

    }

    struct ArgumentValue: IArgItem
    {
        static public ArgumentValue Zero = new ArgumentValue(addressMode.zero);
        static public ArgumentValue Push = new ArgumentValue(addressMode.stack);
        static public ArgumentValue Pull = Push;

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

        public ArgumentValue(addressMode mode, int value)
        {
            this.mode = mode;
            this.data = Writer.toBytes(value);
            this.substitutionType = null;
            this.substitutionData = 0;
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
    }
}
