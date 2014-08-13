namespace NSpeex
{
	public class OggSpeexPage
	{
		public const int OGG_PAGE_HEADER_SIZE = 27;
		public const int OGG_PAGE_FRAMES_PER_PAGE = 250;
		private int mHeaderFlag;
		public int HeaderFlag
		{
			get
			{
				return this.mHeaderFlag;
			}
		}
		private long mGranulePos;
		public long GranulePos
		{
			get
			{
				return this.mGranulePos;
			}
		}
		private int mSerialNum;
		public int SerialNum
		{
			get
			{
				return this.mSerialNum;
			}
		}
		private int mPageNum;
		public int PageNum
		{
			get
			{
				return this.mPageNum;
			}
		}
		private int mCheckSun;
		public int CheckSum
		{
			get
			{
				return this.mCheckSun;
			}
			set
			{
				this.mCheckSun = value;
			}
		}
		private int mPacketCount;
		public int PacketCount
		{
			get
			{
				return this.mPacketCount;
			}
		}
		public OggSpeexPage()
		{
		}
		public OggSpeexPage(int header,long granpos,int serial,int pagenum,int checksum,int packetcount)
		{
			this.mHeaderFlag = header;
			this.mGranulePos = granpos;
			this.mSerialNum = serial;
			this.mPageNum = pagenum;
			this.mCheckSun = checksum;
			this.mPacketCount = packetcount;
		}
		public byte[] GetOggSpeexPageHeader()
		{
			byte[] result = new byte[OGG_PAGE_HEADER_SIZE];
			BigEndian.WriteString(result,0,"OggS");
			result[4] = 0;
			result[5] = (byte)this.mHeaderFlag;
			BigEndian.WriteLong(result,6,this.mGranulePos);
			BigEndian.WriteInt(result,14,this.mSerialNum);
			BigEndian.WriteInt(result,18,this.mPageNum);
			BigEndian.WriteInt(result,22,0);
			result[26] = (byte)this.mPacketCount;
			return result;
		}
	}
}
