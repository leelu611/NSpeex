namespace NSpeex
{
    /// <summary>
    ///the base interface of Encoder and Decoder
    /// </summary>
    public interface ICoder
    {
        /// <summary>
        /// the size of a frame.
        /// (ex: 160 samples for a narrowband frame,320 for wideband and 640 for ultra-wideband).
        /// </summary>
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
