namespace NSpeex
{
    /// <summary>
    /// Sideband Codec.
    /// This class contains all the basic structures needed by the Sideband
    /// encoder and decoder.
    /// </summary>
    public class SbCodec:NbCodec
    {
        public static readonly  int[] SB_FRAME_SIZE = new int[]{ 4, 36, 112, 192, 352, -1, -1, -1 };
        public const int SB_SUBMODES = 8;
        public const int SB_SUBMODE_BITS = 3;
        public const int QMF_ORDER = 64;
        protected int fullFrameSize;
        protected float foldingGain;
        protected float[] high;
        protected float[] y0, y1;
        protected float[] x0d;
        protected float[] g0_mem, g1_mem;
        /// <summary>
        /// Wideband initialisation
        /// </summary>
        public void wbinit()
        {
            submodes = buildWbSubModes();
            submodeID = 3;
        }
        /// <summary>
        /// Ultra-wideband initialisation
        /// </summary>
        public void uwbinit()
        {
            submodes = buildUwbSubModes();
            submodeID = 1;
        }
        /// <summary>
        /// Initialisation
        /// </summary>
        protected void init(int frameSize, int subframeSize, int lpcSize, int bufSize, float foldingGain)
        {
            base.init(frameSize, subframeSize, lpcSize, bufSize);
            this.fullFrameSize = 2 * frameSize;
            this.foldingGain = foldingGain;

            lag_factor = 0.002f;

            high = new float[fullFrameSize];
            y0 = new float[fullFrameSize];
            y1 = new float[fullFrameSize];
            x0d = new float[frameSize];
            g0_mem = new float[QMF_ORDER];
            g1_mem = new float[QMF_ORDER];
        }
        /// <summary>
        /// Build wideband submodes.
        /// </summary>
        /// <returns>
        /// the wideband submodes.
        /// </returns>
        protected static SubMode[] buildWbSubModes()
        {
            /* Initialize Long Term Predictions */
            HighLspQuant highLU = new HighLspQuant();
            /* Initialize Codebook Searches */
            SplitShapeSearch ssCbHighLbrSearch = new SplitShapeSearch(40, 10, 4, Codebook.hexc_10_32_table, 5, 0);
            SplitShapeSearch ssCbHighSearch = new SplitShapeSearch(40, 8, 5, Codebook.hexc_table, 7, 1);
            /* Initialize wide-band modes */
            SubMode[] wbSubModes = new SubMode[SB_SUBMODES];
            wbSubModes[1] = new SubMode(0, 0, 1, 0, highLU, null, null, .75f, .75f, -1, 36);
            wbSubModes[2] = new SubMode(0, 0, 1, 0, highLU, null, ssCbHighLbrSearch, .85f, .6f, -1, 112);
            wbSubModes[3] = new SubMode(0, 0, 1, 0, highLU, null, ssCbHighSearch, .75f, .7f, -1, 192);
            wbSubModes[4] = new SubMode(0, 0, 1, 1, highLU, null, ssCbHighSearch, .75f, .75f, -1, 352);
            return wbSubModes;
        }
        /// <summary>
        /// Build ultra-wideband submodes.
        /// </summary>
        /// <returns>
        /// the ultra-wideband submodes.
        /// </returns>
        protected static SubMode[] buildUwbSubModes()
        {
            /* Initialize Long Term Predictions */
            HighLspQuant highLU = new HighLspQuant();
            SubMode[] uwbSubModes = new SubMode[SB_SUBMODES];
            uwbSubModes[1] = new SubMode(0, 0, 1, 0, highLU, null, null, .75f, .75f, -1, 2);
            return uwbSubModes;
        }


        public new float[] Excitation
        {
            get
            {
                int i;
                float[] excTmp = new float[fullFrameSize];
                for (i = 0; i < frameSize; i++)
                    excTmp[2 * i] = 2 * excBuf[excIdx + i];
                return excTmp;
            }
        }


        public new float[] Innovation
        {
            get { return Excitation; }
            
        }
    }
}
