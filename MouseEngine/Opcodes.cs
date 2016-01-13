namespace MouseEngine.Lowlevel
{
    enum opcodeType : int
    {
        nop = 0x00,
        add = 0x10,
        sub = 0x11,
        mul = 0x12,
        div = 0x13,
        mod = 0x14,
        neg = 0x15,
        bitand = 0x18,
        bitor = 0x19,
        bitxor = 0x1A,
        bitnot = 0x1B,
        shiftl = 0x1C,
        sshiftr = 0x1D,
        ushiftr = 0x1E,
        jump = 0x20,
        jz = 0x22,
        jnz = 0x23,
        jeq = 0x24,
        jne = 0x25,
        jlt = 0x26,
        jge = 0x27,
        jgt = 0x28,
        jle = 0x29,
        jltu = 0x2A,
        jgeu = 0x2B,
        jgtu = 0x2C,
        jleu = 0x2D,
        call = 0x30,
        returnf = 0x31,
        catchf = 0x32,
        throwf = 0x33,
        tailcall = 0x34,
        copy = 0x40,
        copys = 0x41,
        copyb = 0x42,
        sexs = 0x44,
        sexb = 0x45,
        aload = 0x48,
        aloads = 0x49,
        aloadb = 0x4A,
        aloadbit = 0x4B,
        astore = 0x4C,
        astores = 0x4D,
        astoreb = 0x4E,
        astorebit = 0x4F,
        stkcount = 0x50,
        stkpeek = 0x51,
        stkswap = 0x52,
        stkroll = 0x53,
        stkcopy = 0x54,
        streamchar = 0x70,
        streamnum = 0x71,
        streamstr = 0x72,
        streamunichar = 0x73,
        gestalt = 0x100,
        debugtrap = 0x101,
        getmemsize = 0x102,
        setmemsize = 0x103,
        jumpabs = 0x104,
        random = 0x110,
        setrandom = 0x111,
        quit = 0x120,
        verify = 0x121,
        restart = 0x122,
        save = 0x123,
        restore = 0x124,
        saveundo = 0x125,
        restoreundo = 0x126,
        protect = 0x127,
        glk = 0x130,
        getstringtbl = 0x140,
        setstringtbl = 0x141,
        getiosys = 0x148,
        setiosys = 0x149,
        linearsearch = 0x150,
        binarysearch = 0x151,
        linkedsearch = 0x152,
        callf = 0x160,
        callfi = 0x161,
        callfii = 0x162,
        callfiii = 0x163,
        mzero = 0x170,
        mcopy = 0x171,
        malloc = 0x178,
        mfree = 0x179,
        accelfunc = 0x180,
        accelparam = 0x181,
        numtof = 0x190,
        ftonumz = 0x191,
        ftonumn = 0x192,
        ceil = 0x198,
        floor = 0x199,
        fadd = 0x1A0,
        fsub = 0x1A1,
        fmul = 0x1A2,
        fdiv = 0x1A3,
        fmod = 0x1A4,
        sqrt = 0x1A8,
        exp = 0x1A9,
        log = 0x1AA,
        pow = 0x1AB,
        sin = 0x1B0,
        cos = 0x1B1,
        tan = 0x1B2,
        asin = 0x1B3,
        acos = 0x1B4,
        atan = 0x1B5,
        atan2 = 0x1B6,
        jfeq = 0x1C0,
        jfne = 0x1C1,
        jflt = 0x1C2,
        jfle = 0x1C3,
        jfgt = 0x1C4,
        jfge = 0x1C5,
        jisnan = 0x1C8,
        jisinf = 0x1C9
    }

    enum addressMode: byte
    {
        zero=0x0,
        constbyte=0x1,
        constshort=0x2,
        constint=0x3,
        unused=0x4,
        addrbyte=0x5,
        addrshort=0x6,
        addrint=0x7,
        stack=0x8,
        framebyte=0x9,
        frameshort=0xA,
        frameint=0xB,
        unused2=0xC,
        rambyte=0xD,
        ramshort=0xE,
        ramint=0xF

    }

    static class OpcodeTools
    {
        public static byte[] toBytes(this opcodeType d)
        {
            int f = (int)d;
            if (f <= 0x7F)
            {
                return new byte[] { (byte)f };
            }
            else if (f <= 0x3FFF)
            {
                byte[] b = Writer.toBytes(f);
                return new byte[2] { (byte)(b[2] + 0x80), b[3] };
            }
            else
            {
                byte[] b = Writer.toBytes(f);
                return new byte[4] { (byte)(b[0] + 0xC0), b[1], b[2], b[3] };
            }
        }
        //public static byte[] tooBytes
    }
}