using System;

namespace NSpeex
{
    /// <summary>
    /// Speex in-band and User in-band controls.
    /// </summary>
    public class Inband
    {
        private Stereo stereo;
        public Inband(Stereo stereo)
        {
            this.stereo = stereo;
        }
        /// <summary>
        /// Speex in-band request (submode=14).
        /// </summary>
        /// <param name="bits"></param>
        public void speexInbandRequest(Bits bits)
        {
            int code = bits.unpack(4);
            switch (code)
            {
                case 0: // asks the decoder to set perceptual enhancment off (0) or on (1)
                    bits.advance(1);
                    break;
                case 1: // asks (if 1) the encoder to be less "aggressive" due to high packet loss
                    bits.advance(1);
                    break;
                case 2: // asks the encoder to switch to mode N
                    bits.advance(4);
                    break;
                case 3: // asks the encoder to switch to mode N for low-band
                    bits.advance(4);
                    break;
                case 4: // asks the encoder to switch to mode N for high-band
                    bits.advance(4);
                    break;
                case 5: // asks the encoder to switch to quality N for VBR
                    bits.advance(4);
                    break;
                case 6: // request acknowledgement (0=no, 1=all, 2=only for inband data)
                    bits.advance(4);
                    break;
                case 7: // asks the encoder to set CBR(0), VAD(1), DTX(3), VBR(5), VBR+DTX(7)
                    bits.advance(4);
                    break;
                case 8: // transmit (8-bit) character to the other end
                    bits.advance(8);
                    break;
                case 9: // intensity stereo information
                    // setup the stereo decoder; to skip: tmp = bits.unpack(8); break;
                    stereo.init(bits); // read 8 bits
                    break;
                case 10: // announce maximum bit-rate acceptable (N in byets/second)
                    bits.advance(16);
                    break;
                case 11: // reserved
                    bits.advance(16);
                    break;
                case 12: // Acknowledge receiving packet N
                    bits.advance(32);
                    break;
                case 13: // reserved
                    bits.advance(32);
                    break;
                case 14: // reserved
                    bits.advance(64);
                    break;
                case 15: // reserved
                    bits.advance(64);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// User in-band request (submode=13).
        /// </summary>
        /// <param name="bits"></param>
        public void userInbandRequest(Bits bits)
        {
            try
            {
                int req_size = bits.unpack(4);
                bits.advance(5 + 8 * req_size);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
