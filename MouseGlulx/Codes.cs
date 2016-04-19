
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MouseEngine;
using MouseEngine.Lowlevel;

namespace MouseGlulx
{
    partial class Reader
    {

        Dictionary<addressMode, AdressModeBase> addrData;
        Dictionary<opcodeType, OpcodeData> opcdData;

        private void initializeDictionaries()
        {
            addrData = new Dictionary<addressMode, AdressModeBase>()
            {
                //Simple ardess modes
                { addressMode.zero, new AdressModeBase(addressMode.zero,0, doNothing,(rock => 0))},
                {addressMode.stack, new AdressModeBase(addressMode.stack,0,(rock, value) => push(value), (rock)=>pull(true)) },
                //Constant modes
                {addressMode.constint, new AdressModeBase(addressMode.constint,4, new ErrorRaiser(new GlulxTypeError("attempt to use output constant")).raise, toInt) },
                {addressMode.constshort, new AdressModeBase(addressMode.constshort,2, new ErrorRaiser(new GlulxTypeError("attempt to use output constant")).raise, toInt) },
                {addressMode.constbyte, new AdressModeBase(addressMode.constbyte,1, new ErrorRaiser(new GlulxTypeError("attempt to use output constant")).raise, toInt) },
                //Stack Local frame Modes
                {addressMode.frameint, new AdressModeBase(addressMode.frameint,4,writeFrame,readFrame) },
                {addressMode.frameshort, new AdressModeBase(addressMode.frameshort,2, writeFrame,readFrame) },
                {addressMode.framebyte, new AdressModeBase(addressMode.framebyte,1,writeFrame,readFrame) }
            };

            opcdData = new Dictionary<opcodeType, OpcodeData>()
            {
                //Do nothing
                {opcodeType.nop, new OpcodeData(opcodeType.nop, 0, 0, doNothing2) },
                //Math operators
                {opcodeType.add, new OpcodeData(opcodeType.add, 2, 1, MathUtil.add) },
                {opcodeType.sub, new OpcodeData(opcodeType.sub, 2, 1, MathUtil.substract) },
                {opcodeType.mul, new OpcodeData(opcodeType.mul, 2, 1, MathUtil.multiply) },
                {opcodeType.div, new OpcodeData(opcodeType.div, 2, 1, MathUtil.devide) },
                {opcodeType.neg, new OpcodeData(opcodeType.neg, 1,1, MathUtil.negate) },
                {opcodeType.mod, new OpcodeData(opcodeType.mod, 2, 1, MathUtil.modulus) },
                {opcodeType.copy, new OpcodeData(opcodeType.copy, 1,1,MathUtil.copy) },

                //IO operators
                {opcodeType.glk, new OpcodeData(opcodeType.glk, 2, 1, proxy.callGlkFunc) },
                {opcodeType.setiosys, new OpcodeData(opcodeType.setiosys, 2,0,setIOSystem) }
            };

        }

        OpcodeData getOpcodeData(int type)
        {
            return getOpcodeData((opcodeType)type);
        }

        OpcodeData getOpcodeData(opcodeType type)
        {
            return opcdData[type];
        }

        AdressModeData[] getAddrData(OpcodeData data, int startPos, bool setParsePos)
        {
            int inputv = data.numInVariables;
            int outputv = data.numOutVariables;
            AdressModeBase[] output = new AdressModeBase[inputv + outputv];
            int parseIndex=0;

            for (int k = 0; k < (inputv + outputv); k++) //I is set elsewhere
            {

                parseIndex = (k) / 2;
                byte value;
                if (k % 2 == 0)
                {
                    value = (byte)(mainMemory[parseIndex + startPos]%16);
                }
                else
                {
                    value = (byte)(mainMemory[parseIndex + startPos] / 16);
                }
                output[k] = getAddrBase(value);
            }

            parseIndex = (inputv + outputv + 1) / 2;

            List<byte>[] values = new List<byte>[output.Length];
            for (int k=0; k<values.Length; k++)
            {
                values[k] = new List<byte>();
            }
            int j=0;
            foreach (AdressModeBase addrb in output)
            {
                for (int k=0; k < addrb.numBytes; k++) {
                    values[j].Add(mainMemory[parseIndex + startPos]);
                    parseIndex += 1;
                }
                j += 1;
            }

            AdressModeData[] newOutput = new AdressModeData[output.Length];
            for (int k=0; k<output.Length; k++)
            {
                newOutput[k] = new AdressModeData(output[k], values[k].ToArray(), k >= inputv);
            }

            if (setParsePos)
            {
                parsePos = parseIndex + startPos;
            }

            return newOutput;
        }


        AdressModeBase getAddrBase(byte value)
        {

            return addrData[(addressMode)value];

        }

        void doNothing(byte[] rock, int value)
        {

        }

        void doNothing2(AdressModeData[] data)
        {

        }

        int readFrame(byte[] rock)
        {
            return toInt( ArrayUtil.readSlice<byte>(stack, currentFunctionData.localStart + toInt(rock), 4));
        }

        void writeFrame(byte[] rock, int value)
        {
            ArrayUtil.WriteSlice(stack, currentFunctionData.localStart + toInt(rock), toBytes(value));
        }

        void setIOSystem(AdressModeData[] values)
        {
            ioSysData.system = (IOsys)values[0].read();
            ioSysData.rock = values[1].read();
        }
    }

    class OpcodeData
    {
        opcodeType type;
        public int numInVariables;
        public int numOutVariables;
        OpcodeExecuter executer;

        public OpcodeData(opcodeType type, int numInVariables, int numOutVariables, OpcodeExecuter executer)
        {
            this.type = type;
            this.numInVariables = numInVariables;
            this.numOutVariables = numOutVariables;
            this.executer = executer;
        }

        public void execute(AdressModeData[] data)
        {
            executer(data);
        }
    }

    /// <summary>
    /// This function can be called from inside a event handler to take it's output.
    /// Should only be called once.
    /// </summary>
    /// <param name="rock">The </param>
    /// <param name="value"></param>
    delegate void AddressModeOutputFunction(byte[] rock, int value);
    delegate int AddressModeInputFunction(byte[] rock);
    delegate void OpcodeExecuter(AdressModeData[] inputVars);

    class AdressModeBase
    {
        public addressMode mode;
        public int numBytes;

        public AddressModeOutputFunction outputF;
        public AddressModeInputFunction inputF;

        public AdressModeBase(addressMode mode, int numBytes, AddressModeOutputFunction outputF, AddressModeInputFunction inputF)
        {
            this.mode = mode;
            this.numBytes = numBytes;
            this.inputF = inputF;
            this.outputF = outputF;
        }
    }



    class AdressModeData
    {
        public bool isOutput;
        AdressModeBase parent;
        byte[] rock;

        public override string ToString()
        {
            return mode.ToString() + (isOutput ? "(output)" : "(input)");
        }

        public AdressModeData(AdressModeBase parent, byte[] rock, bool isOutput)
        {
            this.isOutput = isOutput;
            this.parent = parent;
            this.rock = rock;
        }

        public addressMode mode
        {
            get
            {
                return parent.mode;
            }
        }

        public int numBytes
        {
            get
            {
                return parent.numBytes;
            }
        }

        public void write(int value)
        {
            if (isOutput)
            {
                parent.outputF(rock, value);
                return;
            }
            throw new InternalError("Attemtping to write to a input var");
        }
        public int read()
        {
            if (!isOutput)
            {
                return parent.inputF(rock);
            }
            throw new InternalError("Attempting to read from a output var");
        }

        
    }
    

}
