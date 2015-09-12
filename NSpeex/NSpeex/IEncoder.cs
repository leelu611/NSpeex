namespace NSpeex
{
    public interface IEncoder
    {
        /// <summary>
        /// Encode the given input signal.
        /// </summary>
        /// <param name="bits">
        /// Speex bits buffer.
        /// </param>
        /// <param name="vin">
        /// the raw mono audio frame to encode.
        /// </param>
        /// <returns>
        /// Encode the given input signal.
        /// </returns>
        int Encode(Bits bits, float[] vin);
        
        /// <summary>
        /// Returns the size in bits of an audio frame encoded with the current mode.
        /// </summary>
        int EncodedFrameSize { get; }

        int FrameSize { get; }

        /// <summary>
        ///  Sets the Quality (between 0 and 10).
        /// </summary>
        int Quality { set; }

        /// <summary>
        /// Get the current Bit Rate.
        /// </summary>
        int BitRate { get; set; }

        float[] PitchGain { get; }

        /// <summary>
        /// excitation array.
        /// </summary>
        float[] Excitation { get; }

        /// <summary>
        /// innovation array.
        /// </summary>
        float[] Innovation { get; }
        /// <summary>
        /// Get and Set the encoding submode.
        /// </summary>
        int Mode { get; set; }
       
        /// <summary>
        /// Get and Set Sets whether or not to use Variable Bit Rate encoding.
        /// </summary>
        bool Vbr { get; set; }
       
        /// <summary>
        /// Get and Set whether or not to use Voice Activity Detection encoding.
        /// </summary>
        bool Vad { get; set; }
        
        /// <summary>
        /// Get and Set whether or not to use Discontinuous Transmission encoding.
        /// </summary>
        bool Dtx { get; set; }

        /// <summary>
        /// Get and Set the Average Bit Rate used (0 if ABR is not turned on).
        /// </summary>
        int Abr { get; set; }

        /// <summary>
        /// Get and Set Varible Bit Rate Quality.
        /// </summary>
        float VbrQuality { get; set; }
        
        /// <summary>
        /// Get and Set the desired algorithmic complexity (between 1 and 10 - default is 3).
        /// </summary>
        int Complexity { get; set; }
        
        /// <summary>
        /// Get and Set the sampling rate.
        /// </summary>
        int SamleRate { get; set; }
        
        /// <summary>
        /// LookAhead.
        /// </summary>
        int LoodAhead { get; }

        /// <summary>
        /// Get the relative quality.
        /// </summary>
        float RelativeQuality { get; }

    }
}
