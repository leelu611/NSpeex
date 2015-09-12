using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NSpeex
{
    /// <summary>
    /// Abstract Class that defines an Audio File Writer.
    /// </summary>
    public abstract class AudioFileWriter
    {

        /// <summary>
        /// Closes the output file.
        /// </summary>
        /// <exception cref="System.IO.IOException">if there was an exception opening the Audio Writer.</exception>
        public abstract void Close();


        public abstract void Open(string fileName);


        public abstract void WriteHeader(String comment);

        public abstract void WritePacket(byte[] data, int offset, int len);

        /// <summary>
        /// Writes an Ogg Page Header to the given byte array.
        /// Ogg Page Header structure:
        /// <list type="bullet">
        /// <item>0 -  3: capture_pattern</item>
        /// <item>4: stream_structure_version</item>
        /// <item>5: header_type_flag</item>
        /// <item>6 - 13: absolute granule position</item>
        /// <item>14 - 17: stream serial number</item>
        /// <item>18 - 21: page sequence no</item>
        /// <item>22 - 25: page checksum</item>
        /// <item>26: page_segments</item>
        /// <item>27 -  x: segment_table</item>
        /// </list>
        /// </summary>
        /// <param name="buf">the buffer to write to.</param>
        /// <param name="offset">the from which to start writing.</param>
        /// <param name="headerType"> the header type flag,(0=normal, 2=bos: beginning of stream, 4=eos: end of stream).</param>
        /// <param name="granulepos">the absolute granule position.</param>
        /// <param name="streamSerialNumber"></param>
        /// <param name="pageCount"></param>
        /// <param name="packetCount"></param>
        /// <param name="packetSizes"></param>
        /// <returns></returns>
        public static int WriteOggPageHeader(byte[] buf, int offset, int headerType,
                                     long granulepos, int streamSerialNumber,
                                     int pageCount, int packetCount,
                                     byte[] packetSizes)
        {
            BigEndian.WriteString(buf, offset, "OggS");             //  0 -  3: capture_pattern
            buf[offset + 4] = 0;                            //       4: stream_structure_version
            buf[offset + 5] = (byte)headerType;            //       5: header_type_flag
            BigEndian.WriteLong(buf, offset + 6, granulepos);         //  6 - 13: absolute granule position
            BigEndian.WriteInt(buf, offset + 14, streamSerialNumber); // 14 - 17: stream serial number
            BigEndian.WriteInt(buf, offset + 18, pageCount);          // 18 - 21: page sequence no
            BigEndian.WriteInt(buf, offset + 22, 0);                  // 22 - 25: page checksum
            buf[offset + 26] = (byte)packetCount;          //      26: page_segments
            Array.Copy(packetSizes, 0,              // 27 -  x: segment_table
                             buf, offset + 27, packetCount);
            return packetCount + 27;
        }

        /// <summary>
        /// Builds and returns an Ogg Page Header.
        /// </summary>
        /// <param name="headerType"> the header type flag,(0=normal, 2=bos: beginning of stream, 4=eos: end of stream).</param>
        /// <param name="granulepos">the absolute granule position.</param>
        /// <param name="streamSerialNumber"></param>
        /// <param name="pageCount"></param>
        /// <param name="packetCount"></param>
        /// <param name="packetSizes"></param>
        /// <returns></returns>
        public static byte[] BuildOggPageHeader(int headerType, long granulepos,
                                         int streamSerialNumber, int pageCount,
                                         int packetCount, byte[] packetSizes)
        {
            byte[] data = new byte[packetCount + 27];
            WriteOggPageHeader(data, 0, headerType, granulepos, streamSerialNumber,
                               pageCount, packetCount, packetSizes);
            return data;
        }

        /// <summary>
        ///  Writes a Speex Header to the given byte array.
        /// Speex Header structure:
        /// <list type="bullet">
        /// <item>0 -  7: speex_string</item>
        /// <item>8 - 27: speex_version</item>
        /// <item>28 - 31: speex_version_id</item>
        /// <item>32 - 35: header_size</item>
        /// <item>36 - 39: rate</item>
        /// <item>40 - 43: mode (0=NB, 1=WB, 2=UWB)</item>
        /// <item>44 - 47: mode_bitstream_version</item>
        /// <item>48 - 51: nb_channels</item>
        /// <item>52 - 55: bitrate</item>
        /// <item>56 - 59: frame_size (NB=160, WB=320, UWB=640)</item>
        /// <item>60 - 63: vbr</item>
        /// <item>64 - 67: frames_per_packet</item>
        /// <item>68 - 71: extra_headers</item>
        /// <item>72 - 75: reserved1</item>
        /// <item>76 - 79: reserved2</item>
        /// </list>
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="sampleRate"></param>
        /// <param name="mode"></param>
        /// <param name="channels"></param>
        /// <param name="vbr"></param>
        /// <param name="nframes"></param>
        /// <returns></returns>
        public static int WriteSpeexHeader(byte[] buf, int offset, int sampleRate,
                                    int mode, int channels, bool vbr,
                                    int nframes)
        {
            BigEndian.WriteString(buf, offset, "Speex   ");    //  0 -  7: speex_string
            BigEndian.WriteString(buf, offset + 8, "speex-1.0"); //  8 - 27: speex_version
            Array.Copy(new byte[11], 0, buf, offset + 17, 11); // : speex_version (fill in up to 20 bytes)
            BigEndian.WriteInt(buf, offset + 28, 1);           // 28 - 31: speex_version_id
            BigEndian.WriteInt(buf, offset + 32, 80);          // 32 - 35: header_size
            BigEndian.WriteInt(buf, offset + 36, sampleRate);  // 36 - 39: rate
            BigEndian.WriteInt(buf, offset + 40, mode);        // 40 - 43: mode (0=NB, 1=WB, 2=UWB)
            BigEndian.WriteInt(buf, offset + 44, 4);           // 44 - 47: mode_bitstream_version
            BigEndian.WriteInt(buf, offset + 48, channels);    // 48 - 51: nb_channels
            BigEndian.WriteInt(buf, offset + 52, -1);          // 52 - 55: bitrate
            BigEndian.WriteInt(buf, offset + 56, 160 << mode); // 56 - 59: frame_size (NB=160, WB=320, UWB=640)
            BigEndian.WriteInt(buf, offset + 60, vbr ? 1 : 0);     // 60 - 63: vbr
            BigEndian.WriteInt(buf, offset + 64, nframes);     // 64 - 67: frames_per_packet
            BigEndian.WriteInt(buf, offset + 68, 0);           // 68 - 71: extra_headers
            BigEndian.WriteInt(buf, offset + 72, 0);           // 72 - 75: reserved1
            BigEndian.WriteInt(buf, offset + 76, 0);           // 76 - 79: reserved2
            return 80;
        }

        /// <summary>
        ///  Builds a Speex Header.
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="mode"></param>
        /// <param name="channels"></param>
        /// <param name="vbr"></param>
        /// <param name="nframes"></param>
        /// <returns></returns>
        public static byte[] BuildSpeexHeader(int sampleRate, int mode, int channels, bool vbr, int nframes)
        {
            byte[] data = new byte[80];
            WriteSpeexHeader(data, 0, sampleRate, mode, channels, vbr, nframes);
            return data;
        }

        /// <summary>
        /// Writes a Speex Comment to the given byte array.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static int WriteSpeexComment(byte[] buf, int offset, String comment)
        {
            int length = comment.Length;
            BigEndian.WriteInt(buf, offset, length);       // vendor comment size
            BigEndian.WriteString(buf, offset + 4, comment); // vendor comment
            BigEndian.WriteInt(buf, offset + length + 4, 0);   // user comment list length
            return length + 8;
        }

        /// <summary>
        /// Builds and returns a Speex Comment.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static byte[] BuildSpeexComment(String comment)
        {
            byte[] data = new byte[comment.Length + 8];
            WriteSpeexComment(data, 0, comment);
            return data;
        }

    }
}
