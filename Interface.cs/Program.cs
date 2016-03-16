using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MouseEngine.Lowlevel;
using MouseEngine;

namespace MouseEngineInterface
{


    class Program
    {

        static int Main(string[] args)
        {
            DateTime startTime = DateTime.Now;

            if (args.Length >= 1)
            {
                Parser parser = new Parser();
                FileStream f = new FileStream(args[0], FileMode.Open);
                
                StreamReader reader = new StreamReader(f, Encoding.UTF8);
                int linenumber = 1;
                string lastLine="";
                try
                {
                    while (!reader.EndOfStream)
                    {
                        lastLine = reader.ReadLine();
                        parser.Parse(lastLine, linenumber);

                        linenumber += 1;
                    }
                    linenumber = 1;
                    f.Seek(0, SeekOrigin.Begin);

                    parser.prepareForSecondStage();

                    while (!reader.EndOfStream)
                    {
                        lastLine = reader.ReadLine();
                        parser.Parse2(lastLine, linenumber);
                        linenumber += 1;
                    }
                }
                catch (MouseEngine.Errors.ParsingException ex)
                {
                    reader.Close();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("----------");
                    Console.WriteLine("ERROR");
                    Console.WriteLine(ex.GetType().ToString() + " occurred at line " + linenumber.ToString() + " of file " + args[0]);
                    Console.WriteLine("\t" + lastLine.TrimStart(StringUtil.whitespace));
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Stack Trace:");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(ex.StackTrace);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("----------");
                    Console.ForegroundColor = ConsoleColor.White;



                    Console.WriteLine("press enter to exit...");
                    Console.ReadLine();


                    return -1;

                }

                f.Close();
                Databases b = parser.getDatabases();
                Console.Write(b.WriteData());

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



                DateTime newTime = DateTime.Now;
                TimeSpan difference = newTime - startTime;
                Console.WriteLine("Total duration: "+difference.ToString());

            }
            else
            {
                Console.WriteLine("please specify a file to open");
            }

            Console.WriteLine("Finished, press enter to exit: ");
            Console.ReadLine();
            return 0;
        }
    }
}