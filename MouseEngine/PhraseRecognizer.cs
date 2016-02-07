using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseEngine
{
    
    public abstract class Matcher
    {
        public static char[] splits = { ' ' };
        abstract public bool match(string s);
        

        public virtual Dictionary<string, string> getArgs()
        {
            return null;
        }
        [Obsolete("the class database argument is no longer required")]
        internal bool match(string s, ClassDatabase cdtb)
        {
            return match(s);
        }
    }
    
    /// <summary>
    /// Returns false every time
    /// </summary>
    class VoidMather : Matcher
    {
        public override bool match(string s)
        {
            return false;
        }
    }


    /// <summary>
    /// Matches a literal text
    /// </summary>
    class StringMatcher : Matcher
    {
        public string Text;
        public StringMatcher(string what)
        {
            Text = what;
        }
        public override bool match(string s)
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

    
    class orMatcher : Matcher
    {
        Matcher[] m;
        Matcher selected;
        public orMatcher(params Matcher[] ms)
        {
            m = ms;
        }
        public override bool match(string s)
        {
            selected = null;
            foreach (Matcher f in m)
            {
                if (f.match(s))
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

    

    public struct Range: IComparable<Range>
    {
        public int start;
        public int end;
        public Range(int start, int end)
        {
            this.start = start;
            this.end = end;
#if DEBUG
            if (start> end)
            {
                throw new IndexOutOfRangeException("start can't be more than end");
            }
#endif
        }

        public bool intersects(Range comp)
        {
            return (start >= comp.start && start <= comp.end) ||
                    (comp.start >= start && comp.start<= end);
        }

        public bool intersects(int other)
        {
            return (start <= other && other <= end);
        }

        public int CompareTo(Range other)
        {
            return start.CompareTo(other.start);
        }

        public int length
        {
            get
            {
                return end - start;
            }
        }

    }

    public class MultiStringMatcher: Matcher
    {
        string[] segments;
        string[] args;
        string[] argsnames;
        
        public MultiStringMatcher(string[] Argsnames, params string[] args)
        {
            argsnames = Argsnames;
            segments = args;
            if (argsnames.Length != args.Length - 1)
            {
                throw new Errors.OpcodeFormatError("inequal ar names and argument fields");
            }
            
        }

        public override bool match(string str)
        {
            if (segments.Length == 0)
            {
                return true;
            }

            if (str.StartsWith("(") && str.EndsWith(")"))
            {
                str = str.Substring(1, str.Length - 2);
            }

            else if (!str.StartsWith(segments[0],StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
            
            //This code finds the parts inside parethesis that won't be scanned, because
            //They are enclosed in parenthesis

            List<Range> protectedParts=new List<Range>();
            int pnesting = 0;
            int pstartpos = 0;
            for (int i=0; i<str.Length; i++)
            {
                if (str[i] == '(')
                {
                    pnesting += 1;
                    if (pnesting == 1) //It was 0 before
                    {
                        pstartpos = i;
                    }
                }
                else if (str[i]==')')
                {
                    pnesting -= 1;
                    if (pnesting == 0)
                    {
                        protectedParts.Add(new Range(pstartpos, i));
                    }
                }
            }


            string[] stringparts = new string[protectedParts.Count + 1];
            pstartpos = 0;
            for (int i=0; i<stringparts.Length-1; i++)
            {
                stringparts[i] = str.Substring(pstartpos, protectedParts[i].start);
                pstartpos = protectedParts[i].end;
            }
            stringparts[stringparts.Length-1] = str.Substring(pstartpos);
//Works till here
            List<Range> argumentPos = new List<Range>();

            int currentIndex = 0;
            bool going=true;
            int firstMatch=0;
            int lastpos = 0;
            int strlength = 0;
            for (int i=0; i < stringparts.Length; i++)
            {
                going = true;
                while (going){
                    if (stringparts[i].Length < lastpos - strlength)
                    {
                        firstMatch = -1;
                    }
                    else if (currentIndex==segments.Length-1 && segments[currentIndex]=="")
                    {
                        firstMatch = str.Length - strlength;
                    }
                    else
                    {
                        firstMatch = stringparts[i].IndexOf(segments[currentIndex], Math.Max(0,  lastpos-strlength), StringComparison.CurrentCultureIgnoreCase);
                    }
                    if (firstMatch < 0)
                    {
                        going = false;
                    }
                    else
                    {
                        if (currentIndex == 0)
                        {

                        }
                        else {
                            argumentPos.Add(new Range(lastpos, strlength+firstMatch));
                        }

                        lastpos = strlength + firstMatch + segments[currentIndex].Length;

#if DEBUG
                        if (segments[currentIndex].Length!=0 && stringparts[i][lastpos -strlength - 1] != segments[currentIndex][segments[currentIndex].Length - 1])
                        {
                            throw new IndexOutOfRangeException("whatever");
                        }
#endif

                        if (lastpos-strlength >= stringparts[i].Length)
                        {
                            going = false;
                        }



                        currentIndex += 1;
                        if (currentIndex == segments.Length || currentIndex>=str.Length)
                        {
                            goto loopend;
                        }


                    }

                }
                if (i < protectedParts.Count)
                {
                    strlength += stringparts[i].Length + protectedParts[i].length;
                }
            }

            loopend:

            if (currentIndex != segments.Length)
            {
                return false;
            }

            args = new string[argumentPos.Count];

            for (int i=0; i < argumentPos.Count; i++)
            {
                args[i] = str.Substring(argumentPos[i].start, argumentPos[i].end - argumentPos[i].start);
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

