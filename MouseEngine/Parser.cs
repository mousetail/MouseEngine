using System;
using System.Collections;
using System.Collections.Generic;
using MouseEngine;
using MouseEngine.Lowlevel;


namespace MouseEngine
{
    enum pStatus: byte {Working, Finished, SyntaxError, RelationError}
    enum ObjType: byte { Object, Kind}
    enum ProcessingInternal: byte { GlobalFunction, LocalFunction }
    

        
    

    class Parser
    {
        BlockParser currentBlock;
        ClassDatabase database;
        public Parser()
        {
            database = new ClassDatabase();

            currentBlock = new BlockParser(database);
        }
        public void Parse(String line)
        {
            if (line[0] != '#' && line[0] != '/')
            {
                pStatus result = currentBlock.Parse(line);
                while (result == pStatus.Finished)
                {
                    currentBlock = new BlockParser(database);
                    result=currentBlock.Parse(line);
                }
            }
        }
        public ClassDatabase getDatabase()
        {
            return database;
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
        ClassDatabase dtb;

        int indentation;
        bool indented;

        ObjType tobj;

        Prototype eobj;

        ProcessingInternal func;
        string funcName;

        CodeParser internalParser;

        public BlockParser(ClassDatabase dtb)
        {
            started = false;
            this.dtb = dtb;
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
                        Console.WriteLine(internalParser.getBlock().ToString());
                        dtb.functionDatabase.AddGlobalFunction(internalParser.getBlock(), funcName);
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
                    eobj.setAttr((string)v["property"], dtb.ParseAnything( (string)v["value"]));
                }
                else if (tobj == ObjType.Kind && NewAttribute.match(strippedLine, dtb))
                {
                    Console.WriteLine("This is an attribute");
                    v = NewAttribute.getArgs();
                    ((KindPrototype)eobj).MakeAttribute((string)v["Name"], dtb.getKindAny( (string)v["kind"], false), true);

                }
                else if (GlobalFunction.match(strippedLine, dtb))
                {
                    internalParser = new CodeParser(dtb.functionDatabase, dtb, indentation);
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

        CodeParser nested;

        CodeBlock block;

        FunctionDatabase fdtb;

        ClassDatabase cdtb;

        int indentation;
        bool indented;
        int oldindentation;

        public CodeParser(FunctionDatabase dtb, ClassDatabase cdtb, int indentation)
        {
            fdtb = dtb;
            this.cdtb = cdtb;
            this.oldindentation = indentation;
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

            EvalExpression(line.Substring(newindentation),addressMode.zero);

            return pStatus.Working;
        }

        public void EvalExpression(string expression, addressMode outputtype)
        {

        }

        public CodeBlock getBlock()
        {
            return block;
        }
    }

    struct ArgumentValue
    {
        addressMode mode;
        byte[] data;
        substitutionType? substitutionType;
        int substitutionData;

        public ArgumentValue(addressMode mode, int value)
        {
            this.mode = mode;
            this.data = Writer.toBytes(value);
            this.substitutionType = null;
            this.substitutionData = 0;
        }

        public ArgumentValue(addressMode mode, substitutionType type)
        {
            this.mode = mode;
            substitutionType = type;
            data = null;
            substitutionData = 0;
        }

        public addressMode getMode()
        {
            return mode;
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




}
