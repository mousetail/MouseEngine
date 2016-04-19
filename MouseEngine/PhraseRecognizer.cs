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

        internal abstract parsingErrorData getLastError();
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

        internal override parsingErrorData getLastError()
        {
            return null;
        }
    }


    /// <summary>
    /// Matches a literal text
    /// </summary>
    class StringMatcher : Matcher
    {
        public parsingErrorData lastErroer;

        public string Text;
        public StringMatcher(string what)
        {
            Text = what;
        }
        public override bool match(string s)
        {
            bool result = s.Trim(StringUtil.whitespace).Equals(Text);
            if (result)
            {
                //Debugger.Log(1, "parser", ", sucsess");
            }
            else
            {
                lastErroer = new parsingErrorData(
                   "String doesn't match stripped version",
                   "No Match",
                   s,
                   s.Trim(StringUtil.whitespace),
                   Text
                   );
                //Debugger.Log(1, "parser", ", failure");
            }
            return result;

        }

        internal override parsingErrorData getLastError()
        {
            return lastErroer;
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

        internal override parsingErrorData getLastError()
        {
            return selected.getLastError();
        }
    }

    
    /// <summary>
    /// A struct to represent a section in a string or array.
    /// It includes both end points. For a zero length array,
    /// end should be one less than start.
    /// </summary>
    public struct Range: IComparable<Range>
    {
        public int start;
        public int end;
        public Range(int start, int end)
        {
            this.start = start;
            this.end = end;
#if DEBUG
            if (length<0)
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
                return end - start + 1;
            }
        }

        public override string ToString()
        {
            return "Range (" + start.ToString() + "-" + end.ToString() + ")";
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
            System.Globalization.CultureInfo f = new System.Globalization.CultureInfo("en-us");
            System.Threading.Thread.CurrentThread.CurrentCulture = f;
            
        }

        public override bool match(string str)
        {

            if (segments.Length == 0)
            {
                return true;
            }

            /*else if (!str.StartsWith(segments[0],StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }*/

            //This code finds the parts inside parethesis that won't be scanned, because
            //They are enclosed in parenthesis

            Range[] protectedParts = StringUtil.getUnprotectedParts(str, true).ToArray().ToArray();
            string[] stringparts = StringUtil.getInsideStrings(protectedParts, str);
            if ((!stringparts[0].StartsWith(segments[0], StringComparison.CurrentCultureIgnoreCase)))
            {
                lastError = new parsingErrorData(
                    "The first segment is not the beginning of the string",
                    "beggining doens't match",
                    str,
                    stringparts.toAdvancedString(),
                    ToString());

                return false;
            }
            if (!stringparts[stringparts.Length - 1].EndsWith(segments[segments.Length-1],StringComparison.CurrentCultureIgnoreCase))
            {
                lastError = new parsingErrorData(
                    "The last segment is not the end of the string",
                    "end doens't match",
                    str,
                    stringparts.toAdvancedString(),
                    ToString());

                return false;
            }
            List<Range> argumentPos = new List<Range>();


            int currentIndex = 0;
            bool going=true;
            int firstMatch=0;
            int lastpos = 0;
            int strlength;
            for (int i=0; i < stringparts.Length; i++)
            {
                strlength = protectedParts[i].start;
                going = true;
                while (going){
                    if (stringparts[i].Length < lastpos - strlength)
                    {
                        firstMatch = -1;
                    }
                    else if (currentIndex==segments.Length-1 && segments[currentIndex]=="")
                    {
                        if (i < stringparts.Length - 1)
                        {
                            firstMatch = -1;
                        }
                        else
                        {
                            firstMatch = protectedParts[i].end + 1 - strlength;
                        }
                    }
                    else
                    {
                        firstMatch = stringparts[i].IndexOf(segments[currentIndex], Math.Max(0,  lastpos-strlength), StringComparison.CurrentCultureIgnoreCase);
                    }
                    if (firstMatch < 0)
                    {
                        going = false;

                        lastError = new parsingErrorData("Can not find segment " + segments[currentIndex] + " in the string",
                            "Can't find segment",
                            str,
                            stringparts.toAdvancedString(),
                            ToString());
                    }
                    else
                    {
                        if (currentIndex == 0)
                        {

                        }
                        else {
                            argumentPos.Add(new Range(lastpos, strlength+firstMatch-1));
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
                /*if (i+1 < protectedParts.Length)
                {
                    strlength = protectedParts[i+1].start;
                }*/
            }

            loopend:

            if (currentIndex != segments.Length)
            {
                lastError = new parsingErrorData("String extends after last segment", "String too long",
                    str,
                    stringparts.toAdvancedString(),

                    ToString()
                    );

                return false;
            }

            args = StringUtil.getInsideStrings(argumentPos.ToArray(), str).ToArray();
            
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

        parsingErrorData lastError;

        internal override parsingErrorData getLastError()
        {
            return lastError;
        }
    }
    
}

