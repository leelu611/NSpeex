namespace NSpeex
{
    /// <summary>
    /// Vector Quantization.
    /// </summary>
    public class VQ
    {
        /// <summary>
        /// Finds the index of the entry in a codebook that best matches the input.
        /// </summary>
        /// <param name="vin">the value to compare.</param>
        /// <param name="codebook">the list of values to search through for the best match.</param>
        /// <param name="entries">the size of the codebook.</param>
        /// <returns>the index of the entry in a codebook that best matches the input.</returns>
        public static int index(float vin, float[] codebook, int entries)
        {
            int i;
            float min_dist=0;
            int best_index=0;
            for (i=0;i<entries;i++)
            {
              float dist = vin-codebook[i];
              dist = dist*dist;
              if (i==0 || dist<min_dist)
              {
                min_dist=dist;
                best_index=i;
              }
            }
            return best_index;
        }

        /// <summary>
        /// Finds the index of the entry in a codebook that best matches the input.
        /// </summary>
        /// <param name="vin">the vector to compare.</param>
        /// <param name="codebook">the list of values to search through for the best match.</param>
        /// <param name="len">the size of the vector.</param>
        /// <param name="entries">the size of the codebook.</param>
        /// <returns>the index of the entry in a codebook that best matches the input.</returns>
        public static int index( float[] vin,float[] codebook,int len,int entries)
        {
            int i,j,k=0;
            float min_dist=0;
            int best_index=0;
            for (i=0;i<entries;i++)
            {
              float dist=0;
              for (j=0;j<len;j++)
              {
                float tmp = vin[j]-codebook[k++];
                dist += tmp*tmp;
              }
              if (i==0 || dist<min_dist)
              {
                min_dist=dist;
                best_index=i;
              }
            }
            return best_index;
        }



        /// <summary>
        /// Finds the indices of the n-best entries in a codebook
        /// </summary>
        public static void nbest(float[] vin,
                                  int offset,
                                  float[] codebook,
                                  int len,
                                  int entries,
                                  float[] E,
                                  int N,
                                  int[] nbest,
                                  float[] best_dist)
        {
            int i, j, k, l=0, used=0;
            for (i=0;i<entries;i++)
            {
                float dist=.5f*E[i];
                for (j=0;j<len;j++)
                dist -= vin[offset+j]*codebook[l++];
                if (i<N || dist<best_dist[N-1]) {
                for (k=N-1; (k >= 1) && (k > used || dist < best_dist[k-1]); k--) {
                    best_dist[k] = best_dist[k-1];
                    nbest[k] = nbest[k-1];
                }
                best_dist[k]=dist;
                nbest[k]=i;
                used++;
                }
            }
        }

        /// <summary>
        /// Finds the indices of the n-best entries in a codebook with sign
        /// </summary>
        public static void nbest_sign(float[] vin,
                                       int offset,
                                       float[] codebook,
                                       int len,
                                       int entries,
                                       float[] E,
                                       int N,
                                       int[] nbest,
                                       float[] best_dist)
        {
             int i, j, k, l = 0, sign, used = 0;
             for (i=0;i<entries;i++) 
             {
              float dist=0;
              for (j=0;j<len;j++)
                dist -= vin[offset+j]*codebook[l++];
              if (dist>0) 
              {
                sign=1;
                dist=-dist;
              }
              else 
              {
                sign=0;
              }
              dist += .5f*E[i];
              if (i<N || dist<best_dist[N-1]) 
              {
                for (k=N-1; (k >= 1) && (k > used || dist < best_dist[k-1]); k--)
                {
                  best_dist[k]=best_dist[k-1];
                  nbest[k] = nbest[k-1];
                }
                best_dist[k]=dist;
                nbest[k]=i;
                used++;
                if (sign != 0)
                  nbest[k]+=entries;
              }
            }
        }


    }
}
