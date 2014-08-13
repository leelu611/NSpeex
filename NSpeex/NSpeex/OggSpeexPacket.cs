namespace NSpeex
{
	public class OggSpeexPacket 
	{
		public const int HeaderSize = 80;
		private int mRate;
		public int Rate
		{
			get
			{
				return this.mRate;
			}
			set
			{
				this.mRate = value;
			}
		}
		private int mMode;
		public int Mode
		{
			get
			{
				return this.mMode;
			}
			set
			{
				this.mMode = value;
			}
		}
		private int mChannels;
		public int Channels
		{
			get
			{
				return this.mChannels;
			}
			set
			{
				this.mChannels = value;
			}
		}
		private int mFrameSize;
		public int FrameSize
		{
			get
			{
				return this.mFrameSize;
			}
			set
			{
				this.mFrameSize = value;
			}
		}
		private bool mVbr;
		public bool Vbr
		{
			get
			{
				return this.mVbr;
			}set
			{
				this.mVbr = value;
			}
		}
		private int mFramePerPacket;
		public int FramePerPacket
		{
			get
			{
				return this.mFramePerPacket;
			}
			set
			{
				this.FramePerPacket = value;
			}
		}
		public OggSpeexPacket()
		{
		}
		public OggSpeexPacket(int rate,int mode,int channels,int framesize,bool vbr,int framePerPacket)
		{
			this.mRate = rate;
			this.mMode = mode;
			this.mChannels = channels;
			this.mFrameSize = framesize;
			this.mVbr = vbr;
			this.mFramePerPacket = framePerPacket;
		}
		public byte[] GetOggSpeexPacketHeader()
		{
			byte[] result = new byte[HeaderSize];
			BigEndian.WriteString(result,0,"Speex   ");//0 - 7: speex_string
			BigEndian.WriteString(result,8,"speex-1.2rc");//  8 - 27: speex_version
			BigEndian.WriteInt(result,28,1);//28 - 31: speex_version_id
			BigEndian.WriteInt(result,32,80);//32 - 35: header_size
			BigEndian.WriteInt(result,36,this.mRate);//36 - 39: rate
			BigEndian.WriteInt(result,40,this.mMode);//40-43 mode(0=NB, 1=WB, 2=UWB)
			BigEndian.WriteInt(result,44,4);//44 - 47: mode_bitstream_version
			BigEndian.WriteInt(result,48,this.mChannels);//48 - 51: nb_channels
			BigEndian.WriteInt(result,52,-1);//52 - 55: bitrate
			BigEndian.WriteInt(result,56,this.mFrameSize);//frame_size (NB=160, WB=320, UWB=640)
			BigEndian.WriteInt(result,60,this.mVbr?1:0);//60-63 vbr
			BigEndian.WriteInt(result,64,this.mFramePerPacket);//64-67 fram per packet
			BigEndian.WriteInt(result,68,0);
			BigEndian.WriteInt(result,72,0);
			BigEndian.WriteInt(result,76,0);
			return result;
		}
	}
}
