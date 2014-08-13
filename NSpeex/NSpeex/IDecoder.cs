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
    /// Speex Decoder inteface, used as a base for the Narrowband and sideband
    /// decoders.
    /// </summary>
    public interface IDecoder:ICoder
    {
        /// <summary>
        /// Decode the given input bits.
        /// </summary>
        /// <param name="bits">
        /// Speex bits buffer.
        /// </param>
        /// <param name="vout">
        /// the decoded mono audio frame.
        /// </param>
        /// <returns>
        /// 1 if a terminator was found, 0 if not.
        /// </returns>
        int Decode(Bits bits, float[] vout);
        
        /// <summary>
        /// Decode the given bits to stereo.
        /// </summary>
        /// <param name="data">
        /// float array of size 2*frameSize, that contains the mono
        /// audio samples in the first half. When the function has completed, the
        /// array will contain the interlaced stereo audio samples.
        /// </param>
        /// <param name="frameSize">
        /// the size of a frame of mono audio samples.
        /// </param>
        void DecodeStereo(float[] data, int frameSize);
        
        /// <summary>
        /// get or set Enables or disables perceptual enhancement.
        /// </summary>
        bool PerceptualEnhancement { get; set; }

    }
}
