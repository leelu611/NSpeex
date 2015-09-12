using System;

namespace NSpeex
{
    /// <summary>
    /// Main Speex Encoder class.
    /// </summary>
    public  class SpeexEncoder
    {
        public const string Version = "Java Speex Encoder v0.9.7 ($Revision: 1.6 $)";
        
        private IEncoder encoder;
        private Bits bits;
        private float[] rawData;
        private int sampleRate;
        private int channels;
        private int frameSize;
        public SpeexEncoder()
        {
            bits = new Bits();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode">
        /// the mode of the encoder (0=NB, 1=WB, 2=UWB).
        /// </param>
        /// <param name="quality">
        /// the quality setting of the encoder (between 0 and 10).
        /// </param>
        /// <param name="sampleRate">
        /// the number of samples per second.
        /// </param>
        /// <param name="channels">
        ///  the number of audio channels (1=mono, 2=stereo).
        /// </param>
        /// <returns></returns>
        public bool init(int mode, int quality, int sampleRate, int channels)
        {
            switch (mode)
            {
                case 0:
                    encoder = new NbEncoder();
                    ((NbEncoder)encoder).nbinit();
                    break;
                //Wideband
                case 1:
                    encoder = new SbEncoder();
                    ((SbEncoder)encoder).wbinit();
                    break;
                case 2:
                    encoder = new SbEncoder();
                    ((SbEncoder)encoder).uwbinit();
                    break;
                //*/
                default:
                    return false;
            }
            /* initialize the speex decoder */
//            encoder.setQuality(quality);
            encoder.Quality = quality;

            /* set decoder format and properties */
            this.frameSize = encoder.FrameSize;
            this.sampleRate = sampleRate;
            this.channels = channels;
            rawData = new float[channels * frameSize];

            bits.Init();
            return true;

        }

        public IEncoder getEncoder()
        {
            return encoder;
        }

        public int getSampleRate()
        {
            return sampleRate;
        }

        public int getChannels()
        {
            return channels;
        }

        public int getFrameSize()
        {
            return frameSize;
        }

        /// <summary>
        /// Pull the decoded data out into a byte array at the given offset
        /// and returns the number of bytes of encoded data just read.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns>
        /// the number of bytes of encoded data just read.
        /// </returns>
        public int getProcessedData(byte[] data, int offset)
        {
            int size = bits.BufferSize;
            Array.Copy(bits.Buffer, 0, data, offset, size);
            bits.Init();
            return size;
        }

        /// <summary>
        /// Returns the number of bytes of encoded data ready to be read.
        /// </summary>
        /// <returns></returns>
        public int getProcessedDataByteSize()
        {
            return bits.BufferSize;
        }


        /// <summary>
        /// Encode an array of shorts.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="numShorts"></param>
        /// <returns>
        /// true if successful.
        /// </returns>
        public bool processData(short[] data, int offset, int numShorts)
        {
            int numSamplesRequired = channels * frameSize;
            if (numShorts != numSamplesRequired)
            {
                throw new Exception("SpeexEncoder requires " + numSamplesRequired + " samples to process a Frame, not " + numShorts);
            }
            for (int i = 0; i < numShorts; i++)
            {
                rawData[i] = (float)data[offset + i];
            }
            return processData(rawData, numShorts);

        }


        /// <summary>
        /// Encode an array of floats.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="numSamples"></param>
        /// <returns>
        /// true if successful.
        /// </returns>
        public bool processData(float[] data, int numSamples)
        {
            int numSamplesRequired = channels * frameSize;
            if (numSamples != numSamplesRequired)
            {
                throw new Exception("SpeexEncoder requires " + numSamplesRequired + " samples to process a Frame, not " + numSamples);
            }
            if (channels == 2)
            {
                Stereo.encode(bits, data, frameSize);
            }
            encoder.Encode(bits, data);
            return true;
        }
        /// <summary>
        /// Converts a 16 bit linear PCM stream (in the form of a byte array)
        /// into a floating point PCM stream (in the form of an float array).
        /// Here are some important details about the encoding:
        /// 
        /// </summary>
        /// <param name="pcm16bitBytes">
        /// byte array of linear 16-bit PCM formated audio.
        /// </param>
        /// <param name="offsetInput"></param>
        /// <param name="samples">
        /// float array to receive the 16-bit linear audio samples.
        /// </param>
        /// <param name="offsetOutput"></param>
        /// <param name="length"></param>
        private static void mapPcm16bitLittleEndian2Float(byte[] pcm16bitBytes, int offsetInput, float[] samples, int offsetOutput, int length)
        {
            if (pcm16bitBytes.Length - offsetInput < 2 * length)
            {
                throw new Exception("Insufficient Samples to convert to floats");
            }
            if (samples.Length - offsetOutput < length)
            {
                throw new Exception("Insufficient float buffer to convert the samples");
            }
            for (int i = 0; i < length; i++)
            {
                samples[offsetOutput + i] = (float)((pcm16bitBytes[offsetInput + 2 * i] & 0xff) | (pcm16bitBytes[offsetInput + 2 * i + 1] << 8)); // no & 0xff at the end to keep the sign
            }
        }
    }
}
