namespace NSpeex
{
    /// <summary>
    /// Speex SubMode
    /// </summary>
    public class SubMode
    {
        /// <summary>
        /// Set to -1 for "normal" modes, 
        /// otherwise encode pitch using a global pitch and allowing a +- lbr_pitch variation (for low not-rates)
        /// </summary>
        public int lbr_pitch;
        /// <summary>
        ///  Use the same (forced) pitch gain for all sub-frames
        /// </summary>
        public int forced_pitch_gain;
        /// <summary>
        /// Number of bits to use as sub-frame innovation gain
        /// </summary>
        public int have_subframe_gain;
        /// <summary>
        /// Apply innovation quantization twice for higher quality (and higher bit-rate)
        /// </summary>
        public int double_codebook;
        /// <summary>
        /// LSP quantization/unquantization function
        /// </summary>
        public LspQuant lsqQuant;
        /// <summary>
        /// Long-term predictor (pitch) un-quantizer
        /// </summary>
        public Ltp ltp;
        /// <summary>
        /// Codebook Search un-quantizer
        /// </summary>
        public CbSearch innovation;
        /// <summary>
        /// Enhancer constant 
        /// </summary>
        public float lpc_enh_k1;
        /// <summary>
        /// Enhancer constant 
        /// </summary>
        public float lpc_enh_k2;
        /// <summary>
        ///  Gain of enhancer comb filter
        /// </summary>
        public float comb_gain;
        /// <summary>
        /// Number of bits per frame after encoding
        /// </summary>
        public int bits_per_frame;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lbr_pitch"></param>
        /// <param name="forced_pitch_gain"></param>
        /// <param name="have_subframe_gain"></param>
        /// <param name="double_codebook"></param>
        /// <param name="lspQuant"></param>
        /// <param name="ltp"></param>
        /// <param name="innovation"></param>
        /// <param name="lpc_enh_k1"></param>
        /// <param name="lpc_enh_k2"></param>
        /// <param name="comb_gain"></param>
        /// <param name="bits_per_frame"></param>
        public SubMode(int lbr_pitch,
                  int forced_pitch_gain,
                  int have_subframe_gain,
                  int double_codebook,
                  LspQuant lspQuant,
                  Ltp ltp,
                  CbSearch innovation,
                  float lpc_enh_k1,
                  float lpc_enh_k2,
                  float comb_gain,
                  int bits_per_frame)
        {
            this.lbr_pitch = lbr_pitch;
            this.forced_pitch_gain = forced_pitch_gain;
            this.have_subframe_gain = have_subframe_gain;
            this.double_codebook = double_codebook;
            this.lsqQuant = lspQuant;
            this.ltp = ltp;
            this.innovation = innovation;
            this.lpc_enh_k1 = lpc_enh_k1;
            this.lpc_enh_k2 = lpc_enh_k2;
            this.comb_gain = comb_gain;
            this.bits_per_frame = bits_per_frame;
        }

    }
}
