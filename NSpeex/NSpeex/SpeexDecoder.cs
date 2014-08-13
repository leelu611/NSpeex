namespace NSpeex
{
    /// <summary>
    /// Main Speex Decoder class.
    /// This class decodes the given Speex packets into PCM 16bit samples.
    /// Here's an example that decodes and recovers one Speex packet.
    /// SpeexDecoder speexDecoder = new SpeexDecoder();
    /// speexDecoder.processData(data, packetOffset, packetSize);
    /// byte[] decoded = new byte[speexDecoder.getProcessedBataByteSize()];
    /// speexDecoder.getProcessedData(decoded, 0);
    /// </summary>
    public class SpeexDecoder
    {
        public const string VERSION = "Java Speex Decoder v0.9.7 ($Revision: 1.4 $)";
        private int sampleRate;
        private int channels;
        private float[] decodedData;
        private short[] outputData;
        private int outputSize;
        private Bits bits;
        private Decoder decoder;
        private int frameSize;
        /// <summary>
        /// Constructor
        /// </summary>
        public SpeexDecoder()
        {
            bits = new Bits();
            sampleRate = 0;
            channels = 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode">
        /// the mode of the decoder (0=NB, 1=WB, 2=UWB).
        /// </param>
        /// <param name="sampleRate">
        /// the number of samples per second.
        /// </param>
        /// <param name="channels">
        /// the number of audio channels (1=mono, 2=stereo, ...).
        /// </param>
        /// <param name="enhanced">
        /// whether to enable perceptual enhancement or not.
        /// </param>
        /// <returns></returns>
        public bool init(int mode, int sampleRate, int channels, bool enhanced)
        {
            switch (mode)
            {
                case 0:
                    decoder = new NbDecoder();
                    ((NbDecoder)decoder).nbinit();
                    break;
                //Wideband
                case 1:
                    decoder = new SbDecoder();
                    ((SbDecoder)decoder).wbinit();
                    break;
                case 2:
                    decoder = new SbDecoder();
                    ((SbDecoder)decoder).uwbinit();
                    break;
                //*/
                default:
                    return false;
            }
            /* initialize the speex decoder */
            decoder.setPerceptualEnhancement(enhanced);
            /* set decoder format and properties */
            this.frameSize = decoder.getFrameSize();
            this.sampleRate = sampleRate;
            this.channels = channels;
            int secondSize = sampleRate * channels;
            decodedData = new float[secondSize * 2];
            outputData = new short[secondSize * 2];
            outputSize = 0;
            bits.init();
            return true;

        }

        public int getSampleRate()
        {
            return sampleRate;
        }

        public int getChannels()
        {
            return channels;
        }

        /// <summary>
        /// This is where the actual decoding takes place
        /// 
        /// </summary>
        /// <param name="data">
        /// the Speex data (frame) to decode.
        /// If it is null, the packet is supposed lost.
        /// 
        /// </param>
        /// <param name="offset">
        /// the offset from which to start reading the data.
        /// </param>
        /// <param name="len">
        ///  the length of data to read (Speex frame size).
        /// </param>
        public void processData(byte[] data, int offset, int len)
        {
            if (data == null)
            {
                processData(true);
            }
            else
            {
                /* read packet bytes into bitstream */
                bits.read_from(data, offset, len);
                processData(false);
            }
        }
        /// <summary>
        ///  This is where the actual decoding takes place.
        /// </summary>
        /// <param name="lost">
        ///  true if the Speex packet has been lost.
        /// </param>
        private void processData(bool lost)
        {
            /* decode the bitstream */
            if (lost)
                decoder.decode(null, decodedData);
            else
                decoder.decode(bits, decodedData);
            if (channels == 2)
                decoder.decodeStereo(decodedData, frameSize);
            for (int i = 0; i < frameSize * channels; i++)
            {
                if (decodedData[i] > 32767.0f)
                    decodedData[i] = 32767.0f;
                else if (decodedData[i] < -32768.0f)
                    decodedData[i] = -32768.0f;
            }
            outputSize = frameSize * channels;
        }

        public int getProcessedDataSize()
        {
            return outputSize;
        }

        public  int getProcessedData(float[] data,int offset)
        {
            if (outputSize <= 0)
            {
                return outputSize;
            }
            System.Array.Copy(decodedData, 0, data, offset, outputSize);
            int size = outputSize;
            outputSize = 0;
            return size;
        }

        public int getProcessedData(short[] data, int offset)
        {

            if (outputSize <= 0)
            {
                return outputSize;
            }
            
            for (int i = 0; i < outputSize; i++)
            {
                outputData[i] = (decodedData[i] > 0) ?
                                         (short)(decodedData[i] + .5) :
                                         (short)(decodedData[i] - .5);
            }
            System.Array.Copy(outputData, 0, data, offset, outputSize);
            int size = outputSize;
            outputSize = 0;
            return size;
        }
    }
}
