using System;

namespace NSpeex
{
    /// <summary>
    /// Wideband Speex Encoder
    /// </summary>
    public class SbEncoder:SbCodec,IEncoder
    {
        /// <summary>
        /// The Narrowband Quality map indicates which narrowband submode
        /// to use for the given wideband/ultra-wideband quality setting
        /// </summary>
        public static readonly int[] NB_QUALITY_MAP = new int[]{ 1, 8, 2, 3, 4, 5, 5, 6, 6, 7, 7 };
        /// <summary>
        /// The Wideband Quality map indicates which sideband submode 
        /// to use for the given wideband/ultra-wideband quality setting
        /// </summary>
        public static readonly int[] WB_QUALITY_MAP = new int[] { 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4 };
        /// <summary>
        /// The Ultra-wideband Quality map indicates which sideband submode 
        /// to use for the given ultra-wideband quality setting
        /// </summary>
        public static readonly int[] UWB_QUALITY_MAP = new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        protected IEncoder lowenc;

        private float[] x1d;
        private float[] h0_mem;
        private float[] buf;
        private float[] swBuf;
        private float[] res;
        private float[] target;
        private float[] window;
        private float[] lagWindow;
        private float[] rc;
        private float[] autocorr;
        private float[] lsp;
        private float[] old_lsp;
        private float[] interp_lsp;
        private float[] interp_lpc;
        private float[] bw_lpc1;
        private float[] bw_lpc2;
        private float[] mem_sp2;
        private float[] mem_sw;
        protected int nb_modes;
        private bool uwb;
        protected int complexity;
        protected int vbr_enabled;
        protected int vad_enabled;
        protected int abr_enabled;
        protected float vbr_quality;
        protected float relative_quality;
        protected float abr_drift;
        protected float abr_drift2;
        protected float abr_count;
        protected int sampling_rate;
        protected int submodeSelect;


        /// <summary>
        /// Wideband initialisation
        /// </summary>
        public override void wbinit()
        {
            lowenc = new NbEncoder();
            ((NbEncoder)lowenc).nbinit();
            // Initialize SubModes
            base.wbinit();
            // Initialize variables
            init(160, 40, 8, 640, .9f);
            uwb = false;
            nb_modes = 5;
            sampling_rate = 16000;
        }

        /// <summary>
        /// Ultra-wideband initialisation
        /// </summary>
        public override void uwbinit()
        {
            lowenc = new SbEncoder();
            ((SbEncoder)lowenc).wbinit();
            // Initialize SubModes
            base.uwbinit();
            // Initialize variables
            init(320, 80, 8, 1280, .7f);
            uwb = true;
            nb_modes = 2;
            sampling_rate = 32000;
        }
        /// <summary>
        /// Initialisation
        /// </summary>
        /// <param name="frameSize"></param>
        /// <param name="subframeSize"></param>
        /// <param name="lpcSize"></param>
        /// <param name="bufSize"></param>
        /// <param name="foldingGain"></param>
        public override void init(int frameSize, int subframeSize, int lpcSize, int bufSize, float foldingGain)
        {
            base.init(frameSize, subframeSize, lpcSize, bufSize, foldingGain);
            complexity = 3; // in C it's 2 here, but set to 3 automatically by the encoder
            vbr_enabled = 0; // disabled by default
            vad_enabled = 0; // disabled by default
            abr_enabled = 0; // disabled by default
            vbr_quality = 8;

            submodeSelect = submodeID;

            x1d = new float[frameSize];
            h0_mem = new float[QMF_ORDER];
            buf = new float[windowSize];
            swBuf = new float[frameSize];
            res = new float[frameSize];
            target = new float[subframeSize];

            window = Misc.window(windowSize, subframeSize);
            lagWindow = Misc.lagWindow(lpcSize, lag_factor);

            rc = new float[lpcSize];
            autocorr = new float[lpcSize + 1];
            lsp = new float[lpcSize];
            old_lsp = new float[lpcSize];
            interp_lsp = new float[lpcSize];
            interp_lpc = new float[lpcSize + 1];
            bw_lpc1 = new float[lpcSize + 1];
            bw_lpc2 = new float[lpcSize + 1];

            mem_sp2 = new float[lpcSize];
            mem_sw = new float[lpcSize];

            abr_count = 0;
        }

        /// <summary>
        /// Encode the given input signal.
        /// </summary>
        /// <param name="bits">Speex bits buffer.</param>
        /// <param name="vin">the raw mono audio frame to encode</param>
        /// <returns>1 if successful.</returns>
        public int Encode(Bits bits, float[] vin)
        {
            int i;
            float[] mem, innov, syn_resp;
            float[] low_pi_gain, low_exc, low_innov;
            int dtx;
    
            /* Compute the two sub-bands by filtering with h0 and h1*/
            Filters.qmf_decomp(vin, h0, x0d, x1d, fullFrameSize, QMF_ORDER, h0_mem);
            /* Encode the narrowband part*/
            lowenc.Encode(bits, x0d);

            /* High-band buffering / sync with low band */
            for (i=0;i<windowSize-frameSize;i++)
              high[i] = high[frameSize+i];
            for (i=0;i<frameSize;i++)
              high[windowSize-frameSize+i]=x1d[i];

            Array.Copy(excBuf, frameSize, excBuf, 0, bufSize-frameSize);

            low_pi_gain = lowenc.PitchGain;
            low_exc = lowenc.Excitation;
            low_innov   = lowenc.Innovation;


            int low_mode = lowenc.Mode;
            if (low_mode==0)
              dtx=1;
            else
              dtx=0;

            /* Start encoding the high-band */
            for (i=0; i<windowSize; i++)
              buf[i] = high[i] * window[i];

            /* Compute auto-correlation */
            Lpc.autocorr(buf, autocorr, lpcSize+1, windowSize);

            autocorr[0] += 1;        /* prevents NANs */
            autocorr[0] *= lpc_floor; /* Noise floor in auto-correlation domain */
            /* Lag windowing: equivalent to filtering in the power-spectrum domain */
            for (i=0; i<lpcSize+1; i++)
              autocorr[i] *= lagWindow[i];

            /* Levinson-Durbin */
            Lpc.wld(lpc, autocorr, rc, lpcSize); // tmperr
            Array.Copy(lpc, 0, lpc, 1, lpcSize);
            lpc[0]=1;
            /* LPC to LSPs (x-domain) transform */
            int roots = Lsp.lpc2lsp(lpc, lpcSize, lsp, 15, 0.2f);
            if (roots != lpcSize)
            {
                roots = Lsp.lpc2lsp(lpc, lpcSize, lsp, 11, 0.02f);
                if (roots != lpcSize)
                {
                    /*If we can't find all LSP's, do some damage control and use a flat filter*/
                    for (i = 0; i < lpcSize; i++)
                    {
                        lsp[i] = (float)Math.Cos(Math.PI * ((float)(i + 1)) / (lpcSize + 1));
                    }
                }
            }
            /* x-domain to angle domain*/
            for (i = 0; i < lpcSize; i++)
                lsp[i] = (float)Math.Acos(lsp[i]);

            float lsp_dist = 0;
            for (i = 0; i < lpcSize; i++)
                lsp_dist += (old_lsp[i] - lsp[i]) * (old_lsp[i] - lsp[i]);
            /*VBR stuff*/
            if ((vbr_enabled != 0 || vad_enabled != 0) && dtx == 0)
            {
                float e_low = 0, e_high = 0;
                float ratio;
                if (abr_enabled != 0)
                {
                    float qual_change = 0;
                    if (abr_drift2 * abr_drift > 0)
                    {
                        /* Only adapt if long-term and short-term drift are the same sign */
                        qual_change = -.00001f * abr_drift / (1 + abr_count);
                        if (qual_change > .1f)
                            qual_change = .1f;
                        if (qual_change < -.1f)
                            qual_change = -.1f;
                    }
                    vbr_quality += qual_change;
                    if (vbr_quality > 10)
                        vbr_quality = 10;
                    if (vbr_quality < 0)
                        vbr_quality = 0;
                }
                for (i = 0; i < frameSize; i++)
                {
                    e_low += x0d[i] * x0d[i];
                    e_high += high[i] * high[i];
                }
                ratio = (float)Math.Log((1 + e_high) / (1 + e_low));
                relative_quality = lowenc.RelativeQuality;

                if (ratio < -4)
                    ratio = -4;
                if (ratio > 2)
                    ratio = 2;
                /*if (ratio>-2)*/
                if (vbr_enabled != 0)
                {
                    int modeid;
                    modeid = nb_modes - 1;
                    relative_quality += 1.0f * (ratio + 2f);
                    if (relative_quality < -1)
                    {
                        relative_quality = -1;
                    }
                    while (modeid != 0)
                    {
                        int v1;
                        float thresh;
                        v1 = (int)Math.Floor(vbr_quality);
                        if (v1 == 10)
                        {
                            thresh =  NSpeex.Vbr.hb_thresh[modeid][v1];
                          
                        }
                        else
                        {
                            thresh = (vbr_quality - v1) * NSpeex.Vbr.hb_thresh[modeid][v1 + 1] + (1 + v1 - vbr_quality) * NSpeex.Vbr.hb_thresh[modeid][v1];
                        }
                        if (relative_quality >= thresh)
                            break;
                        modeid--;
                    }
                    Mode = modeid;
                    if (abr_enabled != 0)
                    {
                        int bitrate = BitRate;
                        abr_drift += (bitrate - abr_enabled);
                        abr_drift2 = .95f * abr_drift2 + .05f * (bitrate - abr_enabled);
                        abr_count += 1.0f;
                    }
                }
                else
                {
                    /* VAD only */
                    int modeid;
                    if (relative_quality < 2.0)
                        modeid = 1;
                    else
                        modeid = submodeSelect;
                    /*speex_encoder_ctl(state, SPEEX_SET_MODE, &mode);*/
                    submodeID = modeid;
                }
            }
            bits.Pack(1, 1);
            if (dtx != 0)
                bits.Pack(0, SB_SUBMODE_BITS);
            else
                bits.Pack(submodeID, SB_SUBMODE_BITS);
            /* If null mode (no transmission), just set a couple things to zero*/
            if (dtx != 0 || submodes[submodeID] == null)
            {
              for (i=0; i<frameSize; i++)
                excBuf[excIdx+i]=swBuf[i]=VERY_SMALL;

              for (i=0; i<lpcSize; i++)
                mem_sw[i]=0;
              first=1;

              /* Final signal synthesis from excitation */
              Filters.iir_mem2(excBuf, excIdx, interp_qlpc, high, 0, subframeSize, lpcSize, mem_sp);

              /* Reconstruct the original */
              filters.fir_mem_up(x0d, h0, y0, fullFrameSize, QMF_ORDER, g0_mem);
              filters.fir_mem_up(high, h1, y1, fullFrameSize, QMF_ORDER, g1_mem);

              for (i=0; i<fullFrameSize; i++)
                vin[i]=2*(y0[i]-y1[i]);

              if (dtx != 0)
                 return 0;
              else
                 return 1;
            }
            /* LSP quantization */
            submodes[submodeID].lsqQuant.quant(lsp, qlsp, lpcSize, bits);

            if (first != 0)
            {
                for (i = 0; i < lpcSize; i++)
                    old_lsp[i] = lsp[i];
                for (i = 0; i < lpcSize; i++)
                    old_qlsp[i] = qlsp[i];
            }
            mem = new float[lpcSize];
            syn_resp = new float[subframeSize];
            innov = new float[subframeSize];
            for (int sub = 0; sub < nbSubframes; sub++)
            {
                float tmp, filter_ratio;
                int exc, sp, sw, resp;
                int offset;
                float rl, rh, eh = 0, el = 0;
                int fold;

                offset = subframeSize * sub;
                sp = offset;
                exc = excIdx + offset;
                resp = offset;
                sw = offset;
                /* LSP interpolation (quantized and unquantized) */
                tmp = (1.0f + sub) / nbSubframes;
                for (i = 0; i < lpcSize; i++)
                    interp_lsp[i] = (1 - tmp) * old_lsp[i] + tmp * lsp[i];
                for (i = 0; i < lpcSize; i++)
                    interp_qlsp[i] = (1 - tmp) * old_qlsp[i] + tmp * qlsp[i];

                Lsp.enforce_margin(interp_lsp, lpcSize, .05f);
                Lsp.enforce_margin(interp_qlsp, lpcSize, .05f);
                /* Compute interpolated LPCs (quantized and unquantized) */
                for (i = 0; i < lpcSize; i++)
                    interp_lsp[i] = (float)Math.Cos(interp_lsp[i]);
                for (i = 0; i < lpcSize; i++)
                    interp_qlsp[i] = (float)Math.Cos(interp_qlsp[i]);

                m_lsp.lsp2lpc(interp_lsp, interp_lpc, lpcSize);
                m_lsp.lsp2lpc(interp_qlsp, interp_qlpc, lpcSize);

                Filters.bw_lpc(gamma1, interp_lpc, bw_lpc1, lpcSize);
                Filters.bw_lpc(gamma2, interp_lpc, bw_lpc2, lpcSize);
                /* Compute mid-band (4000 Hz for wideband) response of low-band and high-band
         filters */
                rl = rh = 0;
                tmp = 1;
                pi_gain[sub] = 0;
                for (i = 0; i <= lpcSize; i++)
                {
                    rh += tmp * interp_qlpc[i];
                    tmp = -tmp;
                    pi_gain[sub] += interp_qlpc[i];
                }
                rl = low_pi_gain[sub];
                rl = 1 / (Math.Abs(rl) + .01f);
                rh = 1 / (Math.Abs(rh) + .01f);
                /* Compute ratio, will help predict the gain */
                filter_ratio = Math.Abs(.01f + rh) / (.01f + Math.Abs(rl));

                fold = filter_ratio < 5 ? 1 : 0;
                /*printf ("filter_ratio %f\n", filter_ratio);*/
                fold = 0;
                /* Compute "real excitation" */
                Filters.fir_mem2(high, sp, interp_qlpc, excBuf, exc, subframeSize, lpcSize, mem_sp2);
                /* Compute energy of low-band and high-band excitation */
                for (i = 0; i < subframeSize; i++)
                    eh += excBuf[exc + i] * excBuf[exc + i];
                if (submodes[submodeID].innovation == null)
                {
                    float g;
                    /*speex_bits_pack(bits, 1, 1);*/
                    for (i = 0; i < subframeSize; i++)
                        el += low_innov[offset + i] * low_innov[offset + i];

                    /* Gain to use if we want to use the low-band excitation for high-band */
                    g = eh / (.01f + el);
                    g = (float)Math.Sqrt(g);

                    g *= filter_ratio;
                    int quant = (int)Math.Floor(.5 + 10 + 8.0 * Math.Log((g + .0001)));
                    /*speex_warning_int("tata", quant);*/
                    if (quant < 0)
                        quant = 0;
                    if (quant > 31)
                        quant = 31;
                    bits.Pack(quant, 5);
                    g = (float)(.1 * Math.Exp(quant / 9.4));
                    g /= filter_ratio;
                }
                else
                {
                    float gc, scale, scale_1;

                    for (i = 0; i < subframeSize; i++)
                        el += low_exc[offset + i] * low_exc[offset + i];
                    gc = (float)(Math.Sqrt(1 + eh) * filter_ratio / Math.Sqrt((1 + el) * subframeSize));
                    {
                        int qgc = (int)Math.Floor(.5 + 3.7 * (Math.Log(gc) + 2));
                        if (qgc < 0)
                            qgc = 0;
                        if (qgc > 15)
                            qgc = 15;
                        bits.Pack(qgc, 4);
                        gc = (float)Math.Exp((1 / 3.7) * qgc - 2);
                    }
                    scale = gc * (float)Math.Sqrt(1 + el) / filter_ratio;
                    scale_1 = 1 / scale;

                    for (i = 0; i < subframeSize; i++)
                        excBuf[exc + i] = 0;
                    excBuf[exc] = 1;
                    Filters.syn_percep_zero(excBuf, exc, interp_qlpc, bw_lpc1, bw_lpc2, syn_resp, subframeSize, lpcSize);

                    /* Reset excitation */
                    for (i = 0; i < subframeSize; i++)
                        excBuf[exc + i] = 0;

                    /* Compute zero response (ringing) of A(z/g1) / ( A(z/g2) * Aq(z) ) */
                    for (i = 0; i < lpcSize; i++)
                        mem[i] = mem_sp[i];
                    Filters.iir_mem2(excBuf, exc, interp_qlpc, excBuf, exc, subframeSize, lpcSize, mem);
                    for (i = 0; i < lpcSize; i++)
                        mem[i] = mem_sw[i];
                    Filters.filter_mem2(excBuf, exc, bw_lpc1, bw_lpc2, res, resp, subframeSize, lpcSize, mem, 0);

                    /* Compute weighted signal */
                    for (i = 0; i < lpcSize; i++)
                        mem[i] = mem_sw[i];
                    Filters.filter_mem2(high, sp, bw_lpc1, bw_lpc2, swBuf, sw, subframeSize, lpcSize, mem, 0);

                    /* Compute target signal */
                    for (i = 0; i < subframeSize; i++)
                        target[i] = swBuf[sw + i] - res[resp + i];

                    for (i = 0; i < subframeSize; i++)
                        excBuf[exc + i] = 0;

                    for (i = 0; i < subframeSize; i++)
                        target[i] *= scale_1;

                    /* Reset excitation */
                    for (i = 0; i < subframeSize; i++)
                        innov[i] = 0;
                    /*print_vec(target, st->subframeSize, "\ntarget");*/
                    submodes[submodeID].innovation.Quant(target, interp_qlpc, bw_lpc1, bw_lpc2,
                                                         lpcSize, subframeSize, innov, 0, syn_resp,
                                                         bits, (complexity + 1) >> 1);
                    /*print_vec(target, st->subframeSize, "after");*/

                    for (i = 0; i < subframeSize; i++)
                        excBuf[exc + i] += innov[i] * scale;
                    if(submodes[submodeID].double_codebook != 0)
                    {
                        float[] innov2 = new float[subframeSize];
                        for (i = 0; i < subframeSize; i++)
                            innov2[i] = 0;
                        for (i = 0; i < subframeSize; i++)
                            target[i] *= 2.5f;
                        submodes[submodeID].innovation.Quant(target, interp_qlpc, bw_lpc1, bw_lpc2,
                                                             lpcSize, subframeSize, innov2, 0, syn_resp,
                                                             bits, (complexity + 1) >> 1);
                        for (i = 0; i < subframeSize; i++)
                            innov2[i] *= scale * (1f / 2.5f);
                        for (i = 0; i < subframeSize; i++)
                            excBuf[exc + i] += innov2[i];
                    }
                }
                /*Keep the previous memory*/
                for (i = 0; i < lpcSize; i++)
                    mem[i] = mem_sp[i];
                /* Final signal synthesis from excitation */
                Filters.iir_mem2(excBuf, exc, interp_qlpc, high, sp, subframeSize, lpcSize, mem_sp);

                /* Compute weighted signal again, from synthesized speech (not sure it's the right thing) */
                Filters.filter_mem2(high, sp, bw_lpc1, bw_lpc2, swBuf, sw, subframeSize, lpcSize, mem_sw, 0);
            }
            //#ifndef RELEASE
            /* Reconstruct the original */
            filters.fir_mem_up(x0d, h0, y0, fullFrameSize, QMF_ORDER, g0_mem);
            filters.fir_mem_up(high, h1, y1, fullFrameSize, QMF_ORDER, g1_mem);

            for (i=0; i<fullFrameSize; i++)
              vin[i]=2*(y0[i]-y1[i]);
        //#endif
            for (i=0; i<lpcSize; i++)
              old_lsp[i] = lsp[i];
            for (i=0; i<lpcSize; i++)
              old_qlsp[i] = qlsp[i];
            first=0;
            return 1;
        }


        public int EncodedFrameSize
        {
            get
            {
                int size = SB_FRAME_SIZE[submodeID];
                size += lowenc.EncodedFrameSize;
                return size;
            }
        }

        public int Quality
        {
            set
            {
                int quality = value;
                if (quality < 0)
                {
                    quality = 0;
                }
                if (quality > 10)
                {
                    quality = 10;
                }
                if (uwb)
                {
                    lowenc.Quality = quality;
                    Mode =UWB_QUALITY_MAP[quality];
                }
                else
                {
                    lowenc.Mode = NB_QUALITY_MAP[quality];
                    Mode = WB_QUALITY_MAP[quality];
                }
            }
        }

        public int BitRate
        {
            get
            {
                if (submodes[submodeID] != null)
                {
                    return lowenc.BitRate + sampling_rate * submodes[submodeID].bits_per_frame / frameSize;
                }
                else
                {
                    return lowenc.BitRate + sampling_rate * (SB_SUBMODE_BITS + 1) / frameSize;
                }
                    
            }
            set
            {
                int bitrate = value;
                for (int i = 10; i >= 0; i--)
                {
                    Quality = i;
                    if (BitRate <= bitrate)
                        return;
                }
            }
        }

        public int Mode
        {
            get
            {
                return submodeID;
            }
            set
            {
                int mode = value;
                if (mode < 0)
                {
                    mode = 0;
                }
                submodeID = submodeSelect = mode;
            }
        }

        public bool Vbr
        {
            get
            {
                return vbr_enabled != 0;
            }
            set
            {
                bool vbr = value;
                vbr_enabled = vbr ? 1 : 0;
                lowenc.Vbr = vbr;
            }
        }

        public bool Vad
        {
            get
            {
                return vad_enabled != 0;
            }
            set
            {
                vad_enabled = value ? 1 : 0;
            }
        }

        public bool Dtx
        {
            get
            {
                return dtx_enabled != 0;
            }
            set
            {
                
                dtx_enabled = value ? 1 : 0;
            }
        }

        public int Abr
        {
            get
            {
                return abr_enabled;
            }
            set
            {
                int abr = value;
                lowenc.Vbr = true; 
                abr_enabled = (abr != 0) ? 1 : 0;
                vbr_enabled = 1;
                {
                    int i = 10, rate, target;
                    float vbr_qual;
                    target = abr;
                    while (i >= 0)
                    {
                        Quality = i;
                        rate = BitRate;
                        if (rate <= target)
                            break;
                        i--;
                    }
                    vbr_qual = i;
                    if (vbr_qual < 0)
                        vbr_qual = 0;
                    VbrQuality = vbr_qual;
                    abr_count = 0;
                    abr_drift = 0;
                    abr_drift2 = 0;
                }
            }
        }

        public float VbrQuality
        {
            get
            {
                return vbr_quality;
            }
            set
            {
                float quality = value;
                vbr_quality = quality;
                float qual = quality + 0.6f;
                if (qual > 10)
                    qual = 10;
                lowenc.VbrQuality = qual;

                int q = (int)Math.Floor(.5 + quality);
                if (q > 10)
                    q = 10;
                Quality = q;
            }
        }

        public int Complexity
        {
            get
            {
                return complexity;
            }
            set
            {
                int complexity2 = value;
                if (complexity2 < 0)
                    complexity2 = 0;
                if (complexity2 > 10)
                    complexity2 = 10;
                this.complexity = complexity2;
            }
        }

        public int SamleRate
        {
            get
            {
                return sampling_rate;
            }
            set
            {
                int rate = value;
                sampling_rate = rate;
                lowenc.SamleRate = rate;
            }
        }

        public int LoodAhead
        {
            get
            {
                return 2 * lowenc.LoodAhead + QMF_ORDER - 1;
            }
        }

        public float RelativeQuality
        {
            get { return this.relative_quality; }
        }

    }
}
