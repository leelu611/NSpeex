using System;

namespace NSpeex
{
    /// <summary>
    /// Abstract class that is the base for the various LTP (Long Term Prediction)
    /// Quantisation and Unquantisation methods.
    /// </summary>
    public abstract class Ltp
    {
        /// <summary>
        /// Long Term Prediction Quantification.
        /// </summary>
        /// <param name="complexity">
        /// pitch
        /// </param>
        /// <returns></returns>
        public  abstract int Quant(float[] target, float[] sw, int sws, float[] ak, float[] awk1, float[] awk2,
                            float[] exc, int es, int start, int end, float pitch_coef, int p,
                            int nsf, Bits bits, float[] exc2, int e2s, float[] r, int complexity);
        /// <summary>
        /// Long Term Prediction Unquantification.
        /// </summary>
        /// <param name="exc">
        /// Excitation
        /// </param>
        /// <param name="es">
        /// Excitation offset
        /// </param>
        /// <param name="start">
        /// Smallest pitch value allowed
        /// </param>
        /// <param name="pitch_coef">
        /// pitch_coef - Voicing (pitch) coefficient
        /// </param>
        /// <param name="nsf">
        /// Number of samples in subframe
        /// </param>
        /// <param name="gain_val">
        /// 
        /// </param>
        /// <param name="bits"></param>
        /// <param name="count_lost"></param>
        /// <param name="subframe_offset"></param>
        /// <param name="last_pitch_gain"></param>
        /// <returns>
        /// pitch
        /// </returns>
        public abstract int UnQuant(float[] exc, int es, int start, float pitch_coef,
                              int nsf, float[] gain_val, Bits bits,
                              int count_lost, int subframe_offset, float last_pitch_gain);
        /// <summary>
        /// Calculates the inner product of the given vectors.
        /// </summary>
        /// <param name="x">
        /// first vector.
        /// </param>
        /// <param name="xs">
        /// offset of the first vector.
        /// </param>
        /// <param name="y">
        ///  second vector.
        /// </param>
        /// <param name="ys">
        /// offset of the second vector.
        /// </param>
        /// <param name="len">
        /// length of the vectors.
        /// </param>
        /// <returns>
        /// the inner product of the given vectors.
        /// </returns>
        public  static float inner_prod(float[] x, int xs, float[] y, int ys, int len)
        {
            int i;
            float sum1 = 0, sum2 = 0, sum3 = 0, sum4 = 0;
            for (i = 0; i < len; )
            {
                sum1 += x[xs + i] * y[ys + i];
                sum2 += x[xs + i + 1] * y[ys + i + 1];
                sum3 += x[xs + i + 2] * y[ys + i + 2];
                sum4 += x[xs + i + 3] * y[ys + i + 3];
                i += 4;
            }
            return sum1 + sum2 + sum3 + sum4;
        }

        /// <summary>
        /// Find the n-best pitch in Open Loop.
        /// </summary>
        public  static void open_loop_nbest_pitch(float[] sw,
                                               int swIdx,
                                               int start,
                                               int end,
                                               int len,
                                               int[] pitch,
                                               float[] gain,
                                               int N)
        {
            int i, j, k;
            /*float corr=0;*/
            /*float energy;*/
            float[] best_score;
            float e0;
            float[] corr, energy, score;

            best_score = new float[N];
            corr = new float[end - start + 1];
            energy = new float[end - start + 2];
            score = new float[end - start + 1];
            for (i = 0; i < N; i++)
            {
                best_score[i] = -1;
                gain[i] = 0;
                pitch[i] = start;
            }
            energy[0] = inner_prod(sw, swIdx - start, sw, swIdx - start, len);
            e0 = inner_prod(sw, swIdx, sw, swIdx, len);
            for (i = start; i <= end; i++)
            {
                /* Update energy for next pitch*/
                energy[i - start + 1] = energy[i - start] + sw[swIdx - i - 1] * sw[swIdx - i - 1] - sw[swIdx - i + len - 1] * sw[swIdx - i + len - 1];
                if (energy[i - start + 1] < 1)
                    energy[i - start + 1] = 1;
            }
            for (i = start; i <= end; i++)
            {
                corr[i - start] = 0;
                score[i - start] = 0;
            }

            for (i = start; i <= end; i++)
            {
                /* Compute correlation*/
                corr[i - start] = inner_prod(sw, swIdx, sw, swIdx - i, len);
                score[i - start] = corr[i - start] * corr[i - start] / (energy[i - start] + 1);
            }
            for (i = start; i <= end; i++)
            {
                if (score[i - start] > best_score[N - 1])
                {
                    float g1, g;
                    g1 = corr[i - start] / (energy[i - start] + 10);
                    g = (float)Math.Sqrt(g1 * corr[i - start] / (e0 + 10));
                    if (g > g1)
                        g = g1;
                    if (g < 0)
                        g = 0;
                    for (j = 0; j < N; j++)
                    {
                        if (score[i - start] > best_score[j])
                        {
                            for (k = N - 1; k > j; k--)
                            {
                                best_score[k] = best_score[k - 1];
                                pitch[k] = pitch[k - 1];
                                gain[k] = gain[k - 1];
                            }
                            best_score[j] = score[i - start];
                            pitch[j] = i;
                            gain[j] = g;
                            break;
                        }
                    }
                }
            }

        }

    }
}
