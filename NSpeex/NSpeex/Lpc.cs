namespace NSpeex
{
    /// <summary>
    /// LPC - and Reflection Coefficients.
    /// The next two functions calculate linear prediction coefficients
    /// and/or the related reflection coefficients from the first P_MAX+1
    /// values of the autocorrelation function.
    /// 
    /// 
    /// </summary>
    public class Lpc
    {
        /// <summary>
        /// Returns minimum mean square error.
        /// </summary>
        /// <param name="lpc">
        /// float[0...p-1] LPC coefficients
        /// </param>
        /// <param name="ac">
        /// in: float[0...p] autocorrelation values
        /// </param>
        /// <param name="reffer">
        /// out: float[0...p-1] reflection coef's
        /// </param>
        /// <param name="p"></param>
        /// <returns>
        /// minimum mean square error.
        /// </returns>
        public static float wld(float[] lpc, float[] ac, float[] reffer, int p)
        {
                int i, j;
                float r, error = ac[0];
                if (ac[0] == 0) {
                  for (i=0; i<p; i++)
                    reffer[i] = 0;
                  return 0;
                }
                for (i = 0; i < p; i++) {
                  /* Sum up this iteration's reflection coefficient. */
                  r = -ac[i + 1];
                  for (j = 0; j < i; j++) r -= lpc[j] * ac[i - j];
                  reffer[i] = r /= error;
                  /*  Update LPC coefficients and total error. */
                  lpc[i] = r;
                  for (j = 0; j < i/2; j++) {
                    float tmp  = lpc[j];
                    lpc[j]     += r * lpc[i-1-j];
                    lpc[i-1-j] += r * tmp;
                  }
                  if ((i % 2) != 0)
                    lpc[j] += lpc[j] * r;
                  error *= 1.0f - r * r;
                }
                return error;
        }
        /// <summary>
        /// Compute the autocorrelation
        ///         ,--,
        /// ac(i) = >  x(n) * x(n-i)  for all n
        ///         `--'
        /// for lags between 0 and lag-1, and x == 0 outside 0...n-1
        /// </summary>
        /// <param name="x">in: float[0...n-1] samples x</param>
        /// <param name="ac">out: float[0...lag-1] ac values</param>
        /// <param name="lag"></param>
        /// <param name="n"></param>
        public static void autocorr(float[] x, float[] ac, int lag, int n)
        {
            float d;
            int i;
            while (lag-- > 0)
            {
                for (i = lag, d = 0; i < n; i++)
                    d += x[i] * x[i - lag];
                ac[lag] = d;
            }
        }
    
        
    }
}
