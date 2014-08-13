using System;

namespace NSpeex
{
    /// <summary>
    /// Noise codebook search
    /// </summary>
    public class NoiseSearch:CbSearch
    {
        /// <summary>
        /// Codebook Search Quantification (Noise).
        /// </summary>
        /// <param name="target">target vector</param>
        /// <param name="ak">LPCs for this subframe</param>
        /// <param name="awk1">Weighted LPCs for this subframe</param>
        /// <param name="awk2">Weighted LPCs for this subframe</param>
        /// <param name="p">number of LPC coeffs</param>
        /// <param name="nsf">number of samples in subframe</param>
        /// <param name="exc">excitation array.</param>
        /// <param name="es">position in excitation array.</param>
        /// <param name="r"></param>
        /// <param name="bits">Speex bits buffer.</param>
        /// <param name="complexity">complexity</param>
        public override sealed void quant(float[] target, float[] ak, float[] awk1, float[] awk2,
                          int p, int nsf, float[] exc, int es, float[] r,
                          Bits bits, int complexity)
        {
            int i;
            float[] tmp = new float[nsf];
            Filters.residue_percep_zero(target, 0, ak, awk1, awk2, tmp, nsf, p);

            for (i = 0; i < nsf; i++)
                exc[es + i] += tmp[i];
            for (i = 0; i < nsf; i++)
                target[i] = 0;
        }
        /// <summary>
        /// Codebook Search Unquantification (Noise).
        /// </summary>
        /// <param name="exc">excitation array.</param>
        /// <param name="es">position in excitation array.</param>
        /// <param name="nsf">number of samples in subframe.</param>
        /// <param name="bits">Speex bits buffer.</param>
        public override sealed void unquant(float[] exc, int es, int nsf, Bits bits)
        {
            for (int i = 0; i < nsf; i++)
            {
                exc[es + i] += (float)(3.0 * (new Random().NextDouble() - .5f));
            }
        }

    }
}
