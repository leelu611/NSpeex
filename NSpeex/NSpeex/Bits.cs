/*
  
   Copyright [2014] [alking of copyright morln]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License. 

 */

namespace NSpeex
{
    /// <summary>
    /// Speex bit packing and unpacking class.
    /// </summary>
    public class Bits
    {
        /// <summary>
        ///Default buffer size
        /// </summary>
        public const int DefaultBufferSize = 1024;
        /// <summary>
        ///  "raw" data
        /// </summary>
        private byte[] _bytes;
        /// <summary>
        /// Position of the byte "cursor"
        /// </summary>
        private int _bytePtr;
        /// <summary>
        /// Position of the bit "cursor" within the current byte
        /// </summary>
        private int _bitPtr;
        /// <summary>
        /// Initialise the bit packing variables.
        /// </summary>
        public void Init()
        {
            _bytes = new byte[DefaultBufferSize];
            _bytePtr = 0;
            _bitPtr = 0;
        }
        /// <summary>
        /// Advance n bits.
        /// </summary>
        /// <param name="n">
        /// the number of bits to advance.
        /// </param>
        public void Advance(int n)
        {
            _bytePtr += n >> 3;
            _bitPtr += n & 7;
            if (_bitPtr > 7)
            {
                _bitPtr -= 8;
                _bytePtr++;
            }
        }
 
        /// <summary>
        /// Take a Peek at the next bit.
        /// </summary>
        /// <returns>
        /// the next bit.
        /// </returns>
        public int Peek()
        {
            return ((_bytes[_bytePtr] & 0xFF) >> (7 - _bitPtr)) & 1;
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
        public void ReadFrom(byte[] newbytes, int offset, int len)
        {
            for (int i = 0; i < len; i++)
                _bytes[i] = newbytes[offset + i];
            _bytePtr = 0;
            _bitPtr = 0;
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
        public int UnPack(int nbBits)
        {
            int d = 0;
            while (nbBits != 0)
            {
                d <<= 1;
                d |= ((_bytes[_bytePtr] & 0xFF) >> (7 - _bitPtr)) & 1;
                _bitPtr++;
                if (_bitPtr == 8)
                {
                    _bitPtr = 0;
                    _bytePtr++;
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
        public void Pack(int data, int nbBits)
        {
            int d = data;

            while (_bytePtr + ((nbBits + _bitPtr) >> 3) >= _bytes.Length)
            {
                int size = _bytes.Length * 2;
                byte[] tmp = new byte[size];
                System.Array.Copy(_bytes, 0, tmp, 0, _bytes.Length);
                _bytes = tmp;
            }
            while (nbBits > 0)
            {
                int bit;
                bit = (d >> (nbBits - 1)) & 1;
                _bytes[_bytePtr] |= (byte)(bit << (7 - _bitPtr));
                _bitPtr++;
                if (_bitPtr == 8)
                {
                    _bitPtr = 0;
                    _bytePtr++;
                }
                nbBits--;
            }
        }

        /// <summary>
        /// get and set current buffer array
        /// </summary>
        public byte[] Buffer
        {
            get { return _bytes; }
            set { _bytes = value; }
        }
        /// <summary>
        /// Returns the number of bytes used in the current buffer.
        /// </summary>
        public int BufferSize
        {
            get
            {
                return _bytePtr + (_bitPtr > 0 ? 1 : 0);
            }
        }

    }
}
