using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseGlulx.Glk
{
    enum Winmethod_Direction
    {
        Left = 0x0,
        right = 0x1,
        Above = 0x2,
        below = 0x3,
        mask = 0x04,
    }

    enum Winmethod_Type
    {
        abolute = 0x10,
        porportional = 0x20,
        mask = 0xf0

    }

    enum Winmethod_Border
    {
        border = 0x000,
        noborder = 0x100,
        mask = 0xf00
    }

    enum Wintype
    {
        alltypes = 0x0,
        pair = 0x1,
        blank = 0x2,
        textBuffer = 0x3,
        textGrid = 0x4,
        Graphics = 0x5

    }

    class GlkMain
    {
        Dictionary<int, glkOpaque> opaqueObjects;
        Random rand = new Random();

        GlkWindow baseWindow;

        const int width=49;
        const int height=169;
        

        public GlkMain()
        {
            Console.WindowHeight = width;
            Console.WindowWidth = height;
            
        }

        public void register(glkOpaque obj)
        {
            while (opaqueObjects.ContainsKey(obj.id) && opaqueObjects[obj.id]!=obj)
            {
                obj.id = rand.Next();
            }
            opaqueObjects[obj.id] = obj;
            obj.glk = this;
        }

        public T getOpaqueObject<T>(int id) where T : glkOpaque
        {
            glkOpaque d = opaqueObjects[id];
            if (d is T)
            {
                return (T)d;
            }
            else
            {
                throw new glkTypeError("Attemting to get a opaque of type " + typeof(T).ToString() + "but got " + d.GetType().ToString());
            }
        }

        public GlkWindow splitWindow(int window, Winmethod_Direction direction, Winmethod_Type method, Winmethod_Border border, int amount, Wintype type)
        {
            if (window == 0)
            {
                if (baseWindow != null)
                {
                    throw new GlkError("Attempting to split a zero window when existing window exists");
                }

                GlkWindow wint = new GlkWindow(0, 0, width, height, false);
                
                GlkWindow neww = wint.split(direction, Winmethod_Type.porportional, border, 100, type, false);
                baseWindow = neww;
                neww.clearParent();
                register(neww);
                return neww;
            }
            else
            {
                GlkWindow baseW = getOpaqueObject<GlkWindow>(window);
                return baseW.split(direction, method, border, amount, type, true);
            }
        }
    }

    class glkOpaque
    {
        internal GlkMain glk;

        protected int identifyer;
        public int id
        {
            get
            {
                return identifyer;
            }
            internal set
            {
                identifyer = value;
            }
        }
    }

    class GlkWindow : glkOpaque {
        SplitParentWindow parent;

        protected int width;
        protected int height;
        protected int x;
        protected int y;
        internal GlkWindow(int x, int y, int width, int height, bool porportional) {
            this.x = x;
            this.y = y;

            if (porportional)
            {
                setPorportionalSize(width, height);
            }
            else
            {

                this.width = width;
                this.height = height;
            }
        }

        public void clearParent()
        {
            parent = null;
        }

        protected virtual void setPorportionalSize(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        internal GlkWindow split(Winmethod_Direction direction, Winmethod_Type method,
            Winmethod_Border border, int amount, Wintype type, bool register)
        {
            SplitParentWindow nbase = new SplitParentWindow(x, y, width, height);
            GlkWindow newWindow;

            bool poportional = (method == Winmethod_Type.porportional);

            int nwidth;
            int nheight;

            int owidth;
            int oheight;

            int ox=0, oy=0, nx=0, ny = 0;

            if (direction==Winmethod_Direction.Above || direction == Winmethod_Direction.below)
            {
                nwidth = width;
                owidth = width;
                if (poportional)
                {
                    nheight = (height * amount) / 100;
                }
                else
                {
                    nheight = amount;
                }
                
            }
            else
            {
                nheight = height;
                oheight = height;
                if (poportional)
                {
                    nwidth = (width * amount) / 100;
                }
                else
                {
                    nwidth= amount;
                }
            }

            switch (type) {
                case Wintype.blank:
                case Wintype.Graphics:
                    newWindow = new blankWindow(0, 0, nwidth, nheight, poportional);
                    break;
                case Wintype.textBuffer:
                case Wintype.textGrid:
                    newWindow = new textWindow(0, 0, nwidth, nheight, poportional);
                    break;
                default:
                    newWindow = null;
                    break;
            }

            nwidth = newWindow.width; //The window size may have been calculated differently
            nheight = newWindow.height;
            owidth = width- nwidth;
            oheight = height -nheight;


            if (direction == Winmethod_Direction.Left)
            {
                ox = nwidth;
            }
            else if (direction == Winmethod_Direction.Above) {
                oy = nheight;
            }
            else if (direction == Winmethod_Direction.right)
            {
                nx = width - nwidth;
                 }
            else
            {
                ny = height - nheight;
            }

            x = ox;
            y = oy;
            width = owidth;
            height = oheight;

            newWindow.x = nx;
            newWindow.y = ny;

            nbase.parent = parent;
            parent = nbase;
            newWindow.parent = nbase;

            if (register)
            {
                glk.register(nbase);
                glk.register(newWindow);
            }
            return newWindow;


        }
    }

    class blankWindow: GlkWindow{
        public blankWindow(int x, int y, int width, int height, bool porportional):base(x,y,width,height, porportional)
        {

        }

        protected override void setPorportionalSize(int width, int height)
        {
            width = 0;
            height = 0;
        }
    }

    class textWindow: GlkWindow
    {
        public textWindow(int x, int y, int width, int height, bool porportional):base(x,y,width,height, porportional)
        {

        }
    }

    class SplitParentWindow: GlkWindow
    {
        public SplitParentWindow(int x, int y, int width, int height):base(x,y,width,height,false)
        {
            
        }
    }
}
