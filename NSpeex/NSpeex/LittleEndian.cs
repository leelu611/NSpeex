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

using System;
using System.Text;
namespace NSpeex
{
    public class LittleEndian
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="v"></param>
        public static void WriteInt(byte[] buf, int offset, int v)
        {
            byte[] data = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian)
            {
                byte tmp = data[0];
                data[0] = data[3];
                data[3] = tmp;
                tmp = data[1];
                data[1] = data[2];
                data[2] = tmp;
            }
            Array.Copy(data, 0, buf, offset, 4);
        }
        public static void WriteLong(byte[] buf, int offset, long v)
        {
            byte[] data = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian)
            {
                byte tmp = data[0];
                data[0] = data[7];
                data[7] = tmp;

                tmp = data[1];
                data[1] = data[6];
                data[6] = tmp;

                tmp = data[2];
                data[2] = data[5];
                data[5] = tmp;

                tmp = data[3];
                data[3] = data[4];
                data[4] = tmp;
            }
            Array.Copy(data, 0, buf, offset, 8);
        }
        public static void WriteString(byte[] buf, int offset, string v)
        {
            byte[] data = UTF8Encoding.UTF8.GetBytes(v);
            Array.Copy(data, 0, buf, offset, data.Length);
        }
        public static int ReadInt(byte[] buf, int offset)
        {
            byte[] data = new byte[4] { 0, 0, 0, 0 };
            Array.Copy(buf, offset, data, 0, 4);
            if (!BitConverter.IsLittleEndian)
            {
                byte tmp = data[0];
                data[0] = data[3];
                data[3] = tmp;
                tmp = data[1];
                data[1] = data[2];
                data[2] = tmp;
            }
            return BitConverter.ToInt32(data, 0);
        }
        public static long ReadLong(byte[] buf, int offset)
        {
            byte[] data = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            Array.Copy(buf, offset, data, 0, 8);
            if (!BitConverter.IsLittleEndian)
            {
                byte tmp = data[0];
                data[0] = data[7];
                data[7] = tmp;

                tmp = data[1];
                data[1] = data[6];
                data[6] = tmp;

                tmp = data[2];
                data[2] = data[5];
                data[5] = tmp;

                tmp = data[3];
                data[3] = data[4];
                data[4] = tmp;
            }
            return BitConverter.ToInt64(data, 0);
        }
        public static string ReadString(byte[] buf, int offset, int size)
        {
            return UTF8Encoding.UTF8.GetString(buf, offset, size);
        }

    }
}
