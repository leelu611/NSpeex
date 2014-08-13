/*
  
   Copyright [2014] [alking of copyright morln]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License. 

 */

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
                             int p, int nsf, float[] exc, int es, float[] r,
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
