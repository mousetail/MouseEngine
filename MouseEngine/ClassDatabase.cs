using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public ClassDatabase(Databases dtbs)
        {
            existingTypes = new Dictionary<string, KindPrototype>();
            existingObjects = new Dictionary<string, ItemPrototype>();
            valueKinds = new Dictionary<string, IValueKind>();
            valueKinds.Add("number", integer);
            valueKinds.Add("text", str);
            functionDatabase = dtbs.fdtb;
            //existingTypes.Add("object", new KindPrototype("object"));
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
            else if (input is int)
            {
                return integer;
            }
            else if (input is string)
            {
                return str;
            }
            else
            {
                return null;
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
                foreach (KeyValuePair<string, IValueKind> l in k.getPossibleAttributes())
                {
                    try
                    {
                        s.AppendLine("\t\t"+l.Key+":"+k.getAttributes()[l.Key]);
                    }
                    catch (KeyNotFoundException)
                    {
                        s.AppendLine("\t\t"+l.Key+":(undefined value of kind "+l.Value.ToString()+")");
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
        public object ParseAnything(string s)
        {
            if (existingObjects.ContainsKey(s))
            {
                return existingObjects[s];
            }
            else if (s.StartsWith("\"") && s.EndsWith("\"")){
                return s.Substring(1, s.Length - 2).substituteSlashes();
            }
            else
            {
                try
                {
                    return (int.Parse(s));
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
    class Prototype
    {
        public Prototype(ClassDatabase dtb, string name)
        {
            attributes = new Dictionary<string, object>();
            this.name = name;
            this.dtb = dtb;
        }

        ClassDatabase dtb;



        public string getName()
        {
            return name;
        }
        public Dictionary<string, object> getAttributes()
        {
            return attributes;
        }

        Dictionary<string, object> attributes;
        bool defined;
        protected string name;
        protected KindPrototype parent;
        public void setAttr(string name, object value)
        {
            if (value == null)
            {
                throw new ArgumentException("Attempt to set a property to null");
            }
            else if (!getPossibleAttributes().ContainsKey(name))
            {
                if (this is KindPrototype)
                {
                    ((KindPrototype)this).MakeAttribute(name,ClassDatabase.getKind(value),false);
                }
                else
                {
                    parent.MakeAttribute(name, ClassDatabase.getKind(value), false);
                }
            }
            

            attributes[name] = value;
            
        }
        public void define()
        {
            defined = true;
        }
        public void setParent(KindPrototype p)
        {
            parent = p;
        }
        public bool getDefined()
        {
            return defined;
        }
        public virtual Dictionary<string, IValueKind> getPossibleAttributes()
        {
            return parent.getPossibleAttributes();
        }
        public IValueKind getKind()
        {
            return parent;
        }
    }
    class ItemPrototype: Prototype
    {
        public ItemPrototype(ClassDatabase dtb, string name):base(dtb,name) { }

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

    class KindPrototype: Prototype, IValueKind<ItemPrototype>
    {
        public KindPrototype(ClassDatabase dtb, string name):base(dtb, name)
        {
            subAttributes = new Dictionary<string, IValueKind>();
            definedAttributes = new Dictionary<string, bool>();
        }

        Dictionary<string, IValueKind> subAttributes;
        Dictionary<string, bool> definedAttributes;

        public void MakeAttribute(string name, IValueKind type, bool defined)
        {
            subAttributes[name] = type;
            if (defined)
            {
                definedAttributes[name] = true;
            }
            else
            {
                if (!definedAttributes.ContainsKey(name))
                {
                    definedAttributes[name] = false;
                } 
            }
        }

        public ItemPrototype parse(string s)
        {
            throw new NotImplementedException();
        }
        

        public override Dictionary<string, IValueKind> getPossibleAttributes()
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
    }

}
