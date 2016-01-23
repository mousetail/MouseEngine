using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseEngine
{
    
    abstract class Matcher
    {
        public static char[] splits = { ' ' };
        abstract public bool match(string s, ClassDatabase dtb);
        public virtual Dictionary<string, string> getArgs()
        {
            return null;
        }
    }

    class SeqMatcher : Matcher
    {

        Matcher[] prob;
        /*
        public SeqMatcher(Matcher[] prob)
        {
            this.prob = prob;
        }
         */
        public SeqMatcher(params Matcher[] prob)
        {
            this.prob = prob;
        }

        public override bool match(string s, ClassDatabase db)
        {
            string[] words = s.Split(Matcher.splits);

            bool suc = true;
            if (words.Length < prob.Length)
            {
                return false;
            }
            int istart = 0;
            int iend = words.Length;
            int j = 0;
            string[] xwords;
            while (istart < words.Length)
            {
                suc = false;
                while (iend > istart)
                {
                    xwords = ListUtil.slice(words, istart, iend);
                    if (prob[j].match(string.Join(" ", xwords),db))
                    {
                        suc = true;
                        istart = iend;
                        iend = words.Length;
                        j += 1;
                        if (j >= prob.Length && istart < words.Length)
                        {
                            //Console.WriteLine("I understood you untill " + words[istart]);
                            return false;
                        }
                        break;
                    }
                    else
                    {
                        iend -= 1;
                    }
                }

                if (!suc)
                {
                    //Debugger.Log(1, "parser", "failure due to iend>words.length");
                    return false;
                }

            }
            if (j == prob.Length)
            {
                return true;
            }
            else
            {
                //Debugger.Log(1, "parser", "failure due to prob <= j");
                return false;
            }
        }
        public override Dictionary<string, string> getArgs()
        {
            Dictionary<string, string> a = new Dictionary<string, string>();
            foreach (Matcher m in prob)
            {
                Dictionary<string, string> dict = m.getArgs();
                if (dict != null)
                {
                    foreach (var KeyValuePair in dict)
                    {
                        a.Add(KeyValuePair.Key, KeyValuePair.Value);
                    }
                }
            }
            return a;
        }
    }

    /// <summary>
    /// Returns false every time
    /// </summary>
    class VoidMather : Matcher
    {
        public override bool match(string s, ClassDatabase db)
        {
            return false;
        }
    }

    class StringMatcher : Matcher
    {
        public string Text;
        public StringMatcher(string what)
        {
            Text = what;
        }
        public override bool match(string s, ClassDatabase db)
        {
            bool result = s.Equals(Text);
            if (result)
            {
                //Debugger.Log(1, "parser", ", sucsess");
            }
            else
            {
                //Debugger.Log(1, "parser", ", failure");
            }
            return result;

        }
    }
    class StringAsVar : Matcher
    {
        string varname;
        string value;
        public StringAsVar(string varname)
        {
            this.varname = varname;
        }

        public StringAsVar()
        {
            varname = "Text";
        }

        public override bool match(string s, ClassDatabase db)
        {
            value = s;
            return true;
        }
        public override Dictionary<string, string> getArgs()
        {
            return new Dictionary<string, string> { { varname, value } };
        }
    }
    /*
    class DirectionMatcher : Matcher
    {
        string name;
        direction d;

        public DirectionMatcher(string n)
        {
            name = n;
        }

        public override bool match(string s, World g)
        {
            switch (s)
            {
                case "n":
                case "north":
                    d = direction.North;
                    return true;
                case "e":
                case "east":
                    d = direction.East;
                    return true;
                case "s":
                case "south":
                    d = direction.South;
                    return true;
                case "w":
                case "west":
                    d = direction.West;
                    return true;
                case "in":
                    d = direction.In;
                    return true;
                case "out":
                    d = direction.Out;
                    return true;
                case "u":
                case "up":
                    d = direction.Up;
                    return true;
                case "d":
                case "down":
                    d = direction.Down;
                    return true;
                case "northeast":
                case "ne":
                    d = direction.Northeast;
                    return true;
                case "northwest":
                case "nw":
                    d = direction.Northwest;
                    return true;
                case "sw":
                case "southwest":
                    d = direction.Southwest;
                    return true;
                case "se":
                case "southeast":
                    d = direction.Southeast;
                    return true;
                default:
                    return false;
            }
        }
        public override Dictionary<string, string> getArgs()
        {
            return new Dictionary<string, string>() { { name, d } };
        }
    }
    */
    class orMatcher : Matcher
    {
        Matcher[] m;
        Matcher selected;
        public orMatcher(params Matcher[] ms)
        {
            m = ms;
        }
        public override bool match(string s, ClassDatabase db)
        {
            selected = null;
            foreach (Matcher f in m)
            {
                if (f.match(s,db))
                {
                    selected = f;
                    return true;
                }
            }
            return false;
        }
        public override Dictionary<string, string> getArgs()
        {
            return selected.getArgs();
        }
    }

    /*
    class NewNameMatcher : Matcher
    {
        string name;
        ClassDatabase dtb;
        public NewNameMatcher()
        {
            
        }
        public override bool match(string s, ClassDatabase database)
        {
            if (3 < s.Length && s.Length < 8)
            {
                name = s;
                this.dtb = database;
                return true;
            }
            else
            {
                return false;
            }
        }
        public override Dictionary<string, string> getArgs()
        {
            return new Dictionary<string, string> { { "object", dtb.getOrMakeObject(name) } };
        }
    }*/
    class KindMatcher : Matcher
    {
        string name;
        public override bool match(string s, ClassDatabase dtb)
        {
            name = s;
            return true;
        }
        public override Dictionary<string, string> getArgs()
        {
            return base.getArgs();
        }
    }
    class AttributeMatcher: StringAsVar
    {
        public override bool match(string s, ClassDatabase db)
        {
            if (4 <= s.Length && s.Length <= 8)
            {
                return base.match(s, db);
            }
            return false;
        }
    }
    class MultiStringMatcher: Matcher
    {
        string[] segments;
        string[] args;
        string[] argsnames;

        public MultiStringMatcher(params string[] args)
        {
            segments = args;
            this.args = new string[segments.Length - 1];
        }
        public MultiStringMatcher(string[] Argsnames, params string[] args):this(args)
        {
            argsnames = Argsnames;
        }

        public override bool match(string str, ClassDatabase dtb)
        {
            if (segments.Length == 0)
            {
                return true;
            }
            else if (!str.StartsWith(segments[0],StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
            int lastpos = segments[0].Length;
            int currentIndex = -1;
            int segmentstart = 0;
            int segmentEnd = lastpos;
            for (int i=1; i<segments.Length; i++)
            {
                if (segments[i] != "") {
                    currentIndex = str.IndexOf(segments[i], lastpos, StringComparison.CurrentCultureIgnoreCase);
                    if (currentIndex < lastpos)
                    {/*
                        Console.Write("Can't find \"");
                        Console.Write(segments[i]);
                        Console.Write("\" in \"");
                        Console.Write(str);
                        Console.Write("\"index: ");
                        Console.Write(currentIndex);
                        Console.Write("max index: ");
                        Console.WriteLine(lastpos);*/
                        return false;
                    }
                }
                else
                {
                    currentIndex = str.Length;
                }
                segmentstart = lastpos;
                segmentEnd = currentIndex;
                string tmp = str.Substring(segmentstart, segmentEnd-segmentstart);
                args[i - 1] = tmp;
                lastpos = segmentEnd + segments[i].Length;
                /*
                Console.Write("Found phrase \"");
                Console.Write(segments[i]);
                Console.Write("\" inbetween area: ");
                Console.Write(tmp);
                Console.WriteLine();
                */
            }
            return true;
            
        }
        public override Dictionary<string, string> getArgs()
        {
            if (argsnames != null && argsnames.Length == args.Length)
            {
                Dictionary<string, string> rtrn = new Dictionary<string, string>();
                for (int i=0; i<args.Length; i++)
                {
                    rtrn[argsnames[i]] = args[i];
                }
                return rtrn;
            }
            else
            {
                throw new ArgumentException("You can't do that!");
            }
        }
        public override string ToString()
        {
            string tmp = "";
            foreach (string s in segments)
            {
                tmp += s;
                tmp += "[something]";
            }
            return tmp.Substring(0, tmp.Length - 11);
        }
    }
    
}

