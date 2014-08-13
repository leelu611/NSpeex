namespace NSpeex
{
    public abstract class LspQuant:Codebook
    {
        public const  int MAX_LSP_SIZE       = 20;
        protected LspQuant()
        {
        }
        /// <summary>
        /// Line Spectral Pair Quantification.
        /// </summary>
        /// <param name="lsp">
        /// Line Spectral Pairs table.
        /// </param>
        /// <param name="qlsp">
        /// Quantified Line Spectral Pairs table.
        /// </param>
        /// <param name="order">
        /// 
        /// </param>
        /// <param name="bits">
        /// Speex bits buffer.
        /// </param>
        public abstract void quant( float[] lsp,float[] qlsp,int order,Bits bits);
        /// <summary>
        /// Line Spectral Pair Unquantification.
        /// </summary>
        /// <param name="lsp">
        /// Line Spectral Pairs table.
        /// </param>
        /// <param name="order"></param>
        /// <param name="bits">
        /// peex bits buffer.
        /// </param>
        public abstract void unquant(float[] lsp, int order, Bits bits);
        /// <summary>
        /// Read the next 6 bits from the buffer, and using the value read and the given codebook, rebuild LSP table.
        /// </summary>
        /// <param name="lsp"></param>
        /// <param name="tab"></param>
        /// <param name="bits"></param>
        /// <param name="k"></param>
        /// <param name="ti"></param>
        /// <param name="li"></param>
        protected void unpackPlus(float[] lsp, int[] tab, Bits bits, float k, int ti, int li)
        {
            int id = bits.UnPack(6);
            for (int i = 0; i < ti; i++)
                lsp[i + li] += k * (float)tab[id * ti + i];
        }
        /// <summary>
        /// LSP quantification
        /// </summary>
        /// <param name="x"></param>
        /// <param name="xs"></param>
        /// <param name="cdbk"></param>
        /// <param name="nbVec"></param>
        /// <param name="nbDim"></param>
        /// <returns>
        /// the index of the best match in the codebook
        /// (NB x is also modified).
        /// </returns>
        protected static int lsp_quant(float[] x, int xs, int[] cdbk, int nbVec, int nbDim)
        {
            int i, j;
            float dist, tmp;
            float best_dist = 0;
            int best_id = 0;
            int ptr = 0;
            for (i = 0; i < nbVec; i++)
            {
                dist = 0;
                for (j = 0; j < nbDim; j++)
                {
                    tmp = (x[xs + j] - cdbk[ptr++]);
                    dist += tmp * tmp;
                }
                if (dist < best_dist || i == 0)
                {
                    best_dist = dist;
                    best_id = i;
                }
            }

            for (j = 0; j < nbDim; j++)
                x[xs + j] -= cdbk[best_id * nbDim + j];

            return best_id;
        }
        /// <summary>
        /// LSP weighted quantification
        /// </summary>
        /// <param name="x"></param>
        /// <param name="xs"></param>
        /// <param name="weight"></param>
        /// <param name="ws"></param>
        /// <param name="cdbk"></param>
        /// <param name="nbVec"></param>
        /// <param name="nbDim"></param>
        /// <returns>
        /// the index of the best match in the codebook
        /// (NB x is also modified).
        /// </returns>
        protected static int lsp_weight_quant(float[] x, int xs, float[] weight, int ws, int[] cdbk, int nbVec, int nbDim)
        {
            int i, j;
            float dist, tmp;
            float best_dist = 0;
            int best_id = 0;
            int ptr = 0;
            for (i = 0; i < nbVec; i++)
            {
                dist = 0;
                for (j = 0; j < nbDim; j++)
                {
                    tmp = (x[xs + j] - cdbk[ptr++]);
                    dist += weight[ws + j] * tmp * tmp;
                }
                if (dist < best_dist || i == 0)
                {
                    best_dist = dist;
                    best_id = i;
                }
            }
            for (j = 0; j < nbDim; j++)
                x[xs + j] -= cdbk[best_id * nbDim + j];
            return best_id;
        }


    }
}
