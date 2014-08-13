using System;

namespace NSpeex
{
    /// <summary>
    /// This class analyses the signal to help determine what bitrate to use when
    /// the Varible BitRate option has been selected.
    /// </summary>
    public class Vbr
    {
        public const int VBR_MEMORY_SIZE = 5;
        public const int MIN_ENERGY = 6000;
        public const float NOISE_POW = 0.3f;
        /// <summary>
        /// Narrowband threshhold table.
        /// </summary>
        public static readonly float[][] nb_thresh = new float[][] 
        {
            new float[11]{-1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f}, /*   CNG   */
            new float[11]{ 3.5f,  2.5f,  2.0f,  1.2f,  0.5f,  0.0f, -0.5f, -0.7f, -0.8f, -0.9f, -1.0f}, /*  2 kbps */
            new float[11]{10.0f,  6.5f,  5.2f,  4.5f,  3.9f,  3.5f,  3.0f,  2.5f,  2.3f,  1.8f,  1.0f}, /*  6 kbps */
            new float[11]{11.0f,  8.8f,  7.5f,  6.5f,  5.0f,  3.9f,  3.9f,  3.9f,  3.5f,  3.0f,  1.0f}, /*  8 kbps */
            new float[11]{11.0f, 11.0f,  9.9f,  9.0f,  8.0f,  7.0f,  6.5f,  6.0f,  5.0f,  4.0f,  2.0f}, /* 11 kbps */
            new float[11]{11.0f, 11.0f, 11.0f, 11.0f,  9.5f,  9.0f,  8.0f,  7.0f,  6.5f,  5.0f,  3.0f}, /* 15 kbps */
            new float[11]{11.0f, 11.0f, 11.0f, 11.0f, 11.0f, 11.0f,  9.5f,  8.5f,  8.0f,  6.5f,  4.0f}, /* 18 kbps */
            new float[11]{11.0f, 11.0f, 11.0f, 11.0f, 11.0f, 11.0f, 11.0f, 11.0f,  9.8f,  7.5f,  5.5f}, /* 24 kbps */ 
            new float[11]{ 8.0f,  5.0f,  3.7f,  3.0f,  2.5f,  2.0f,  1.8f,  1.5f,  1.0f,  0.0f,  0.0f}  /*  4 kbps */
        };
        /// <summary>
        /// Wideband threshhold table.
        /// </summary>
        public static readonly float[][] hb_thresh = new float[][]
        {
            new float[11]{-1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f}, /* silence */
            new float[11]{-1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f}, /*  2 kbps */
            new float[11]{11.0f, 11.0f,  9.5f,  8.5f,  7.5f,  6.0f,  5.0f,  3.9f,  3.0f,  2.0f,  1.0f}, /*  6 kbps */
            new float[11]{11.0f, 11.0f, 11.0f, 11.0f, 11.0f,  9.5f,  8.7f,  7.8f,  7.0f,  6.5f,  4.0f}, /* 10 kbps */
            new float[11]{11.0f, 11.0f, 11.0f, 11.0f, 11.0f, 11.0f, 11.0f, 11.0f,  9.8f,  7.5f,  5.5f}  /* 18 kbps */ 
        };
        /// <summary>
        /// Ultra-wideband threshhold table.
        /// </summary>
        public static readonly float[][] uhb_thresh =new float[][]
        {
            new float[11]{-1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f}, /* silence */
            new float[11]{ 3.9f,  2.5f,  0.0f,  0.0f,  0.0f,  0.0f,  0.0f,  0.0f,  0.0f,  0.0f, -1.0f}  /*  2 kbps */
        };
        private float energy_alpha;
        private float average_energy;
        private float last_energy;
        private float[] last_log_energy;
        private float accum_sum;
        private float last_pitch_coef;
        private float soft_pitch;
        private float last_quality;
        private float noise_level;
        private float noise_accum;
        private float noise_accum_count;
        private int consec_noise;

        public Vbr()
        {
            average_energy = 0;
            last_energy = 1;
            accum_sum = 0;
            energy_alpha = .1f;
            soft_pitch = 0;
            last_pitch_coef = 0;
            last_quality = 0;

            noise_accum = (float)(.05 * Math.Pow(MIN_ENERGY, NOISE_POW));
            noise_accum_count = .05f;
            noise_level = noise_accum / noise_accum_count;
            consec_noise = 0;

            last_log_energy = new float[VBR_MEMORY_SIZE];
            for (int i = 0; i < VBR_MEMORY_SIZE; i++)
                last_log_energy[i] = (float)Math.Log(MIN_ENERGY);
        }
        /// <summary>
        /// This function should analyse the signal and decide how critical the
        /// coding error will be perceptually. The following factors should be
        /// taken into account:
        /// Attacks (positive energy derivative) should be coded with more bits
        /// Stationary voiced segments should receive more bits
        /// Segments with (very) low absolute energy should receive less bits (maybe only shaped noise?)
        /// DTX for near-zero energy?
        /// Stationary fricative segments should have less bits
        /// Temporal masking: when energy slope is decreasing, decrease the bit-rate
        /// Decrease bit-rate for males (low pitch)?
        /// (wideband only) less bits in the high-band when signal is very 
        /// non-stationary (harder to notice high-frequency noise)???
        /// </summary>
        /// <param name="sig">signal.</param>
        /// <param name="len">signal length.</param>
        /// <param name="pitch">signal pitch.</param>
        /// <param name="pitch_coef">pitch coefficient.</param>
        /// <returns>quality</returns>
        public float analysis(float[] sig, int len, int pitch, float pitch_coef)
        {
            int i;
            float ener = 0, ener1 = 0, ener2 = 0;
            float qual = 7;
            int va;
            float log_energy;
            float non_st = 0;
            float voicing;
            float pow_ener;

            for (i = 0; i < len >> 1; i++)
                ener1 += sig[i] * sig[i];
            for (i = len >> 1; i < len; i++)
                ener2 += sig[i] * sig[i];
            ener = ener1 + ener2;

            log_energy = (float)Math.Log(ener + MIN_ENERGY);
            for (i = 0; i < VBR_MEMORY_SIZE; i++)
                non_st += (log_energy - last_log_energy[i]) * (log_energy - last_log_energy[i]);
            non_st = non_st / (30 * VBR_MEMORY_SIZE);
            if (non_st > 1)
                non_st = 1;

            voicing = 3 * (pitch_coef - .4f) * Math.Abs(pitch_coef - .4f);
            average_energy = (1 - energy_alpha) * average_energy + energy_alpha * ener;
            noise_level = noise_accum / noise_accum_count;
            pow_ener = (float)Math.Pow(ener, NOISE_POW);
            if (noise_accum_count < .06f && ener > MIN_ENERGY)
                noise_accum = .05f * pow_ener;

            if ((voicing < .3f && non_st < .2f && pow_ener < 1.2f * noise_level)
               || (voicing < .3f && non_st < .05f && pow_ener < 1.5f * noise_level)
               || (voicing < .4f && non_st < .05f && pow_ener < 1.2f * noise_level)
               || (voicing < 0 && non_st < .05f))
            {
                float tmp;
                va = 0;
                consec_noise++;
                if (pow_ener > 3 * noise_level)
                    tmp = 3 * noise_level;
                else
                    tmp = pow_ener;
                if (consec_noise >= 4)
                {
                    noise_accum = .95f * noise_accum + .05f * tmp;
                    noise_accum_count = .95f * noise_accum_count + .05f;
                }
            }
            else
            {
                va = 1;
                consec_noise = 0;
            }

            if (pow_ener < noise_level && ener > MIN_ENERGY)
            {
                noise_accum = .95f * noise_accum + .05f * pow_ener;
                noise_accum_count = .95f * noise_accum_count + .05f;
            }

            /* Checking for very low absolute energy */
            if (ener < 30000)
            {
                qual -= .7f;
                if (ener < 10000)
                    qual -= .7f;
                if (ener < 3000)
                    qual -= .7f;
            }
            else
            {
                float short_diff, long_diff;
                short_diff = (float)Math.Log((ener + 1) / (1 + last_energy));
                long_diff = (float)Math.Log((ener + 1) / (1 + average_energy));
                /*fprintf (stderr, "%f %f\n", short_diff, long_diff);*/

                if (long_diff < -5)
                    long_diff = -5;
                if (long_diff > 2)
                    long_diff = 2;

                if (long_diff > 0)
                    qual += .6f * long_diff;
                if (long_diff < 0)
                    qual += .5f * long_diff;
                if (short_diff > 0)
                {
                    if (short_diff > 5)
                        short_diff = 5;
                    qual += .5f * short_diff;
                }
                /* Checking for energy increases */
                if (ener2 > 1.6f * ener1)
                    qual += .5f;
            }
            last_energy = ener;
            soft_pitch = .6f * soft_pitch + .4f * pitch_coef;
            qual += 2.2f * ((pitch_coef - .4f) + (soft_pitch - .4f));

            if (qual < last_quality)
                qual = .5f * qual + .5f * last_quality;
            if (qual < 4)
                qual = 4;
            if (qual > 10)
                qual = 10;

            /*
            if (consec_noise>=2)
               qual-=1.3f;
            if (consec_noise>=5)
              qual-=1.3f;
            if (consec_noise>=12)
               qual-=1.3f;
            */
            if (consec_noise >= 3)
                qual = 4;

            if (consec_noise != 0)
                qual -= (float)(1.0 * (Math.Log(3.0 + consec_noise) - Math.Log(3)));
            if (qual < 0)
                qual = 0;

            if (ener < 60000)
            {
                if (consec_noise > 2)
                    qual -= (float)(0.5 * (Math.Log(3.0 + consec_noise) - Math.Log(3)));
                if (ener < 10000 && consec_noise > 2)
                    qual -= (float)(0.5 * (Math.Log(3.0 + consec_noise) - Math.Log(3)));
                if (qual < 0)
                    qual = 0;
                qual += (float)(.3 * Math.Log(ener / 60000.0));
            }
            if (qual < -1)
                qual = -1;

            last_pitch_coef = pitch_coef;
            last_quality = qual;

            for (i = VBR_MEMORY_SIZE - 1; i > 0; i--)
                last_log_energy[i] = last_log_energy[i - 1];
            last_log_energy[0] = log_energy;

            return qual;
        }
    }
}
