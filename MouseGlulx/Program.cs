using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MouseEngine;
using MouseEngine.Lowlevel;

namespace MouseGlulx
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please supply a file");
            }
            FileStream f = new FileStream(args[0], FileMode.Open);
            BinaryReader r = new BinaryReader(f);
            byte[] header = r.ReadBytes(36);
            int size = Writer.toInt(header.readSlice(12, 4));
            Console.WriteLine(size);
            f.Seek(0,SeekOrigin.Begin);
            Reader read=new Reader(r.ReadBytes(size));
            read.start();
            f.Close();

            Console.WriteLine("press any key to continue...");
            Console.ReadKey();
        }
    }
}
