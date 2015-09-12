using System;

namespace NSpeex
{
    /// <summary>
    /// Narrowband Speex Decoder
    /// </summary>
    public class NbDecoder:NbCodec,IDecoder
    {
        private float[] innov2;
        private int count_lost;
        private int last_pitch;
        private float last_pitch_gain;
        private float[] pitch_gain_buf;
        private int pitch_gain_buf_idx;
        private float last_ol_gain;
        protected Random random = new Random();
        protected Stereo stereo;
        protected Inband inband;
        protected bool enhanced;
        public NbDecoder()
        {
            stereo = new Stereo();
            inband = new Inband(stereo);
            enhanced = true;
        }

        public override void init(int frameSize, int subframeSize, int lpcSize, int bufSize)
        {
            base.init(frameSize, subframeSize, lpcSize, bufSize);
            filters.init();
            innov2 = new float[40];

            count_lost = 0;
            last_pitch = 40;
            last_pitch_gain = 0;
            pitch_gain_buf = new float[3];
            pitch_gain_buf_idx = 0;
            last_ol_gain = 0;
        }

        /// <summary>
        /// Decode the given input bits.
        /// </summary>
        /// <param name="bits">Speex bits buffer.</param>
        /// <param name="vout">the decoded mono audio frame.</param>
        /// <returns>1 if a terminator was found, 0 if not.</returns>
        public int Decode(Bits bits, float[] vout)
        {
            int i, sub, pitch, ol_pitch = 0, m;
            float[] pitch_gain = new float[3];
            float ol_gain = 0.0f, ol_pitch_coef = 0.0f;
            int best_pitch = 40;
            float best_pitch_gain = 0;
            float pitch_average = 0;
            if (bits == null && dtx_enabled != 0)
            {
                submodeID = 0;
            }
            else
            {
                if (bits == null)
                {
                    decodeLost(vout);
                    return 0;
                }
                do 
                {
                    if (bits.UnPack(1) != 0)
                    {
                        m = bits.UnPack(SbCodec.SB_SUBMODE_BITS);
                        int advance = SbCodec.SB_FRAME_SIZE[m];
                        if (advance < 0)
                        {
                            throw new Exception("Invalid sideband mode encountered (1st sideband): " + m);
                        }
                        advance -= (SbCodec.SB_SUBMODE_BITS + 1);
                        bits.Advance(advance);
                        if (bits.UnPack(1) != 0)
                        { /* Skip ultra-wideband block (for compatibility) */
                            /* Get the sub-mode that was used */
                            m = bits.UnPack(SbCodec.SB_SUBMODE_BITS);
                            advance = SbCodec.SB_FRAME_SIZE[m];
                            if (advance < 0)
                            {
                                throw new Exception("Invalid sideband mode encountered. (2nd sideband): " + m);
                                //return -2;
                            }
                            advance -= (SbCodec.SB_SUBMODE_BITS + 1);
                            bits.Advance(advance);
                            if (bits.UnPack(1) != 0)
                            { /* Sanity check */
                                throw new Exception("More than two sideband layers found");
                                //return -2;
                            }
                        }
                    }

                    m = bits.UnPack(NB_SUBMODE_BITS);
                    if (m == 15)
                    { /* We found a terminator */
                        return 1;
                    }
                    else if (m == 14)
                    { /* Speex in-band request */
                        inband.speexInbandRequest(bits);
                    }
                    else if (m == 13)
                    { /* User in-band request */
                        inband.userInbandRequest(bits);
                    }
                    else if (m > 8)
                    { /* Invalid mode */
                        throw new Exception("Invalid mode encountered: " + m);
                        //return -2;
                    }


                }
                while (m > 8);
                submodeID = m;
            }
            /* Shift all buffers by one frame */
            System.Array.Copy(frmBuf, frameSize, frmBuf, 0, bufSize - frameSize);
            System.Array.Copy(excBuf, frameSize, excBuf, 0, bufSize - frameSize);
            if (submodes[submodeID] == null)
            {
                Filters.bw_lpc(.93f, interp_qlpc, lpc, 10);
                float innov_gain = 0;
                for (i = 0; i < frameSize; i++)
                    innov_gain += innov[i] * innov[i];
                innov_gain = (float)Math.Sqrt(innov_gain / frameSize);
                for (i = excIdx; i < excIdx + frameSize; i++)
                {
                    excBuf[i] = 3 * innov_gain * ((float)random.NextDouble() - .5f);
                }
                first = 1;
                /* Final signal synthesis from excitation */
                Filters.iir_mem2(excBuf, excIdx, lpc, frmBuf, frmIdx, frameSize, lpcSize, mem_sp);
                vout[0] = frmBuf[frmIdx] + preemph * pre_mem;
                for (i=1;i<frameSize;i++)
                    vout[i]=frmBuf[frmIdx+i] + preemph*vout[i-1];
                pre_mem=vout[frameSize-1];
                count_lost=0;
                return 0;
            }
            /* Unquantize LSPs */
            submodes[submodeID].lsqQuant.unquant(qlsp, lpcSize, bits);
            if (count_lost != 0)
            {
                float lsp_dist = 0, fact;
                for (i = 0; i < lpcSize; i++)
                    lsp_dist += Math.Abs(old_qlsp[i] - qlsp[i]);
                fact = (float)(.6 * Math.Exp(-.2 * lsp_dist));
                for (i = 0; i < 2 * lpcSize; i++)
                    mem_sp[i] *= fact;
            }
            /* Handle first frame and lost-packet case */
            if (first != 0 || count_lost != 0)
            {
                for (i = 0; i < lpcSize; i++)
                    old_qlsp[i] = qlsp[i];
            }

            /* Get open-loop pitch estimation for low bit-rate pitch coding */
            if (submodes[submodeID].lbr_pitch != -1)
            {
                ol_pitch = min_pitch + bits.UnPack(7);
            }

            if (submodes[submodeID].forced_pitch_gain != 0)
            {
                int quant = bits.UnPack(4);
                ol_pitch_coef = 0.066667f * quant;
            }
            /* Get global excitation gain */
            int qe = bits.UnPack(5);
            ol_gain = (float)Math.Exp(qe / 3.5);

            /* unpacks unused dtx bits */
            if (submodeID == 1)
            {
                int extra = bits.UnPack(4);
                if (extra == 15)
                    dtx_enabled = 1;
                else
                    dtx_enabled = 0;
            }
            if (submodeID > 1)
                dtx_enabled = 0;
            /*Loop on subframes */
            for (sub = 0; sub < nbSubframes; sub++)
            {
                int offset, spIdx, extIdx;
                float tmp;
                /* Offset relative to start of frame */
                offset = subframeSize * sub;
                /* Original signal */
                spIdx = frmIdx + offset;
                /* Excitation */
                extIdx = excIdx + offset;

                /* LSP interpolation (quantized and unquantized) */
                tmp = (1.0f + sub) / nbSubframes;
                for (i = 0; i < lpcSize; i++)
                    interp_qlsp[i] = (1 - tmp) * old_qlsp[i] + tmp * qlsp[i];

                /* Make sure the LSP's are stable */
                Lsp.enforce_margin(interp_qlsp, lpcSize, .002f);

                /* Compute interpolated LPCs (unquantized) */
                for (i = 0; i < lpcSize; i++)
                    interp_qlsp[i] = (float)Math.Cos(interp_qlsp[i]);
                m_lsp.lsp2lpc(interp_qlsp, interp_qlpc, lpcSize);
                /* Compute enhanced synthesis filter */
                if (enhanced)
                {
                    float r = .9f;
                    float k1, k2, k3;

                    k1 = submodes[submodeID].lpc_enh_k1;
                    k2 = submodes[submodeID].lpc_enh_k2;
                    k3 = (1 - (1 - r * k1) / (1 - r * k2)) / r;
                    Filters.bw_lpc(k1, interp_qlpc, awk1, lpcSize);
                    Filters.bw_lpc(k2, interp_qlpc, awk2, lpcSize);
                    Filters.bw_lpc(k3, interp_qlpc, awk3, lpcSize);
                }
                /* Compute analysis filter at w=pi */
                tmp = 1;
                pi_gain[sub] = 0;
                for (i = 0; i <= lpcSize; i++)
                {
                    pi_gain[sub] += tmp * interp_qlpc[i];
                    tmp = -tmp;
                }
                /* Reset excitation */
                for (i = 0; i < subframeSize; i++)
                    excBuf[extIdx + i] = 0;

                /*Adaptive codebook contribution*/
                int pit_min, pit_max;
                /* Handle pitch constraints if any */
                if (submodes[submodeID].lbr_pitch != -1)
                {
                    int margin = submodes[submodeID].lbr_pitch;
                    if (margin != 0)
                    {
                        pit_min = ol_pitch - margin + 1;
                        if (pit_min < min_pitch)
                            pit_min = min_pitch;
                        pit_max = ol_pitch + margin;
                        if (pit_max > max_pitch)
                            pit_max = max_pitch;
                    }
                    else
                    {
                        pit_min = pit_max = ol_pitch;
                    }
                }
                else
                {
                    pit_min = min_pitch;
                    pit_max = max_pitch;
                }
                /* Pitch synthesis */
                pitch = submodes[submodeID].ltp.UnQuant(excBuf, extIdx, pit_min, ol_pitch_coef,
                                                        subframeSize, pitch_gain, bits,
                                                        count_lost, offset, last_pitch_gain);
                /* If we had lost frames, check energy of last received frame */
                if (count_lost != 0 && ol_gain < last_ol_gain)
                {
                    float fact = ol_gain / (last_ol_gain + 1);
                    for (i = 0; i < subframeSize; i++)
                        excBuf[excIdx + i] *= fact;
                }
                tmp = Math.Abs(pitch_gain[0] + pitch_gain[1] + pitch_gain[2]);
                tmp = Math.Abs(pitch_gain[1]);
                if (pitch_gain[0] > 0)
                    tmp += pitch_gain[0];
                else
                    tmp -= .5f * pitch_gain[0];
                if (pitch_gain[2] > 0)
                    tmp += pitch_gain[2];
                else
                    tmp -= .5f * pitch_gain[0];

                pitch_average += tmp;
                if (tmp > best_pitch_gain)
                {
                    best_pitch = pitch;
                    best_pitch_gain = tmp;
                }
                /* Unquantize the innovation */
                int q_energy, ivi = sub * subframeSize;
                float ener;

                for (i = ivi; i < ivi + subframeSize; i++)
                    innov[i] = 0.0f;
                /* Decode sub-frame gain correction */
                if (submodes[submodeID].have_subframe_gain == 3)
                {
                    q_energy = bits.UnPack(3);
                    ener = (float)(ol_gain * Math.Exp(exc_gain_quant_scal3[q_energy]));
                }
                else if (submodes[submodeID].have_subframe_gain == 1)
                {
                    q_energy = bits.UnPack(1);
                    ener = (float)(ol_gain * Math.Exp(exc_gain_quant_scal1[q_energy]));
                }
                else
                {
                    ener = ol_gain;
                }
                if (submodes[submodeID].innovation != null)
                {
                    /* Fixed codebook contribution */
                    submodes[submodeID].innovation.UnQuant(innov, ivi, subframeSize, bits);
                }
                /* De-normalize innovation and update excitation */
                for (i = ivi; i < ivi + subframeSize; i++)
                    innov[i] *= ener;
                /*  Vocoder mode */
                if (submodeID == 1)
                {
                    float g = ol_pitch_coef;

                    for (i = 0; i < subframeSize; i++)
                        excBuf[extIdx + i] = 0;
                    while (voc_offset < subframeSize)
                    {
                        if (voc_offset >= 0)
                            excBuf[extIdx + voc_offset] = (float)Math.Sqrt(1.0f * ol_pitch);
                        voc_offset += ol_pitch;
                    }
                    voc_offset -= subframeSize;

                    g = .5f + 2 * (g - .6f);
                    if (g < 0)
                        g = 0;
                    if (g > 1)
                        g = 1;
                    for (i = 0; i < subframeSize; i++)
                    {
                        float itmp = excBuf[extIdx + i];
                        excBuf[extIdx + i] = .8f * g * excBuf[extIdx + i] * ol_gain + .6f * g * voc_m1 * ol_gain + .5f * g * innov[ivi + i] - .5f * g * voc_m2 + (1 - g) * innov[ivi + i];
                        voc_m1 = itmp;
                        voc_m2 = innov[ivi + i];
                        voc_mean = .95f * voc_mean + .05f * excBuf[extIdx + i];
                        excBuf[extIdx + i] -= voc_mean;
                    }
                }
                else
                {
                    for (i = 0; i < subframeSize; i++)
                        excBuf[extIdx + i] += innov[ivi + i];
                }
                /* Decode second codebook (only for some modes) */
                if (submodes[submodeID].double_codebook != 0)
                {
                    for (i = 0; i < subframeSize; i++)
                        innov2[i] = 0;
                    submodes[submodeID].innovation.UnQuant(innov2, 0, subframeSize, bits);
                    for (i = 0; i < subframeSize; i++)
                        innov2[i] *= ener * (1f / 2.2f);
                    for (i = 0; i < subframeSize; i++)
                        excBuf[extIdx + i] += innov2[i];
                }
                for (i = 0; i < subframeSize; i++)
                    frmBuf[spIdx + i] = excBuf[extIdx + i];
                /* Signal synthesis */
                if (enhanced && submodes[submodeID].comb_gain > 0)
                {
                    filters.comb_filter(excBuf, extIdx, frmBuf, spIdx, subframeSize,
                                        pitch, pitch_gain, submodes[submodeID].comb_gain);
                }
                if (enhanced)
                {
                    /* Use enhanced LPC filter */
                    Filters.filter_mem2(frmBuf, spIdx, awk2, awk1, subframeSize, lpcSize, mem_sp, lpcSize);
                    Filters.filter_mem2(frmBuf, spIdx, awk3, interp_qlpc, subframeSize, lpcSize, mem_sp, 0);
                }
                else
                {
                    /* Use regular filter */
                    for (i = 0; i < lpcSize; i++)
                        mem_sp[lpcSize + i] = 0;
                    Filters.iir_mem2(frmBuf, spIdx, interp_qlpc, frmBuf, spIdx, subframeSize, lpcSize, mem_sp);
                }
            }
            /*Copy output signal*/
            vout[0] = frmBuf[frmIdx] + preemph * pre_mem;
            for (i=1;i<frameSize;i++)
                vout[i] = frmBuf[frmIdx+i] + preemph * vout[i-1];
            pre_mem = vout[frameSize-1];
    
            /* Store the LSPs for interpolation in the next frame */
            for (i=0;i<lpcSize;i++)
                 old_qlsp[i] = qlsp[i];
            /* The next frame will not be the first (Duh!) */
            first = 0;
            count_lost=0;
            last_pitch = best_pitch;
            last_pitch_gain = .25f*pitch_average;
            pitch_gain_buf[pitch_gain_buf_idx++] = last_pitch_gain;
            if (pitch_gain_buf_idx > 2) /* rollover */
              pitch_gain_buf_idx = 0;
            last_ol_gain = ol_gain;
            return 0;
        }

        public int decodeLost(float[] vout)
        {
            int i;
            float pitch_gain, fact, gain_med;

            fact = (float) Math.Exp(-.04*count_lost*count_lost);
            // median3(a, b, c) = (a<b ? (b<c ? b : (a<c ? c : a))
            //                         : (c<b ? b : (c<a ? c : a)))
            gain_med = (pitch_gain_buf[0] < pitch_gain_buf[1] ? (pitch_gain_buf[1] < pitch_gain_buf[2] ? pitch_gain_buf[1] : (pitch_gain_buf[0] < pitch_gain_buf[2] ? pitch_gain_buf[2] : pitch_gain_buf[0]))
                                                                : (pitch_gain_buf[2] < pitch_gain_buf[1] ? pitch_gain_buf[1] : (pitch_gain_buf[2] < pitch_gain_buf[0] ? pitch_gain_buf[2] : pitch_gain_buf[0])));
            if (gain_med < last_pitch_gain)
                last_pitch_gain = gain_med;

            pitch_gain = last_pitch_gain;
            if (pitch_gain>.95f)
                pitch_gain=.95f;

            pitch_gain *= fact;
   
            /* Shift all buffers by one frame */
            System.Array.Copy(frmBuf, frameSize, frmBuf, 0, bufSize-frameSize);
            System.Array.Copy(excBuf, frameSize, excBuf, 0, bufSize-frameSize);

            for (int sub=0; sub<nbSubframes; sub++)
            {
                int offset;
                int spIdx, extIdx;
                /* Offset relative to start of frame */
                offset = subframeSize*sub;
                /* Original signal */
                spIdx  = frmIdx+offset;
                /* Excitation */
                extIdx = excIdx+offset;
                /* Excitation after post-filter*/

                /* Calculate perceptually enhanced LPC filter */
                if (enhanced) {
                float r=.9f;   
                float k1,k2,k3;
                if (submodes[submodeID] != null) {
                    k1=submodes[submodeID].lpc_enh_k1;
                    k2=submodes[submodeID].lpc_enh_k2;
                }
                else {
                    k1 = k2 = 0.7f;
                }
                k3=(1-(1-r*k1)/(1-r*k2))/r;
                Filters.bw_lpc(k1, interp_qlpc, awk1, lpcSize);
                Filters.bw_lpc(k2, interp_qlpc, awk2, lpcSize);
                Filters.bw_lpc(k3, interp_qlpc, awk3, lpcSize);
                }
                /* Make up a plausible excitation */
                /* THIS CAN BE IMPROVED */
                /*if (pitch_gain>.95)
                pitch_gain=.95;*/
                {
                float innov_gain=0;
                for (i=0; i<frameSize; i++)
                    innov_gain += innov[i]*innov[i];
                innov_gain = (float) Math.Sqrt(innov_gain/frameSize);
                for (i=0; i<subframeSize; i++)
                {
        //#if 0
        //          excBuf[extIdx+i] = pitch_gain*excBuf[extIdx+i-last_pitch] + fact*((float)Math.sqrt(1-pitch_gain))*innov[i+offset];
        //          /*Just so it give the same lost packets as with if 0*/
        //          /*rand();*/
        //#else
                    /*excBuf[extIdx+i] = pitch_gain*excBuf[extIdx+i-last_pitch] + fact*innov[i+offset];*/
                    excBuf[extIdx+i] = pitch_gain*excBuf[extIdx+i-last_pitch] + fact*((float)Math.Sqrt(1-pitch_gain))*3*innov_gain*(((float)random.NextDouble())-0.5f);
        //#endif
                }
                }
                for (i=0;i<subframeSize;i++)
                frmBuf[spIdx+i]=excBuf[extIdx+i];

                /* Signal synthesis */
                if (enhanced) {
                /* Use enhanced LPC filter */
                Filters.filter_mem2(frmBuf, spIdx, awk2, awk1, subframeSize, lpcSize, mem_sp, lpcSize);
                Filters.filter_mem2(frmBuf, spIdx, awk3, interp_qlpc, subframeSize, lpcSize, mem_sp, 0);
                }
                else {
                /* Use regular filter */
                for (i=0;i<lpcSize;i++)
                    mem_sp[lpcSize+i] = 0;
                Filters.iir_mem2(frmBuf, spIdx, interp_qlpc, frmBuf, spIdx, subframeSize, lpcSize, mem_sp);
                }
            }

            vout[0] = frmBuf[0] + preemph*pre_mem;
            for (i=1;i<frameSize;i++)
                vout[i] = frmBuf[i] + preemph*vout[i-1];
            pre_mem=vout[frameSize-1];
            first = 0;
            count_lost++;
            pitch_gain_buf[pitch_gain_buf_idx++] = pitch_gain;
            if (pitch_gain_buf_idx > 2) /* rollover */
                pitch_gain_buf_idx = 0;

            return 0;
        }

        public void DecodeStereo(float[] data, int frameSize)
        {
            stereo.decode(data, frameSize);
        }

        public bool PerceptualEnhancement
        {
            get { return enhanced; }
            set { enhanced = value; }
        }


        public bool Dtx
        {
            get { return dtx_enabled != 0; }
        }
    }
}
