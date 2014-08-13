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
    /// Split shape codebook search
    /// </summary>
    public class SplitShapeSearch:CbSearch
    {
        public const int MaxComplexity = 10;

        private int _subFrameSize;
        private readonly int _subVectSize;
        private readonly int _nbSubvect;
        private readonly int[] _shapeCb;
        private readonly int _shapeCbSize;
        private readonly int _shapeBits;
        private readonly int _haveSign;
        private readonly int[] _ind;
        private readonly int[] _signs;
        private readonly float[] t;
        private readonly float[] e;
        private readonly float[] E;
        private readonly float[] r2;
        private float[][] _ot, _nt;
        private readonly int[][] _nind;
        private readonly int[][] _oind;

        public SplitShapeSearch(int subFrameSize, int subVectSize, int nbSubvect, int[] shapeCb, int shapeBits, int haveSign)
        {
            _subFrameSize = subFrameSize;
            _subVectSize = subVectSize;
            _nbSubvect   = nbSubvect;
            _shapeCb     = shapeCb;
            _shapeBits   = shapeBits;
            _haveSign    = haveSign;  
            _ind          = new int[nbSubvect]; 
            _signs        = new int[nbSubvect];
            _shapeCbSize = 1<<shapeBits;
            _ot=new float[MaxComplexity][];
            _nt=new float[MaxComplexity][];
            _oind=new int[MaxComplexity][];
            _nind=new int[MaxComplexity][];
            for (int i = 0; i < MaxComplexity; i++)
            {
                _ot[i] = new float[subFrameSize];
                _nt[i] = new float[subFrameSize];
                _oind[i] = new int[nbSubvect];
                _nind[i] = new int[nbSubvect];
                     
            }
            t = new float[subFrameSize];
            e = new float[subFrameSize];
            r2 = new float[subFrameSize];
            E = new float[_shapeCbSize];
        }

        /// <summary>
        /// Codebook Search Quantification (Split Shape).
        /// </summary>
        /// <param name="target"></param>
        /// <param name="ak">LPCs for this subframe</param>
        /// <param name="awk1">Weighted LPCs for this subframe</param>
        /// <param name="awk2">Weighted LPCs for this subframe</param>
        /// <param name="p">number of LPC coeffs</param>
        /// <param name="nsf">number of samples in subframe</param>
        /// <param name="exc">excitation array.</param>
        /// <param name="es">position in excitation array.</param>
        /// <param name="r"></param>
        /// <param name="bits">Speex bits buffer.</param>
        /// <param name="complexity"></param>
        public override sealed void Quant(float[] target, float[] ak, float[] awk1, float[] awk2,
                           int p, int nsf, float[] exc, int es, float[] r,
                          Bits bits, int complexity)
        {
            int i, j, k, m, n, q;
            float[] resp;
            float[] ndist, odist;
            int[] best_index;
            float[] best_dist;

            int N = complexity;
            if (N > 10)
                N = 10;

            resp = new float[_shapeCbSize * _subVectSize];

            best_index = new int[N];
            best_dist = new float[N];
            ndist = new float[N];
            odist = new float[N];

            for (i = 0; i < N; i++)
            {
                for (j = 0; j < _nbSubvect; j++)
                    _nind[i][j] = _oind[i][j] = -1;
            }

            for (j = 0; j < N; j++)
                for (i = 0; i < nsf; i++)
                    _ot[j][i] = target[i];

            //    System.arraycopy(target, 0, t, 0, nsf);

            /* Pre-compute codewords response and energy */
            for (i = 0; i < _shapeCbSize; i++)
            {
                int res;
                int shape;

                res = i * _subVectSize;
                shape = i * _subVectSize;

                /* Compute codeword response using convolution with impulse response */
                for (j = 0; j < _subVectSize; j++)
                {
                    resp[res + j] = 0;
                    for (k = 0; k <= j; k++)
                        resp[res + j] += 0.03125f * _shapeCb[shape + k] * r[j - k];
                }

                /* Compute codeword energy */
                E[i] = 0;
                for (j = 0; j < _subVectSize; j++)
                    E[i] += resp[res + j] * resp[res + j];
            }

            for (j = 0; j < N; j++)
                odist[j] = 0;
            /*For all subvectors*/
            for (i = 0; i < _nbSubvect; i++)
            {
                int offset = i * _subVectSize;
                /*"erase" nbest list*/
                for (j = 0; j < N; j++)
                    ndist[j] = -1;

                /*For all n-bests of previous subvector*/
                for (j = 0; j < N; j++)
                {
                    /*Find new n-best based on previous n-best j*/
                    if (_haveSign != 0)
                        VQ.nbest_sign(_ot[j], offset, resp, _subVectSize, _shapeCbSize, E, N, best_index, best_dist);
                    else
                        VQ.nbest(_ot[j], offset, resp, _subVectSize, _shapeCbSize, E, N, best_index, best_dist);

                    /*For all new n-bests*/
                    for (k = 0; k < N; k++)
                    {
                        float[] ct;
                        float err = 0;
                        ct = _ot[j];
                        /*update target*/

                        /*previous target*/
                        for (m = offset; m < offset + _subVectSize; m++)
                            t[m] = ct[m];

                        /* New code: update only enough of the target to calculate error*/
                        {
                            int rind;
                            int res;
                            float sign = 1;
                            rind = best_index[k];
                            if (rind >= _shapeCbSize)
                            {
                                sign = -1;
                                rind -= _shapeCbSize;
                            }
                            res = rind * _subVectSize;
                            if (sign > 0)
                                for (m = 0; m < _subVectSize; m++)
                                    t[offset + m] -= resp[res + m];
                            else
                                for (m = 0; m < _subVectSize; m++)
                                    t[offset + m] += resp[res + m];
                        }

                        /*compute error (distance)*/
                        err = odist[j];
                        for (m = offset; m < offset + _subVectSize; m++)
                            err += t[m] * t[m];
                        /*update n-best list*/
                        if (err < ndist[N - 1] || ndist[N - 1] < -.5)
                        {

                            /*previous target (we don't care what happened before*/
                            for (m = offset + _subVectSize; m < nsf; m++)
                                t[m] = ct[m];
                            /* New code: update the rest of the target only if it's worth it */
                            for (m = 0; m < _subVectSize; m++)
                            {
                                float g;
                                int rind;
                                float sign = 1;
                                rind = best_index[k];
                                if (rind >= _shapeCbSize)
                                {
                                    sign = -1;
                                    rind -= _shapeCbSize;
                                }

                                g = sign * 0.03125f * _shapeCb[rind * _subVectSize + m];
                                q = _subVectSize - m;
                                for (n = offset + _subVectSize; n < nsf; n++, q++)
                                    t[n] -= g * r[q];
                            }


                            for (m = 0; m < N; m++)
                            {
                                if (err < ndist[m] || ndist[m] < -.5)
                                {
                                    for (n = N - 1; n > m; n--)
                                    {
                                        for (q = offset + _subVectSize; q < nsf; q++)
                                            _nt[n][q] = _nt[n - 1][q];
                                        for (q = 0; q < _nbSubvect; q++)
                                            _nind[n][q] = _nind[n - 1][q];
                                        ndist[n] = ndist[n - 1];
                                    }
                                    for (q = offset + _subVectSize; q < nsf; q++)
                                        _nt[m][q] = t[q];
                                    for (q = 0; q < _nbSubvect; q++)
                                        _nind[m][q] = _oind[j][q];
                                    _nind[m][i] = best_index[k];
                                    ndist[m] = err;
                                    break;
                                }
                            }
                        }
                    }
                    if (i == 0)
                        break;
                }

                /*update old-new data*/
                /* just swap pointers instead of a long copy */
                {
                    float[][] tmp2;
                    tmp2 = _ot;
                    _ot = _nt;
                    _nt = tmp2;
                }
                for (j = 0; j < N; j++)
                    for (m = 0; m < _nbSubvect; m++)
                        _oind[j][m] = _nind[j][m];
                for (j = 0; j < N; j++)
                    odist[j] = ndist[j];
            }

            /*save indices*/
            for (i = 0; i < _nbSubvect; i++)
            {
                _ind[i] = _nind[0][i];
                bits.Pack(_ind[i], _shapeBits + _haveSign);
            }

            /* Put everything back together */
            for (i = 0; i < _nbSubvect; i++)
            {
                int rind;
                float sign = 1;
                rind = _ind[i];
                if (rind >= _shapeCbSize)
                {
                    sign = -1;
                    rind -= _shapeCbSize;
                }

                for (j = 0; j < _subVectSize; j++)
                    e[_subVectSize * i + j] = sign * 0.03125f * _shapeCb[rind * _subVectSize + j];
            }
            /* Update excitation */
            for (j = 0; j < nsf; j++)
                exc[es + j] += e[j];

            /* Update target */
            Filters.syn_percep_zero(e, 0, ak, awk1, awk2, r2, nsf, p);
            for (j = 0; j < nsf; j++)
                target[j] -= r2[j];
        }

        /// <summary>
        /// Codebook Search Unquantification (Split Shape).
        /// </summary>
        /// <param name="exc"> excitation array.</param>
        /// <param name="es">position in excitation array.</param>
        /// <param name="nsf">number of samples in subframe.</param>
        /// <param name="bits">Speex bits buffer.</param>
        public override sealed void UnQuant(float[] exc, int es, int nsf, Bits bits)
        {
            int i, j;

            /* Decode codewords and gains */
            for (i = 0; i < _nbSubvect; i++)
            {
                if (_haveSign != 0)
                    _signs[i] = bits.UnPack(1);
                else
                    _signs[i] = 0;
                _ind[i] = bits.UnPack(_shapeBits);
            }

            /* Compute decoded excitation */
            for (i = 0; i < _nbSubvect; i++)
            {
                float s = 1.0f;
                if (_signs[i] != 0)
                    s = -1.0f;
                for (j = 0; j < _subVectSize; j++)
                {
                    exc[es + _subVectSize * i + j] += s * 0.03125f * (float)_shapeCb[_ind[i] * _subVectSize + j];
                }
            }
        }

    }
}
