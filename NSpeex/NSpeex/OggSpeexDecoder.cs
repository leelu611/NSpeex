using System;
using System.Collections.Generic;
using System.Text;
namespace NSpeex
{
    public class OggSpeexDecoder 
	{
	
		private byte[] mData;
		public byte[] Data
		{
			get
			{
				return this.mData;
			}
		}
		public OggSpeexDecoder(byte[] buf)
		{
			this.mData = buf;
		}
		public SpeexFrame[] GetSpeexFrames()
		{
			List<SpeexFrame> frames = new List<SpeexFrame>();
			int offset = 0;
			while(offset < mData.Length)
			{
				string cap = ASCIIEncoding.ASCII.GetString(this.mData,0,4);
				if(cap != "OggS")
				{
				    throw new SpeexException("cap error,cap must equal OggS");
				}
				int headerType = this.mData[offset+5];
				
				if(headerType == 2)
				{
						offset += 108;//因为当初jspeex的comment不知道为什么就是108个byte
				}else
				{
					long granulePos = BigEndian.ReadLong(this.mData,offset+6);
					if(granulePos == 0)
					{
						offset += 107;
					}
					else
					{
						int frameCount = this.mData[offset+26];
						byte[] table = new byte[frameCount];
						offset += 27;//to table
						Array.Copy(this.mData,offset,table,0,frameCount);
						offset += frameCount;
						for (int i = 0; i < frameCount; i++) 
						{
							SpeexFrame tmp = SpeexFrame.ParseFrom(this.mData,offset,table[i]);
							frames.Add(tmp);
							offset += table[i];
						}
					}
				}
				
			}
			return frames.ToArray();
		}
		
	}
}
