namespace NSpeex
{
    public class Bits
    {
        /// <summary>
        ///Default buffer size
        /// </summary>
        public const int DEFAULT_BUFFER_SIZE = 1024;
        /// <summary>
        ///  "raw" data
        /// </summary>
        private byte[] bytes;
        /// <summary>
        /// Position of the byte "cursor"
        /// </summary>
        private int bytePtr;
        /// <summary>
        /// Position of the bit "cursor" within the current byte
        /// </summary>
        private int bitPtr;
        /// <summary>
        /// Initialise the bit packing variables.
        /// </summary>
        public void init()
        {
            bytes = new byte[DEFAULT_BUFFER_SIZE];
            bytePtr = 0;
            bitPtr = 0;
        }
        /// <summary>
        /// Advance n bits.
        /// </summary>
        /// <param name="n">
        /// the number of bits to advance.
        /// </param>
        public void advance(int n)
        {
            bytePtr += n >> 3;
            bitPtr += n & 7;
            if (bitPtr > 7)
            {
                bitPtr -= 8;
                bytePtr++;
            }
        }
        /// <summary>
        /// Sets the buffer to the given value.
        /// </summary>
        /// <param name="newBuffer">
        /// newBuffer
        /// </param>
        public void setBuffer(byte[] newBuffer)
        {
            bytes = newBuffer;
        }
        /// <summary>
        /// Take a peek at the next bit.
        /// </summary>
        /// <returns>
        /// the next bit.
        /// </returns>
        public int peek()
        {
            return ((bytes[bytePtr] & 0xFF) >> (7 - bitPtr)) & 1;
        }
        /// <summary>
        /// Read the given array into the buffer
        /// </summary>
        /// <param name="newbytes">
        /// newbytes
        /// </param>
        /// <param name="offset">
        /// offset
        /// </param>
        /// <param name="len">
        /// len
        /// </param>
        public void read_from(byte[] newbytes, int offset, int len)
        {
            for (int i = 0; i < len; i++)
                bytes[i] = newbytes[offset + i];
            bytePtr = 0;
            bitPtr = 0;
        }
        /// <summary>
        /// Read the next N bits from the buffer.
        /// </summary>
        /// <param name="nbBits">
        /// the number of bits to read.
        /// </param>
        /// <returns>
        /// the next N bits from the buffer.
        /// </returns>
        public int unpack(int nbBits)
        {
            int d = 0;
            while (nbBits != 0)
            {
                d <<= 1;
                d |= ((bytes[bytePtr] & 0xFF) >> (7 - bitPtr)) & 1;
                bitPtr++;
                if (bitPtr == 8)
                {
                    bitPtr = 0;
                    bytePtr++;
                }
                nbBits--;
            }
            return d;
        }
        /// <summary>
        /// Write N bits of the given data to the buffer.
        /// </summary>
        /// <param name="data">
        /// the data to write.
        /// </param>
        /// <param name="nbBits">
        /// the number of bits of the data to write.
        /// </param>
        public void pack(int data, int nbBits)
        {
            int d = data;

            while (bytePtr + ((nbBits + bitPtr) >> 3) >= bytes.Length)
            {
                // System.err.println("Buffer too small to pack bits");
                /* Expand the buffer as needed. */
                int size = bytes.Length * 2;
                byte[] tmp = new byte[size];
                System.Array.Copy(bytes, 0, tmp, 0, bytes.Length);
                bytes = tmp;
            }
            while (nbBits > 0)
            {
                int bit;
                bit = (d >> (nbBits - 1)) & 1;
                bytes[bytePtr] |= (byte)(bit << (7 - bitPtr));
                bitPtr++;
                if (bitPtr == 8)
                {
                    bitPtr = 0;
                    bytePtr++;
                }
                nbBits--;
            }
        }
        /// <summary>
        /// Returns the current buffer array.
        /// </summary>
        /// <returns></returns>
        public byte[] getBuffer()
        {
            return bytes;
        }
        /// <summary>
        /// Returns the number of bytes used in the current buffer.
        /// </summary>
        /// <returns></returns>
        public int getBufferSize()
        {
            return bytePtr + (bitPtr > 0 ? 1 : 0);
        }

    }
}
