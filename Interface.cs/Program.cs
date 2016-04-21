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

            if (args.Length == 1)
            {
                Console.WriteLine("starting processing of " + args[0]);

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

                    parser.finishStage2();
                }
                catch (MouseEngine.Errors.ParsingException ex)
                {


                    reader.Close();

                    DisplayError(ex, linenumber, lastLine, args[0], true);

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

        public static void DisplayError(MouseEngine.Errors.ParsingException ex, int linenumber, string lastLine, string fileName, bool ShowExtra)
        {
            if (ex == null)
            {
                Console.WriteLine("Null exception, exiting");
                return;
            }

            if (ShowExtra)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("----------");
                Console.WriteLine("ERROR");
                Console.ForegroundColor = ConsoleColor.White;
            }
            Console.WriteLine(ex.GetType().ToString() + " occurred at line " + linenumber.ToString() + " of file " + fileName);
            if (ShowExtra)
            {
                Console.WriteLine("\t" + lastLine.TrimStart(StringUtil.whitespace));
            }
            Console.WriteLine(ex.Message);
            if (ex is MouseEngine.Errors.ErrorStack)
            {
                List<MouseEngine.Errors.ParsingException> errors = ((MouseEngine.Errors.ErrorStack)ex).getErrors();
                for (int i = 0; i < errors.Count; i++)
                {
                    Console.WriteLine((i.ToString() + ": " + errors[i].GetType().ToString() + errors[i].Message).shorten(200));
                }
                

                

                DisplayError(chooseOne(errors.ToArray(),null), linenumber, lastLine, fileName, false);
            }
            else if (ex is MouseEngine.Errors.IExDataException)
            {
                Console.WriteLine(((MouseEngine.Errors.IExDataException)ex).getData().getExpandedString());
            }
            else if (ex is MouseEngine.Errors.IMultiExDataException)
            {
                var b = ((MouseEngine.Errors.IMultiExDataException)ex).getData();

                if (b == null)
                {
                    Console.WriteLine("Null reference in data, exit");
                    return;
                }

                for (int i=0; i<b.Length; i++)
                {
                    Console.WriteLine((i+1).ToString() + ": " + (b[i]!=null ? b[i].getTitle(): "Null"));
                }

                MouseEngine.Errors.IErrorData d = chooseOne(b, null);

                if (d != null)
                {
                    Console.WriteLine(d.getExpandedString());
                }
            }
            else
            {

                Console.WriteLine("Stack Trace:");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(ex.StackTrace);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("----------");
                Console.ForegroundColor = ConsoleColor.White;




            }

            if (ShowExtra)
            {
                Console.WriteLine("press enter to exit...");
                Console.ReadLine();
            }
        }

        public static T chooseOne<T>(T[] list, T defaultval)
        {
            Console.WriteLine("Enter a number to learn more, q or nothing to exit");

            bool valid = false;
            int number = -1;
            while (!valid)
            {
                string line = Console.ReadLine();
                if (line == "q" || line == "")
                {
                    valid = true;
                }
                else if (int.TryParse(line, out number))
                {
                    if (number > 0 && number <= list.Length)
                    {
                        valid = true;
                    }
                    else
                    {
                        Console.WriteLine("Out of range, try again: ");
                        valid = false;
                    }
                }
                else
                {
                    Console.WriteLine("output not recognized, try again: ");
                    valid = false;
                }
            }

            if (number > 0)
            {
                return list[number-1];
            }
            else
            {
                return defaultval;
            }

        }
    }

   
}