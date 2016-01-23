using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseEngine
{
    class ClassDatabase
    {
        public Dictionary<string, ItemPrototype> existingObjects;
        public Dictionary<string, KindPrototype> existingTypes;
        public Dictionary<string, IValueKind> valueKinds;
        public Lowlevel.FunctionDatabase functionDatabase;

        public ClassDatabase()
        {
            existingTypes = new Dictionary<string, KindPrototype>();
            existingObjects = new Dictionary<string, ItemPrototype>();
            valueKinds = new Dictionary<string, IValueKind>();
            valueKinds.Add("number", str);
            valueKinds.Add("text", integer);
            functionDatabase = new Lowlevel.FunctionDatabase();
            //existingTypes.Add("object", new KindPrototype("object"));
        }
        public KindPrototype getKind(string name)
        {
            return getKind(name, false);
        }

        public IValueKind getKindAny(string name, bool defined)
        {
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
            string s = "BEGIN DUMP";
            s += "\nTypes defined:";

            s+=showSingleDict(this.existingTypes);

            s += "\nObject defined:";


            s+=showSingleDict(this.existingObjects);

            s += "\n";

            foreach (Lowlevel.Phrase f in functionDatabase)
            {
                s += "\ndefined function: " + f.ToString();
            }

            return s;
        }

        private string showSingleDict <T> (Dictionary<string,T> v) where T :Prototype
        {
            string s="\n";
            foreach (Prototype k in v.Values)
            {
                s += "\n\t" + k.getName();
                if (!k.getDefined())
                {
                    s += " (implied)";
                }
                foreach (KeyValuePair<string, IValueKind> l in k.getPossibleAttributes())
                {
                    try
                    {
                        s+="\n\t\t"+l.Key+":"+k.getAttributes()[l.Key];
                    }
                    catch (KeyNotFoundException)
                    {
                        s += "\n\t\t"+l.Key+":(undefined value of kind "+l.Value.ToString()+")";
                    }
                }
            }

            

            return s;
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
                Console.WriteLine("\""+s+"\" has been interpreted as a existing object");
                return existingObjects[s];
            }
            else if (s.StartsWith("\"") && s.EndsWith("\"")){
                Console.WriteLine(s + " has been interpreted as a string");
                return s.Substring(1, s.Length - 2);
            }
            else
            {
                try
                {
                    Console.WriteLine("\"" + s + "\" has been interpreted as a int");
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

            Console.WriteLine(value.GetType() + " has been assigned");

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
        object parse(string s);

    }
    interface IValueKind<T>: IValueKind
    {

    }

    class IntValueKind: IValueKind<int?>
    {
        public object parse(string s)
        {
            return int.Parse(s);
        }
    }
    class StringValueKind: IValueKind<string>
    {
        public string parse(string s)
        {
            return s.Substring(1, s.Length - 2);
        }

        object IValueKind.parse(string s)
        {
            return parse(s);
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

        object IValueKind.parse(string s)
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
    }
}
