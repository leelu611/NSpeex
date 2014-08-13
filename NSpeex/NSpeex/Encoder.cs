namespace NSpeex
{
    public interface Encoder
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
        int encode(Bits bits, float[] vin);

        /// <summary>
        /// Returns the size in bits of an audio frame encoded with the current mode.
        /// </summary>
        /// <returns></returns>
        int getEncodedFrameSize();
        /// <summary>
        /// Returns the size of a frame.
        /// </summary>
        /// <returns></returns>
        int getFrameSize();
        /// <summary>
        /// Sets the Quality (between 0 and 10).
        /// </summary>
        /// <param name="quality"></param>
        void setQuality(int quality);
        /// <summary>
        /// Get the current Bit Rate.
        /// </summary>
        /// <returns></returns>
        int getBitRate();
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
        /// <summary>
        /// Sets the encoding submode.
        /// </summary>
        /// <param name="mode"></param>
        void setMode(int mode);
        /// <summary>
        /// Returns the encoding submode currently in use.
        /// </summary>
        /// <returns></returns>
        int getMode();
        /// <summary>
        /// Sets the bitrate.
        /// </summary>
        /// <param name="bitrate"></param>
        void setBitRate(int bitrate);
        /// <summary>
        /// Sets whether or not to use Variable Bit Rate encoding.
        /// </summary>
        /// <param name="vbr"></param>
        void setVbr(bool vbr);
        /// <summary>
        /// Returns whether or not we are using Variable Bit Rate encoding.
        /// </summary>
        /// <returns></returns>
        bool getVbr();
        /// <summary>
        /// Sets whether or not to use Voice Activity Detection encoding.
        /// </summary>
        /// <param name="vad"></param>
        void setVad(bool vad);
        /// <summary>
        /// Returns whether or not we are using Voice Activity Detection encoding.
        /// </summary>
        /// <returns></returns>
        bool getVad();
        /// <summary>
        /// Sets whether or not to use Discontinuous Transmission encoding.
        /// </summary>
        /// <param name="dtx"></param>
        void setDtx(bool dtx);
        /// <summary>
        /// Returns whether or not we are using Discontinuous Transmission encoding.
        /// </summary>
        /// <returns></returns>
        bool getDtx();
        /// <summary>
        /// Returns the Average Bit Rate used (0 if ABR is not turned on).
        /// </summary>
        /// <returns></returns>
        int getAbr();
        /// <summary>
        /// Sets the Average Bit Rate.
        /// </summary>
        /// <param name="abr"></param>
        void setAbr(int abr);
        /// <summary>
        /// Sets the Varible Bit Rate Quality.
        /// </summary>
        /// <param name="quality">
        /// the desired Varible Bit Rate Quality.
        /// </param>
        void setVbrQuality(float quality);
        /// <summary>
        /// Returns the Varible Bit Rate Quality.
        /// </summary>
        /// <returns></returns>
        float getVbrQuality();
        /// <summary>
        /// Sets the algorithmic complexity.
        /// </summary>
        /// <param name="complexity">
        /// the desired algorithmic complexity (between 1 and 10 - default is 3).
        /// </param>
        void setComplexity(int complexity);
        /// <summary>
        /// Returns the algorthmic complexity.
        /// </summary>
        /// <returns></returns>
        int getComplexity();
        /// <summary>
        /// Sets the sampling rate.
        /// </summary>
        /// <param name="rate"></param>
        void setSamplingRate(int rate);

        /// <summary>
        ///  Returns the sampling rate.
        /// </summary>
        /// <returns></returns>
        int getSamplingRate();

        /// <summary>
        /// Return LookAhead.
        /// </summary>
        /// <returns></returns>
        int getLookAhead();
        /// <summary>
        /// Returns the relative quality.
        /// </summary>
        /// <returns></returns>
        float getRelativeQuality();
    }
}
