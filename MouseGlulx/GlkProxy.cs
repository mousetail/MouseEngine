using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MouseGlulx.Glk;
using MouseEngine.Lowlevel;

namespace MouseGlulx
{
    delegate int glkFunction(params int[] args);

    class GlkProxy
    {
        GlkMain glk;
        Reader reader;

        Dictionary<glkFunctionID, glkFunctionData> glkFunctions;

        public GlkProxy(Reader r, GlkMain glkM)
        {
            reader = r;
            glk = glkM;
            initDictionaries();
        }

        public void callGlkFunc(AdressModeData[] data)
        {
            int id = data[0].read();
            int numargs = data[1].read();
            int[] args = new int[numargs];
            for (int i=0; i<args.Length; ++i)
            {
                args[i] = reader.pull();
            }
            glkFunctionData dat = getData(id);
            data[2].write(dat.call(args));
        }

        internal glkFunctionData getData(int id)
        {
            glkFunctionID fid = (glkFunctionID)id;
            return glkFunctions[fid];

        }

        void initDictionaries()
        {
            glkFunctions = new Dictionary<glkFunctionID, glkFunctionData>()
            {
                {
                    glkFunctionID.glk_window_open, new glkFunctionData(4,splitWindow)
                }
            };
        }

        int splitWindow(int[] arguments)
        {
            int bordb = arguments[1] & (int)Winmethod_Border.mask;
            int dirb = (arguments[1] & (int)Winmethod_Direction.mask);
            int typeb = arguments[1] & (int)Winmethod_Direction.mask;
            Winmethod_Direction dir;
            Winmethod_Type type;
            Winmethod_Border bord;
            if (Enum.IsDefined(typeof(Winmethod_Border), bordb))
            {
                bord = (Winmethod_Border)(bordb);
            }
            else
            {
                throw new glkTypeError("Invalid border type");
            }
            if (Enum.IsDefined(typeof(Winmethod_Direction), dirb)){
                 dir = (Winmethod_Direction)dirb;
            }
            else
            {
                throw new glkTypeError("invalid direction");
            }
            if (Enum.IsDefined(typeof(Winmethod_Type), typeb)) {
                type = (Winmethod_Type)(typeb);
            }
            else
            {
                throw new glkTypeError("Invalid window method: "+typeb.ToString());
            }

            if (!Enum.IsDefined(typeof(Wintype), arguments[3]))
            {
                throw new glkTypeError("invalid window type");
            }
            return glk.splitWindow(arguments[0], dir, type, bord, arguments[2], (Wintype)arguments[3]).id;
        }
    }

    class glkFunctionData
    {
        glkFunction func;
        int numArgs;

        public int call(params int[] args)
        {
            return func(args);
        }

        public glkFunctionData(int numargs, glkFunction function)
        {
            numArgs = numargs;
            func = function;
        }
    }
}
