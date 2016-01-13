using System;
using System.Collections.Generic;
using System.Collections;

namespace MouseEngine
{
    static class ListUtil
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
    }

    static class ArrayUtil
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

        public static byte[] toBytes(this int[] arr)
        {
            byte[] tmp = new byte[arr.Length * 4];
            for (int i = 0; i < arr.Length; i++)
            {
                tmp.WriteSlice(i * 4, 4, Lowlevel.Writer.toBytes(arr[i]));
            }
            return tmp;
        }
    }

    static class DictUtil
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
    }

    static class StringUtil
    {
        static List<char> whitespace = new List<char> () { ' ', '\t' };

        public static int getIndentation(string input)
        {
            int indentation = 0;
            while (whitespace.Contains(input[indentation])){
                indentation += 1;
            }
            return indentation;
        }
    }
    static class NumUtil
    {
        public static int RoundUp(this int value, int rto)
        {
            return rto * ((value + rto-1) / rto);
        }
    }
}

