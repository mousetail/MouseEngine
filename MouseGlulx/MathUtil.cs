using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MouseGlulx
{
    class MathUtil
    {
        static public void add(AdressModeData[] values)
        {
            values[2].write(values[0].read() + values[1].read());
        }

        static public void substract(AdressModeData[] values)
        {
            values[2].write(values[0].read() - values[1].read());
        }

        static public void multiply(AdressModeData[] values)
        {
            values[2].write(values[0].read() * values[1].read());
        }

        static public void devide(AdressModeData[] values)
        {
            values[2].write(values[0].read() / values[1].read());
        }

        static public void negate(AdressModeData[] values)
        {
            values[1].write(-values[0].read());
        }

        static public void modulus(AdressModeData[] values)
        {
            values[2].write(values[0].read() % values[1].read());
        }

        static public void bitand(AdressModeData[] values)
        {
            values[2].write(values[0].read() & values[1].read());
        }

        static public void bitor(AdressModeData[] values)
        {
            values[2].write(values[0].read() | values[1].read());
        }

        static public void bitxor(AdressModeData[] values)
        {
            values[2].write(values[0].read() ^ values[1].read());
        }

        static public void bitnot(AdressModeData[] values)
        {
            values[1].write(values[0].read()^-1);
        }

        static public void bitshiftl(AdressModeData[] values)
        {
            values[2].write(values[0].read() << values[1].read());
        }

        static public void bitshiftr(AdressModeData[] values)
        {
            values[2].write(values[0].read() >> values[1].read());
        }

        static public void copy(AdressModeData[] values)
        {
            values[1].write(values[0].read());
        }
    }

    class ErrorRaiser
    {
        Exception error;

        public ErrorRaiser(Exception error)
        {
            this.error = error;

        }

        public void raise(byte[] rock, int value)
        {
            throw error;
        }

        public static explicit operator AddressModeOutputFunction(ErrorRaiser value)
        {
            return value.raise;
        } 
    }
}
