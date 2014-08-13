namespace NSpeex
{
    /// <summary>
    /// Narrowband Codec.
    /// This class contains all the basic structures needed by the Narrowband
    /// encoder and decoder.
    /// </summary>
    public abstract class NbCodec:Codebook
    {
        /// <summary>
        /// Very small initial value for some of the buffers. 
        /// </summary>
        public const float VERY_SMALL = (float)0e-30;
        /// <summary>
        /// The Narrowband Frame Size gives the size in bits of a Narrowband frame for a given narrowband submode.
        /// </summary>
        public static readonly int[] NB_FRAME_SIZE = new int[]{ 5, 43, 119, 160, 220, 300, 364, 492, 79, 1, 1, 1, 1, 1, 1, 1 };
        /// <summary>
        /// The Narrowband Submodes gives the number of submodes possible for the Narrowband codec.
        /// </summary>
        public const int NB_SUBMODES = 16;
        /// <summary>
        /// The Narrowband Submodes Bits gives the number bits used to encode the Narrowband Submode
        /// </summary>
        public const int NB_SUBMODE_BITS = 4;

        public static readonly  float[] exc_gain_quant_scal1 = new float[]{ -0.35f, 0.05f };
        public static readonly float[] exc_gain_quant_scal3 = new float[]
        {
            -2.794750f, -1.810660f,
            -1.169850f, -0.848119f, 
            -0.587190f, -0.329818f,
            -0.063266f, 0.282826f
        };
        protected Lsp m_lsp;
        protected Filters filters;
        protected SubMode[] submodes;
        protected int submodeID;
        protected int first;
        protected int frameSize;
        protected int subframeSize;
        protected int nbSubframes;
        protected int windowSize;
        protected int lpcSize;
        protected int bufSize;
        protected int min_pitch;
        protected int max_pitch;
        protected float gamma1;
        protected float gamma2;
        protected float lag_factor;
        protected float lpc_floor;
        protected float preemph;
        protected float pre_mem;


        protected float[] frmBuf;      
        protected int frmIdx;
        protected float[] excBuf;      
        protected int excIdx;     
        protected float[] innov;       
        protected float[] lpc;       
        protected float[] qlsp;       
        protected float[] old_qlsp;   
        protected float[] interp_qlsp; 
        protected float[] interp_qlpc;
        protected float[] mem_sp;     
        protected float[] pi_gain;    
        protected float[] awk1, awk2, awk3;

        protected float voc_m1;
        protected float voc_m2;
        protected float voc_mean;
        protected int voc_offset;
        /// <summary>
        /// 1 for enabling DTX, 0 otherwise
        /// </summary>
        protected int dtx_enabled;
        /// <summary>
        /// Constructor
        /// </summary>
        public NbCodec()
        {
            m_lsp = new Lsp();
            filters = new Filters();
        }
        /// <summary>
        /// Narrowband initialisation.
        /// </summary>
        public  void nbinit()
        {
            // Initialize SubModes
            submodes = buildNbSubModes();
            submodeID = 5;
            // Initialize narrwoband parameters and variables
            init(160, 40, 10, 640);
        }
        /// <summary>
        /// Initialisation.
        /// </summary>
        /// <param name="frameSize"></param>
        /// <param name="subframeSize"></param>
        /// <param name="lpcSize"></param>
        /// <param name="bufSize"></param>
        protected void init(int frameSize, int subframeSize, int lpcSize, int bufSize)
        {
            first = 1;
            // Codec parameters, should eventually have several "modes"
            this.frameSize = frameSize;
            this.windowSize = frameSize * 3 / 2;
            this.subframeSize = subframeSize;
            this.nbSubframes = frameSize / subframeSize;
            this.lpcSize = lpcSize;
            this.bufSize = bufSize;
            min_pitch = 17;
            max_pitch = 144;
            preemph = 0.0f;
            pre_mem = 0.0f;
            gamma1 = 0.9f;
            gamma2 = 0.6f;
            lag_factor = .01f;
            lpc_floor = 1.0001f;

            frmBuf = new float[bufSize];
            frmIdx = bufSize - windowSize;
            excBuf = new float[bufSize];
            excIdx = bufSize - windowSize;
            innov = new float[frameSize];

            lpc = new float[lpcSize + 1];
            qlsp = new float[lpcSize];
            old_qlsp = new float[lpcSize];
            interp_qlsp = new float[lpcSize];
            interp_qlpc = new float[lpcSize + 1];
            mem_sp = new float[5 * lpcSize]; // TODO - check why 5 (why not 2 or 1)
            pi_gain = new float[nbSubframes];

            awk1 = new float[lpcSize + 1];
            awk2 = new float[lpcSize + 1];
            awk3 = new float[lpcSize + 1];

            voc_m1 = voc_m2 = voc_mean = 0;
            voc_offset = 0;
            dtx_enabled = 0; // disabled by default
        }
        /// <summary>
        /// Build narrowband submodes
        /// </summary>
        /// <returns></returns>
        protected static SubMode[] buildNbSubModes()
        {
            /* Initialize Long Term Predictions */
            Ltp3Tap ltpNb = new Ltp3Tap(Codebook.gain_cdbk_nb, 7, 7);
            Ltp3Tap ltpVlbr = new Ltp3Tap(Codebook.gain_cdbk_lbr, 5, 0);
            Ltp3Tap ltpLbr = new Ltp3Tap(Codebook.gain_cdbk_lbr, 5, 7);
            Ltp3Tap ltpMed = new Ltp3Tap(Codebook.gain_cdbk_lbr, 5, 7);
            LtpForcedPitch ltpFP = new LtpForcedPitch();
            /* Initialize Codebook Searches */
            NoiseSearch noiseSearch = new NoiseSearch();
            SplitShapeSearch ssNbVlbrSearch = new SplitShapeSearch(40, 10, 4, Codebook.exc_10_16_table, 4, 0);
            SplitShapeSearch ssNbLbrSearch = new SplitShapeSearch(40, 10, 4, Codebook.exc_10_32_table, 5, 0);
            SplitShapeSearch ssNbSearch = new SplitShapeSearch(40, 5, 8, Codebook.exc_5_64_table, 6, 0);
            SplitShapeSearch ssNbMedSearch = new SplitShapeSearch(40, 8, 5, Codebook.exc_8_128_table, 7, 0);
            SplitShapeSearch ssSbSearch = new SplitShapeSearch(40, 5, 8, Codebook.exc_5_256_table, 8, 0);
            SplitShapeSearch ssNbUlbrSearch = new SplitShapeSearch(40, 20, 2, Codebook.exc_20_32_table, 5, 0);
            /* Initialize Line Spectral Pair Quantizers */
            NbLspQuant nbLspQuant = new NbLspQuant();
            LbrLspQuant lbrLspQuant = new LbrLspQuant();
            /* Initialize narrow-band modes */
            SubMode[] nbSubModes = new SubMode[NB_SUBMODES];
            /* 2150 bps "vocoder-like" mode for comfort noise */
            nbSubModes[1] = new SubMode(0, 1, 0, 0, lbrLspQuant, ltpFP, noiseSearch, .7f, .7f, -1, 43);
            /* 5.95 kbps very low bit-rate mode */
            nbSubModes[2] = new SubMode(0, 0, 0, 0, lbrLspQuant, ltpVlbr, ssNbVlbrSearch, 0.7f, 0.5f, .55f, 119);
            /* 8 kbps low bit-rate mode */
            nbSubModes[3] = new SubMode(-1, 0, 1, 0, lbrLspQuant, ltpLbr, ssNbLbrSearch, 0.7f, 0.55f, .45f, 160);
            /* 11 kbps medium bit-rate mode */
            nbSubModes[4] = new SubMode(-1, 0, 1, 0, lbrLspQuant, ltpMed, ssNbMedSearch, 0.7f, 0.63f, .35f, 220);
            /* 15 kbps high bit-rate mode */
            nbSubModes[5] = new SubMode(-1, 0, 3, 0, nbLspQuant, ltpNb, ssNbSearch, 0.7f, 0.65f, .25f, 300);
            /* 18.2 high bit-rate mode */
            nbSubModes[6] = new SubMode(-1, 0, 3, 0, nbLspQuant, ltpNb, ssSbSearch, 0.68f, 0.65f, .1f, 364);
            /* 24.6 kbps high bit-rate mode */
            nbSubModes[7] = new SubMode(-1, 0, 3, 1, nbLspQuant, ltpNb, ssNbSearch, 0.65f, 0.65f, -1, 492);
            /* 3.95 kbps very low bit-rate mode */
            nbSubModes[8] = new SubMode(0, 1, 0, 0, lbrLspQuant, ltpFP, ssNbUlbrSearch, .7f, .5f, .65f, 79);
            /* Return the Narrowband SubModes*/
            return nbSubModes;
        }


        /// <summary>
        /// Returns the size of a frame (ex: 160 samples for a narrowband frame,
        /// 320 for wideband and 640 for ultra-wideband).
        /// </summary>
        /// <returns>the size of a frame (number of audio samples in a frame).</returns>
        public int getFrameSize()
        {
            return frameSize;
        }
        /// <summary>
        /// Returns whether or not we are using Discontinuous Transmission encoding.
        /// </summary>
        /// <returns>
        /// whether or not we are using Discontinuous Transmission encoding.
        /// </returns>
        public bool getDtx()
        {
            return dtx_enabled != 0;
        }
        /// <summary>
        /// Returns the Pitch Gain array.
        /// </summary>
        /// <returns>the Pitch Gain array.</returns>
        public float[] getPiGain()
        {
            return pi_gain;
        }
        /// <summary>
        /// Returns the excitation array.
        /// </summary>
        /// <returns>
        /// the excitation array.
        /// </returns>
        public float[] getExc()
        {
            float[] excTmp = new float[frameSize];
            System.Array.Copy(excBuf, excIdx, excTmp, 0, frameSize);
            return excTmp;
        }
        /// <summary>
        /// Returns the innovation array.
        /// </summary>
        /// <returns>
        /// the innovation array.
        /// </returns>
        public float[] getInnov()
        {
            return innov;
        }

    }
}
