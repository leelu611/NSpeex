namespace NSpeex
{
    /// <summary>
    /// Speex Decoder inteface, used as a base for the Narrowband and sideband
    /// decoders.
    /// </summary>
    public interface IDecoder
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
        int Decode(Bits bits, float[] vout);
        
        /// <summary>
        /// Decode the given bits to stereo.
        /// </summary>
        /// <param name="data">
        /// float array of size 2*frameSize, that contains the mono
        /// audio samples in the first half. When the function has completed, the
        /// array will contain the interlaced stereo audio samples.
        /// </param>
        /// <param name="frameSize">
        /// the size of a frame of mono audio samples.
        /// </param>
        void DecodeStereo(float[] data, int frameSize);
        
        /// <summary>
        /// get or set Enables or disables perceptual enhancement.
        /// </summary>
        bool PerceptualEnhancement { get; set; }

        int FrameSize { get; }

        /// <summary>
        /// whether or not we are using Discontinuous Transmission encoding.
        /// </summary>
        bool Dtx { get; }

        /// <summary>
        /// Pitch Gain array.
        /// </summary>
        float[] PitchGain { get; }

        /// <summary>
        /// excitation array.
        /// </summary>
        float[] Excitation { get; }

        /// <summary>
        /// innovation array.
        /// </summary>
        float[] Innovation { get; }

    }
}
