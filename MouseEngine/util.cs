using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace MouseEngine
{
    /*static class ListUtil
    {
        public static T[] slice<T>(T[] l, int from, int to)
        {
            T[] r = new T[to - from];
            for (int i = from; i < to; i++)
            {
                r[i - from] = l[i];
            }
            return r;
        }
    }*/

    static public class ArrayUtil
    {
        public static void WriteSlice<T>(this T[] arr, int start, int length, T[] replacement)
        {
            for (int i = 0; i < length; i++)
            {
                arr[i + start] = replacement[i];
            }
        }
        public static void WriteSlice<t>(this t[] arr, int start, t[] replacement)
        {
            for (int i=0; i < replacement.Length; i++)
            {
                arr[i + start] = replacement[i];
            }
        }

        public static void WriteSlice<T>(this List<T> arr, int start, T[] replacement)
        {
            for (int i = 0; i < replacement.Length; i++)
            {
                arr[i + start] = replacement[i];
            }
        }

        public static T[] readSlice<T>(this T[] arr, int start, int length)
        {
            T[] output = new T[length];
            for (int i=0; i<length; i++)
            {
                output[i] = arr[i + start];
            }
            return output;
        }
        public static T[] readSlice<T>(this List<T> arr, int start, int length)
        {
            T[] output = new T[length];
            for (int i = 0; i < length; i++)
            {
                output[i] = arr[i + start];
            }
            return output;
        }

        public static t[] Combine<t>(this t[] a1, t[] a2)
        {
            t[] tmp = new t[a1.Length + a2.Length];
            for (int i = 0; i < a1.Length; i++)
            {
                tmp[i] = a1[i];
            }
            for (int i = 0; i < a2.Length; i++)
            {
                tmp[i + a1.Length] = a2[i];
            }
            return tmp;
        }

        public static bool isEmpty<T>(this IEnumerable<T> list)
        {
            IEnumerator l = list.GetEnumerator();
            return !(l.MoveNext());
        }

        public static byte[] toBytes(this int[] arr)
        {
            byte[] tmp = new byte[arr.Length * 4];
            for (int i = 0; i < arr.Length; i++)
            {
                tmp.WriteSlice(i * 4, 4, Lowlevel.Writer.toBytes(arr[i]));
            }
            return tmp;
        }

        

        public static List<Range> getRangeInverse(this IEnumerable<Range> input, int lenght)
        {
            return getRangeInverse(input, 0, lenght, true);
        }

        public static List<Range> getRangeInverse(this IEnumerable<Range> input, int start, int lenght, bool removeUnused)
        {
            List<Range> output=new List<Range>();
            foreach (Range r in input)
            {
                if (!removeUnused || start < r.start)
                    output.Add(new Range(start, r.start - 1));
                start = r.end+1;
            }
            if (start < lenght || !removeUnused)
            {
                output.Add(new Range(start, lenght-1));
            }
            return output;
        }

        public static string toAdvancedString<T>(this T[] arr)
        {
            StringBuilder b = new StringBuilder();
            foreach (T k in arr)
            {
                if (k != null)
                {
                    b.Append(k.ToString());
                }
                else
                {
                    b.Append("Null");
                }
                b.Append(", ");
            }
            return b.ToString();
        }

    }

    public static class DictUtil
    {
        public static void CombineInPlace<T1, T2>(Dictionary<T1, T2> a, Dictionary<T1, T2> b)
        {
            foreach (KeyValuePair<T1, T2> pair in b)
            {
                a.Add(pair.Key, pair.Value);
            }
        }
        public static Dictionary<t1, t2> Combine<t1, t2>(Dictionary<t1, t2> a, Dictionary<t1, t2> b)
        {
            Dictionary<t1, t2> tmp = new Dictionary<t1, t2>(a);
            CombineInPlace(tmp, b);
            return tmp;
        }
        public static void SayDict<T, T2>(Dictionary<T, T2> what)
        {
            foreach (KeyValuePair<T, T2> theta in what)
            {
                Console.Write(theta.Key);
                Console.Write(" is ");
                if ((!(theta.Value is string)) && theta.Value is IEnumerable)
                {
                    foreach (object b in (IEnumerable)theta.Value)
                    {
                        Console.Write("\"");
                        Console.Write(b);
                        Console.Write("\",");
                    }
                }
                else
                {
                    Console.WriteLine(theta.Value);
                }
            }
        }

        public static string toAdvancedString<T1,T2>(this Dictionary<T1,T2> input)
        {
            string s = "{";
            foreach (KeyValuePair<T1,T2> pair in input)
            {
                s += pair.Key.ToString() + ": " + pair.Value.ToString() + ", ";
            }
            s += "}";
            return s;
        }
    }

    static public class StringUtil
    {
        public static char[] whitespace = new char[] { ' ', '\t' };

        public static int getIndentation(string input)
        {
            int indentation = 0;
            while (whitespace.Contains(input[indentation])){
                indentation += 1;
            }
            return indentation;
        }

        static Dictionary<string, string> replacements = new Dictionary<string, string>()
        {
            {"\\n","\n" },
            {"\\\\","\\" },
            {"\\t","\t" }
        };
        
        public static string substituteSlashes(this string input)
        {
            foreach (KeyValuePair<string,string> rep in replacements)
            {
                input = input.Replace(rep.Key, rep.Value);
            }
            return input;
        }

        internal static bool isBlank(string line)
        {
            for (int i=0; i<line.Length; i++)
            {
                if (!whitespace.Contains(line[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static List<Range> getProtectedParts(string str)
        {
            return getProtectionData(str, false).ProtectedParts;
        }

        public static List<Range> getUnprotectedParts(string str)
        {
            return getProtectionData(str, false).unprotectedParts;
        }

        public static List<Range> getProtectedParts(string str, bool reduce)
        {
            return getProtectionData(str, reduce).ProtectedParts;
        }

        public static List<Range> getUnprotectedParts(string str, bool reduce)
        {
            return getProtectionData(str, reduce).unprotectedParts;
        }

        struct protectedPartsData
        {
            public List<Range> ProtectedParts;
            public List<Range> unprotectedParts;
            public Range stripped;
        }

        private static protectedPartsData getProtectionData(string str, bool reduce)
        {
            List<Range> unprotectedParts = new List<Range>();
            

            int minNesting = 6;
            int pnesting = 0;
            int pstartpos = 0;

            int extraoffsetr = 1;
            int extraoffsetl = 0;

            do
            {
                pnesting = 0;
                minNesting = 6;


                while (whitespace.Contains(str[extraoffsetl]))
                {
                    extraoffsetl += 1;
                }

                while (whitespace.Contains(str[str.Length - extraoffsetr]))
                {
                    extraoffsetr += 1;
                }


                pstartpos = extraoffsetl;

                for (int i = extraoffsetl; i <= str.Length-extraoffsetr; i++)
                {
                    if (str[i] == '(')
                    {
                        if (pnesting == 0)
                        {
                            unprotectedParts.Add(new Range(pstartpos, i-1));
                        }
                        pnesting += 1;
                    }
                    else if (str[i] == ')')
                    {
                        pnesting -= 1;

                        if (pnesting == 0)
                        {
                            pstartpos = i+1;
                        }
                        else if (pnesting < 0)
                        {
                            throw new Errors.SyntaxError("Someting went wrong with your parenthesees in " + str);
                        }


                        if (pnesting < minNesting && i != str.Length - extraoffsetr)
                        {
                            minNesting = pnesting;
                        }
                    }
                    else if (i == extraoffsetl)
                    {
                        minNesting = 0;
                    }
                }
                if (pnesting != 0)
                {
                    throw new Errors.SyntaxError("You probably missed a set of parenthsis in the string \"" + str + "\"");
                }

                if (minNesting != 0 && reduce)
                {
                    if (extraoffsetl >= str.Length)
                    {
                        return new protectedPartsData()
                        {
                            unprotectedParts = new List<Range>()
                            {
                                new Range(0,str.Length-1)
                            },
                            ProtectedParts = new List<Range>(),
                            stripped = new Range(0, str.Length - 1)
                        };
                    }

                    unprotectedParts.Clear();

                    if (str[extraoffsetl] != '(')
                    {
                        throw new Errors.OpcodeFormatError("Internal error: str[extraoffset left] should be (, is "+str[extraoffsetl]);
                    }

                    if (str[str.Length-extraoffsetr] != ')')
                    {
                        throw new Errors.OpcodeFormatError("internal error2");
                    }

                    extraoffsetr += 1;
                    extraoffsetl += 1;
                }
                else
                {
                    unprotectedParts.Add(new Range(pstartpos, str.Length-extraoffsetr));
                }
            }
            while (reduce && minNesting!=0);

            var protect = unprotectedParts.getRangeInverse(extraoffsetl, extraoffsetr, true);

            return new protectedPartsData()
            {
                ProtectedParts = protect,
                unprotectedParts = unprotectedParts,
                stripped = new Range(extraoffsetl, str.Length - extraoffsetr)

            };

        }

        public static string[] getInsideStrings(Range[] protectedParts, string str)
        {
            string[] stringparts = new string[protectedParts.Length];
            for (int i = 0; i < protectedParts.Length; i++)
            {
                stringparts[i] = str.Substring(protectedParts[i].start, protectedParts[i].length);
            }
            return stringparts;
        }

        /// <summary>
        /// Makes sure a string is less than maxlength, and inserts
        /// a "..." at the end if it isn't. It also removes breaking
        /// like newlines and tabs.
        /// </summary>
        /// <param name="item">the string to be shortened</param>
        /// <param name="maxlenght">the maximum length the item should be.</param>
        /// <returns></returns>
        public static string shorten(this string item, int maxlenght)
        {
            item = item.Replace("\n", "\\n").Replace("\r","\\r").Replace("\t","\\t");
            
            if (item.Length <= maxlenght)
            {
                return item;
            }
            else
            {
                return item.Substring(0,maxlenght - 3) + "...";
            }
        }

        internal static string repeat(string v, int parsePartsIndent)
        {
            StringBuilder b = new StringBuilder();
            for (int i=0; i<parsePartsIndent; i++)
            {
                b.Append(v);
            }
            return b.ToString();
        }
    }
    static public class NumUtil
    {
        public static int RoundUp(this int value, int rto)
        {
            return rto * ((value + rto-1) / rto);
        }
    }

    
}

