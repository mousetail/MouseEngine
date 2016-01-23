using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MouseEngine.Lowlevel;

namespace MouseEngine
{
    

    class Program
    {
        
        static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                Parser parser = new Parser();
                FileStream f = new FileStream(args[0],FileMode.Open);
                StreamReader reader = new StreamReader(f,Encoding.UTF8);
                while (!reader.EndOfStream)
                {
                    parser.Parse(reader.ReadLine());
                }
                f.Close();
                Databases b = parser.getDatabases();
                Console.Write(b.cdtb.WriteData());
                
                Writer w = new Writer(b);
                Console.WriteLine("Opening file...");
                f = new FileStream("output.ulx", FileMode.Create);
                BinaryWriter writer = new BinaryWriter(f);
                // byte[] q= w.write();
                //f.Write(q, 0, q.Length);
                Console.WriteLine("Preparing...");
                w.prepare();
                Console.WriteLine("Writing...");
                writer.Write(w.write());
                writer.Close();
            }

            Console.WriteLine(FileMode.Create.ToString());

            Console.WriteLine("Finished, press enter to exitஇ");
            Console.ReadLine();
        }
    }
}
