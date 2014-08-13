namespace NSpeex
{
    /// <summary>
    /// Speex Decoder inteface, used as a base for the Narrowband and sideband
    /// decoders.
    /// </summary>
    public interface Decoder
    {
        /// <summary>
        /// Decode the given input bits.
        /// </summary>
        /// <param name="bits">
        /// Speex bits buffer.
        /// </param>
        /// <param name="vout">
        /// the decoded mono audio frame.
        /// </param>
        /// <returns>
        /// 1 if a terminator was found, 0 if not.
        /// </returns>
        int decode(Bits bits, float[] vout);
        /// <summary>
        /// Decode the given bits to stereo.
        /// </summary>
        /// <param name="data">
        /// float array of size 2*frameSize, that contains the mono
        /// audio samples in the first half. When the function has completed, the
        /// array will contain the interlaced stereo audio samples.
        /// 
        /// </param>
        /// <param name="frameSize">
        /// the size of a frame of mono audio samples.
        /// </param>
        void decodeStereo(float[] data, int frameSize);
        /// <summary>
        /// Enables or disables perceptual enhancement.
        /// </summary>
        /// <param name="enhanced"></param>
        void setPerceptualEnhancement(bool enhanced);
        /// <summary>
        /// Returns whether perceptual enhancement is enabled or disabled.
        /// </summary>
        /// <returns></returns>
        bool getPerceptualEnhancement();
        /// <summary>
        /// Returns the size of a frame.
        /// </summary>
        /// <returns></returns>
        int getFrameSize();
        /// <summary>
        /// Returns whether or not we are using Discontinuous Transmission encoding.
        /// </summary>
        /// <returns></returns>
        bool getDtx();
        /// <summary>
        /// Returns the Pitch Gain array.
        /// </summary>
        /// <returns></returns>
        float[] getPiGain();
        /// <summary>
        /// Returns the excitation array.
        /// </summary>
        /// <returns></returns>
        float[] getExc();
        /// <summary>
        /// Returns the innovation array.
        /// </summary>
        /// <returns></returns>
        float[] getInnov();
    }
}
