using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MouseEngine.Lowlevel;

namespace MouseEngine
{
    public class Databases
    {
        internal ClassDatabase cdtb;
        internal Lowlevel.FunctionDatabase fdtb;
        internal StringDatabase sdtb;

        public static Databases getDefault()
        {
            Databases d = new Databases();
            d.fdtb = new Lowlevel.FunctionDatabase();
            d.cdtb = new ClassDatabase(d);
            d.sdtb = new StringDatabase();
            return d;
        }

        static public int ids=0;

        public string WriteData()
        {
            return cdtb.WriteData();
        }
    }

    struct Nothing
    {

    }

    class ClassDatabase
    {
        public Dictionary<string, ItemPrototype> existingObjects;
        public Dictionary<string, KindPrototype> existingTypes;
        public Dictionary<string, IValueKind> valueKinds;
        public Lowlevel.FunctionDatabase functionDatabase;
        internal Databases databases;

        public ClassDatabase(Databases dtbs)
        {
            existingTypes = new Dictionary<string, KindPrototype>();
            existingObjects = new Dictionary<string, ItemPrototype>();
            valueKinds = new Dictionary<string, IValueKind>();
            valueKinds.Add("number", integer);
            valueKinds.Add("text", str);
            functionDatabase = dtbs.fdtb;
            databases = dtbs;
            //existingTypes.Add("object", new KindPrototype("object"))
            existingTypes.Add("kind",new KindPrototype(this, "kind"));
        }
        public KindPrototype getKind(string name)
        {
            return getKind(name, false);
        }

        public IValueKind getKindAny(string name, bool defined)
        {
            name = name.Trim(StringUtil.whitespace);
            if (valueKinds.ContainsKey(name))
            {
                return valueKinds[name];
            }
            else return getKind(name, defined);
        }

        public KindPrototype getOrMakeKind(string name)
        {
            return getKind(name, true);
        }
        internal ItemPrototype getOrMakeObject(string name)
        {
            return getObject(name, true);
        }
        internal ItemPrototype getObject(string name)
        {
            return getObject(name, false);
        }
        internal ItemPrototype getObject(string name, bool defined)
        {
            name = name.Trim(StringUtil.whitespace);
            ItemPrototype newObject;
            if (existingObjects.ContainsKey(name))
            {
                newObject = existingObjects[name];
            }
            else
            {
                newObject = new ItemPrototype(this,name);
                existingObjects[name] = newObject;
            }

            if (defined)
            {
                newObject.define();
            }
            return newObject;
        }
        internal KindPrototype getKind(string name, bool defined)
        {
            name = name.Trim(StringUtil.whitespace);
            KindPrototype newObject;
            if (existingTypes.ContainsKey(name))
            {
                newObject = existingTypes[name];
            }
            else
            {
                newObject = new KindPrototype(this, name);
                existingTypes[name] = newObject;
            }
            if (defined)
            {
                Console.WriteLine("Defined object " + newObject.getName());
                newObject.define();
            }
            return newObject;
        }

        
        public static IValueKind getKind(object input)
        {
            if (input is ItemPrototype)
            {
                return ((ItemPrototype)input).getKind();
            }
            else if (input is int || input is I32COnvertibleWrapper)
            {
                return integer;
            }
            else if (input is string || input is StringItem)
            {
                return str;
            }
            else
            {
                return null;
            }
        }

        public void doIndexChecking()
        {
            bool done = false;
            while (!done)
            {
                done = true;
                foreach (KindPrototype k in existingTypes.Values)
                {
                    if (k.IDprocessingFinished)
                    {
                        //This space is intentionally empty: If processing if finished, nothing needs to be done
                    }
                    else if (k.getParent()==null || k.getParent().IDprocessingFinished == true)
                    {
                        //Only if these conditions are true are we ready to start processing.
                        int lastID;
                        if (k.getParent() == null)
                        {
                            lastID = 2;
                        }
                        else
                        {
                            lastID = k.getParent().nextID;
                        }

                        foreach (var b in k.getPossibleAttributes().Values)
                        {
                            if (b.owner== k)
                            {
                                b.pos = lastID;
                                lastID += 1;
                            }
                        }
                        k.nextID = lastID;
                        k.IDprocessingFinished = true;
                    }
                    else
                    {
                        done = false;
                    }
                }
            }
        }

        /// <summary>
        /// This function gives a basic overview of what objects are defined.
        /// </summary>
        /// <returns>the string of all the defined objects.</returns>
        public string WriteData()
        {
            StringBuilder s = new StringBuilder("BEGIN DUMP\n");
            s.AppendLine("Types defined");

            showSingleDict(s, existingTypes);

            s.AppendLine("Object defined:");


            showSingleDict(s, existingObjects);

            s.AppendLine("");


            s.AppendLine("definded a total of " + functionDatabase.Count() + " functions: ");
            foreach (Lowlevel.Phrase f in functionDatabase)
            {
                s.AppendLine("defined function: " + f.ToString());
            }

            s.AppendLine("\n");

            return s.ToString();
        }

        private void showSingleDict <T> (StringBuilder s, Dictionary<string,T> v) where T :Prototype
        {
            foreach (Prototype k in v.Values)
            {
                s.Append( "\t" + k.getName());
                if (!k.getDefined())
                {
                    s.AppendLine(" (implied)");
                }
                else
                {
                    s.AppendLine();
                }
                foreach (KeyValuePair<string, KAttribute> l in k.getPossibleAttributes())
                {
                    try
                    {
                        s.AppendLine("\t\t"+l.Key+":"+k.getAttributes()[l.Key]);
                    }
                    catch (KeyNotFoundException)
                    {
                        s.AppendLine("\t\t"+l.Key+":(undefined value of kind "+l.Value.kind.ToString()+")");
                    }
                }
            }

            
            
        }
        /// <summary>
        /// Preforms basic checking of non-expression literals, like numbers, strings and names of existing objects
        /// I am not sure if this is the class that should have this responsibility, but who cares.
        /// Returns null if no simple object could be found, in this case, you could throw an error or use
        /// codeParser.evalExpression.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public I32Convertable ParseAnything(string s)
        {
            if (existingObjects.ContainsKey(s))
            {
                return existingObjects[s];
            }
            else if (s.StartsWith("\"") && s.EndsWith("\"")){
                return databases.sdtb.getStr( s.Substring(1, s.Length - 2).substituteSlashes());
            }
            else
            {
                try
                {
                    return (new I32COnvertibleWrapper( int.Parse(s)));
                }
                catch (FormatException)
                {
                    return null;
                }
            }

        }

        public static IValueKind str = new StringValueKind();
        public static IValueKind integer = new IntValueKind();
        public static IValueKind nothing = new VoidValueKind();
        internal static IValueKind condition = new ConditionValueKind();
    }

    class I32COnvertibleWrapper: I32Convertable
    {
        public int value;

        public I32COnvertibleWrapper(int value)
        {
            this.value = value;
            I32Convertable f = (I32Convertable)this;
            //this.value = (int)f;
        }

        public IUnsubstitutedBytes to32bits()
        {
            return new UnsubstitutedBytes(Lowlevel.Writer.toBytes(value));
        }

        /*static public implicit operator int (I32COnvertibleWrapper obj)
        {
            return obj.value ;
        }*/

        static public explicit operator int (I32COnvertibleWrapper obj)
        {
            return obj.value;
        }

    }

    interface I32Convertable
    {
        /// <summary>
        /// Should return an exactly 4 byte sequence, that can be
        /// written to the file. FOr most types, this will be a referance
        /// </summary>
        /// <returns></returns>
        Lowlevel.IUnsubstitutedBytes to32bits();
    }

    class Prototype: Lowlevel.IReferable
    {
        public Prototype(ClassDatabase dtb, string name)
        {
            attributes = new Dictionary<string, I32Convertable>();
            this.name = name;
            this.dtb = dtb;
            id = Databases.ids++;
        }

        ClassDatabase dtb;



        public string getName()
        {
            return name;
        }
        public Dictionary<string, I32Convertable> getAttributes()
        {
            return attributes;
        }

        public I32Convertable getSpecificAttribute(string name)
        {
            if (attributes.ContainsKey(name))
            {
                return attributes[name];
            }
            else if (parent == null)
            {
                throw new KeyNotFoundException("Object " + ToString() + " has no attribute " + name);
            }
            else
            {
                return parent.getSpecificAttribute(name);
            }
        }

        Dictionary<string, I32Convertable> attributes;
        bool defined;
        protected string name;
        protected KindPrototype parent;
        public void setAttr(string name, I32Convertable value)
        {
            if (value == null)
            {
                throw new ArgumentException("Attempt to set a property to null");
            }
            else if (!getPossibleAttributes().ContainsKey(name))
            {
                if (this is KindPrototype)
                {
                    ((KindPrototype)this).MakeAttribute(name, ClassDatabase.getKind(value), false);
                }
                else
                {
                    parent.MakeAttribute(name, ClassDatabase.getKind(value), false);
                }
            }

            IValueKind attrKind = getPossibleAttributes()[name].kind;
            if (attrKind.isParent(ClassDatabase.getKind(value)))
            {

                attributes[name] = value;
            }
            else
            {
                throw new Errors.TypeMismatchException("Trying to assign value " + value.ToString() +
                    "of kind" + ClassDatabase.getKind(value).ToString() +
                    " to varable " + name + " which should be a " + attrKind.ToString());
            }




        }
        public void define()
        {
            defined = true;
        }
        public void setParent(KindPrototype p)
        {
            parent = p;
        }
        public KindPrototype getParent()
        {
            return parent;
        }

        public bool getDefined()
        {
            return defined;
        }
        public virtual Dictionary<string, KAttribute> getPossibleAttributes()
        {
            return parent.getPossibleAttributes();
        }
        public IValueKind getKind()
        {
            return parent;
        }

        public override string ToString()
        {
            return name;
        }

        public int getID()
        {
            return id;
        }

        WriterComponent writer;

        public WriterComponent getWriter()
        {
            return writer;
        }

        public void setWriter(WriterComponent w)
        {
            writer = w;
        }

        public IUnsubstitutedBytes to32bits()
        {
            return new UnsubstitutedBytes(new byte[] { 1, 1, 1, 1 },
                new Substitution[]
                {
                    new Substitution(0,substitutionType.WriterRef,substitutionRank.Normal,getID())
                });
        }


        int id;
    }
    class ItemPrototype: Prototype, I32Convertable
    {

        public ItemPrototype(ClassDatabase dtb, string name):base(dtb,name) {
        }

        
    }
    interface IValueKind
    {
        bool isParent(IValueKind other);

    }
    interface IValueKind<T>: IValueKind
    {

    }

    struct ConditionValueKind : IValueKind
    {
        public bool isParent(IValueKind other)
        {
            return other is ConditionValueKind;
        }
    }

    class IntValueKind: IValueKind<int?>
    {
        public bool isParent(IValueKind other)
        {
            return other==this;
        }

        public object parse(string s)
        {
            return int.Parse(s);
        }
    }
    class StringValueKind: IValueKind<string>
    {
        public bool isParent(IValueKind other)
        {
            return other==this;
        }

        public string parse(string s)
        {
            return s.Substring(1, s.Length - 2);
        }
        
    }

    class VoidValueKind : IValueKind<Nothing>
    {
        public bool isParent(IValueKind other)
        {
            return true;
        }

        public object parse(string s)
        {
            return null;
        }
    }

    class KAttribute
    {
        public string name;
        public bool defined;
        public int? pos;
        public IValueKind kind;
        public KindPrototype owner;
        internal readonly int id;

        private static int globalIds=0;

        public KAttribute (string name, IValueKind kind, bool defined, KindPrototype owner)
        {
            this.name = name;
            this.defined = defined;
            this.owner = owner;
            this.kind = kind;
            pos = null;
            id = globalIds++;
        }
    }

    class KindPrototype: Prototype, IValueKind<ItemPrototype>
    {
        //These two variables are used by class database in the ID processing process
        //ID processing stores whereter it's been done,
        //and next ID at what place the next ID should be processed.
        //Probably read the func in Class Database for info
        public bool IDprocessingFinished = false;
        public int nextID;
        bool locked;

        Databases dtbs;

        List<LocalFunction> localFunctions = new List<LocalFunction>();

        public KindPrototype(ClassDatabase dtb, string name, bool locked):base(dtb, name)
        {
            subAttributes = new Dictionary<string, KAttribute>();
            this.locked = locked;
            this.dtbs = dtb.databases;
        }

        public KindPrototype(ClassDatabase dtb, string name): this(dtb, name, false)
        {

        }

        internal void addFunction(LocalFunction f)
        {
            if (locked)
            {
                throw new Errors.ReservedWordError("Attempt to add function to reserved class " + name);
            }
            localFunctions.Add(f);
        }

        internal List<LocalFunction> getFunctions()
        {
            List<LocalFunction> Parentfuncs;
            if (this.parent != null)
            {
                Parentfuncs=(this.parent.getFunctions());
            }
            else
            {
                Parentfuncs = new List<LocalFunction>();
            }

            int lastIndex = 4+Parentfuncs.Count; //First 4 places are reserved for metadata

            for (int i = 0; i < localFunctions.Count; i++)
            {
                int id2 = -1;
                for (int j = 0; j < Parentfuncs.Count; j++)
                {
                    if (localFunctions[i].getEquivolent(Parentfuncs[j]))
                    {
                        if (id2 != -1)
                        {
                            throw new Errors.ParsingException(
                                "Somehow, two functions with same signitarue got into the parent");
                        }
                        id2 = j;
                    }
                }

                if (id2 == -1)
                {
                    localFunctions[i].localID = lastIndex;
                    lastIndex += 1;
                }
                else
                {
                    localFunctions[i].localID = Parentfuncs[id2].localID;
                    Parentfuncs.RemoveAt(id2);
                }


            }

            List<LocalFunction> final = new List<LocalFunction>();

            final.AddRange(localFunctions);
            final.AddRange(Parentfuncs);
            
            return final;
        }

        Dictionary<string, KAttribute> subAttributes;

        public void MakeAttribute(string name, IValueKind type, bool defined)
        {
            if (locked)
            {
                //THROW some kind of error
                throw new Errors.ReservedWordError("Attempting to add atribute to reserved class "+this.name);
            }
#if DEBUG
            if (type == null)
            {
                throw new NullReferenceException("Type can not be null");
            } else
#endif
            if (parent != null && parent.getPossibleAttributes().ContainsKey("name"))
            {
                throw new Errors.ItemMatchException("This attrubute is allready defined somewhere else");
            }
            if (!subAttributes.ContainsKey(name))
            {
                subAttributes[name] = new KAttribute(name, type, defined, this);
                dtbs.fdtb.addAttribute(this, type, name, subAttributes[name].id);
            }
            else if (subAttributes[name].kind == type)
            {
                subAttributes[name].defined |= defined;
            }
            else
            {
                throw new Errors.TypeMismatchException("Attempting to give attribute " + name + " of type " + ToString()
                    + " 2 kinds: " + subAttributes[name].kind.ToString() + ", and " + type.ToString() + ".");
            }
            
        }

        bool preparedForWrite = false;

        internal void prepareForWrite()
        {
            preparedForWrite = true;

            if (parent != null && !parent.preparedForWrite){
                parent.prepareForWrite();
            }

            int startPos;

            if (parent!=null)
            {
                startPos = parent.localFunctions[parent.localFunctions.Count - 1].getID();
            }
            else
            {
                startPos = 4; //The first 4 spaces are reserved, so I start at 5 (index 4)
                //See writer.TypeWriter for more info
            }

            foreach (var f in localFunctions)
            {
                f.localID=startPos;
                startPos += 1;
            }
        }

        public ItemPrototype parse(string s)
        {
            throw new NotImplementedException();
        }
        

        public override Dictionary<string, KAttribute> getPossibleAttributes()
        {
            if (parent == null)
            {
                return subAttributes;
            }
            else
            {
                return DictUtil.Combine( subAttributes,parent.getPossibleAttributes());
            }
        }

        public bool isParent(IValueKind other)
        {
            if (!(other is KindPrototype))
            {
                return false;
            }
            KindPrototype v = (KindPrototype)other;
            if (v.parent == null)
            {
                return false;
            }
            else if (v==this || v.parent==this || isParent(v.parent))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal KAttribute getAttributeByID(int data)
        {
            foreach (KAttribute k in subAttributes.Values)
            {
                if (k.id == data){
                    return k;
                }
            }

            return null;
        }
    }

}
