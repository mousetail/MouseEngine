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
                StreamReader reader = new StreamReader(f);
                while (!reader.EndOfStream)
                {
                    parser.Parse(reader.ReadLine());
                }
                f.Close();
                ClassDatabase b = parser.getDatabase();
                Console.Write(b.WriteData());

                Writer w = new Writer(b);
                f = new FileStream("output.ulx", FileMode.Create);
                BinaryWriter writer = new BinaryWriter(f);
                // byte[] q= w.write();
                //f.Write(q, 0, q.Length);
                w.prepare();
                writer.Write(w.write());
                writer.Close();
            }
            


            Console.ReadLine();
        }
    }
}
