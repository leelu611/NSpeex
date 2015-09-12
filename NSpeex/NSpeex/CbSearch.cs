namespace NSpeex
{
    /// <summary>
    /// Abstract class that is the base for the various Codebook search methods.
    /// </summary>
    public abstract class CbSearch
    {
        /// <summary>
        /// Codebook Search Quantification.
        /// </summary>
        /// <param name="target">target vector</param>
        /// <param name="ak">LPCs for this subframe</param>
        /// <param name="awk1">Weighted LPCs for this subframe</param>
        /// <param name="awk2">Weighted LPCs for this subframe</param>
        /// <param name="p">number of LPC coeffs</param>
        /// <param name="nsf">number of samples in subframe</param>
        /// <param name="exc"> excitation array.</param>
        /// <param name="es">position in excitation array.</param>
        /// <param name="r"></param>
        /// <param name="bits">Speex bits buffer.</param>
        /// <param name="complexity">complexity</param>
        public abstract void Quant(float[] target, float[] ak, float[] awk1, float[] awk2,       
            int p, int nsf, float[] exc,int es, float[] r,                 
            Bits bits, int complexity);
        /// <summary>
        /// Codebook Search Unquantification.
        /// </summary>
        /// <param name="exc">excitation array.</param>
        /// <param name="es">position in excitation array.</param>
        /// <param name="nsf">number of samples in subframe.</param>
        /// <param name="bits">Speex bits buffer.</param>
        public abstract void UnQuant(float[] exc, int es, int nsf, Bits bits); 
    }
}
