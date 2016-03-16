
using System.Collections.Generic;
//using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MouseEngine;

namespace MouseGlulx
{
    enum IOsys
    {
        Nothing=0,
        Filter=1,
        glk=3,
        fyre=20
    }

    struct IOdata
    {
        public IOsys system;
        public int rock;

    }

    partial class Reader
    {
        byte[] mainMemory;
        ListStack<byte> stack=new ListStack<byte>();
        Stack<int> functionStart=new Stack<int>();
        
        int RAMstart;

        Glk.GlkMain glk;
        GlkProxy proxy;

        IOdata ioSysData;

        //int readHead;


        /// <summary>
        /// Setting this to false exits the interpreter
        /// </summary>
        bool running;

        FunctionData currentFunctionData;

        public Reader(byte[] bytes)
        {
            mainMemory = bytes;
            glk = new Glk.GlkMain();
            proxy = new GlkProxy(this, glk);
            initializeDictionaries();
            
        }

        static int toInt(byte[] bytes)
        {
            return MouseEngine.Lowlevel.Writer.toInt(bytes);
        }

        static byte[] toBytes(int value)
        {
            return MouseEngine.Lowlevel.Writer.toBytes(value);
        }

        static byte[] toBytes(int value, bool force32)
        {
            return MouseEngine.Lowlevel.Writer.toBytes(value, true, force32);
        }

        static byte[] toBytes(int value, int size)
        {
            return MouseEngine.Lowlevel.Writer.toBytes(value, size);
        }

        public void start()
        {
            currentFunctionData = new FunctionData();

            currentFunctionData.startPos = toInt(mainMemory.readSlice(24,4));
            RAMstart = toInt(mainMemory.readSlice(8, 4));

            SetUpFunction(currentFunctionData.startPos,new int[0]);

            running = true;

            while (running)
            {
                execCode();
            }

        }

        public int parsePos
        {
            get
            {
                return currentFunctionData.parsingPosition;
            }
            set
            {
                currentFunctionData.parsingPosition = value;
            }
        }

        public void execCode()
        {
            int opcodeNumber;
            int varStart = 0;
            //Console.WriteLine("parsing at: " + parsePos.ToString());
            if (mainMemory[ parsePos] < 0x80)
            {
                opcodeNumber = mainMemory[parsePos];
                varStart = parsePos + 1;
            }
            else if (mainMemory[parsePos] < 0xC0)
            {
                opcodeNumber = toInt(mainMemory.readSlice(parsePos, 2)) - 0x8000;
                varStart = parsePos + 2;
            }
            else
            {
                opcodeNumber = toInt(mainMemory.readSlice(parsePos, 2)) - 0xc000000;
                varStart = parsePos + 4;
            }

            OpcodeData opcodeData = getOpcodeData(opcodeNumber);

            AdressModeData[] addrData = getAddrData(opcodeData, varStart, true);

            opcodeData.execute(addrData);
        }

        public void SetUpFunction(int funcIndex, int[] arguments)
        {
            currentFunctionData.startPos = funcIndex;
            functionStart.Push(stack.Count);
            int locals_len=0;
            List<byte> localFormat = new List<byte>();
            List<localVarPos> localPositions=new List<localVarPos>();

            for (int i=funcIndex+1; 
                mainMemory[i]!=0 || mainMemory[i+1]!=0;
                i+=2)//8 is start of locals pos
                                                                                // 2 blank bytes end local pos{
            {
                byte type= mainMemory[i];
                byte number = mainMemory[i + 1];
                if (type!=2 && type!=1 && type != 4) //Only even locals up to 4 are supported
                {
                    throw new ByteAligningException("Memory type has to be 2,4, or 8 bytes, is "+type.ToString("X"));
                }
                for (int k=0; k<number; k++)
                {
                    int startPos = locals_len.RoundUp(type);
                    localPositions.Add(new localVarPos(startPos, type));
                    locals_len = startPos + type;
                }
                localFormat.Add(type);
                localFormat.Add(number);

                currentFunctionData.parsingPosition = i+4;
                
            }

            currentFunctionData.localEnd = locals_len + localFormat.Count + 8;
            currentFunctionData.localStart=localFormat.Count+8;

            stack.pushRange(toBytes(currentFunctionData.localEnd));
            stack.pushRange(toBytes(currentFunctionData.localStart));
            stack.pushRange(localFormat);
            for (int i=0; i<locals_len; i++)
            {
                stack.push(0);
            }

            

            if (mainMemory[funcIndex] == 0xC0)
            {
                foreach (int i in arguments)
                {
                    stack.pushRange(toBytes(i));
                }
            }
            else if (mainMemory[funcIndex] == 0xC1)
            {
                for (int i= 0; (i < arguments.Length && i < localPositions.Count); i++)
                {
                    byte[] bytes = toBytes(arguments[i], localPositions[i].size);
                    if (bytes.Length != localPositions[i].size)
                    {
                        throw new IntOverflowException("Trying to write a big argument into small value");
                    }
                    for (int j=0; i<bytes.Length; j++)
                    {
                        stack[functionStart.Peek() + localFormat.Count+ localPositions[i].position + j] = bytes[j];
                    }
                }
            }
            else
            {
                throw new TypeCodeException("A reference to a function was made, but the type code was"+mainMemory[funcIndex].ToString("X"));
            }
            /*
            Console.WriteLine("Function parsing data:");
            foreach (var b in localPositions)
            {
                Console.Write("newvar ");
                for (int i=0; i<b.size; i++)
                {
                    Console.WriteLine("\t"+stack[functionStart.Peek()+localFormat.Count+2+b.position+i]);
                }
            }
            */
        }

        public void push(int what)
        {
            stack.pushRange(toBytes(what));
        }

        public int pull()
        {
            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = stack.pop();
            }
            return toInt(bytes);
        }


        public void setLocalVariable(int position, int value, bool force32)
        {
            setLocalVariable(position, toBytes(value, force32));
        }

        public void setLocalVariable(int position, byte[] what)
        {
            for (int i=0; i<what.Length; i++)
            {
                stack[functionStart.Peek() + currentFunctionData.localStart + position + i] = what[i];
            }
        }

        public int getLocalVariable(int position, int size)
        {
            if (size == 1)
            {
                return getLocalVariableByte(position);

            }
            else
            {
                byte[] test = new byte[size];
                for (int i=0; i< size; i++)
                {
                    test[i] = getLocalVariableByte(position + i);
                }
                return toInt(test);
            }

        }

        private byte getLocalVariableByte(int position)
        {
            return stack[functionStart.Peek() + currentFunctionData.localEnd + position];
        }
    }

    struct FunctionData
    {
        public int startPos;
        public int localStart;
        public int localEnd;

        public int parsingPosition;

        public FunctionData(int startPos, int localStart, int localEnd)
        {
            this.startPos = startPos;
            this.localStart = localStart;
            this.localEnd = localEnd;
            parsingPosition = 0;
        }

        
    }

    struct localVarPos
    {
        public int position;
        public int size;

        public localVarPos(int position, int size)
        {
            this.position = position;
            this.size = size;
            
        }
    }

    


}
