using System;

namespace NSpeex
{
	public class OggSpeexEncoder 
	{
		private SpeexFrame[] mFrames;
		public SpeexFrame[] Frames
		{
			get
			{
				return this.mFrames;
			}
		}
		public OggSpeexEncoder()
		{
		}
		public OggSpeexEncoder(SpeexFrame[] frames)
		{
			this.mFrames = frames;
		}
		public byte[] GetData()
		{
			OggCrc.Initialize();
			int streamnum  = new Random().Next();
			int checksum = 0;
			int offset = 0;
			long granulePos = 0;
			byte[] data = new byte[1024*80];
			//first page
			byte[] table0 = new byte[1]{(byte)80};
			byte[] page0header = BuildOggPageHeader(2,0,streamnum,1,table0);
			byte[] firstpacket = BuildOggPacket();
			byte[] data0 = new byte[108];
			checksum = OggCrc.checksum(0,page0header,0,28);
			checksum = OggCrc.checksum(checksum,firstpacket,0,80);
			Array.Copy(page0header,0,data0,0,28);
			Array.Copy(firstpacket,0,data0,28,80);
			LittleEndian.WriteInt(data0,22,checksum);//write check sum
			Array.Copy(data0,0,data,offset,108);
			offset += 108;
			
			
			//second page 
			byte[] table1 = new byte[1]{(byte)79};
			byte[] page1header = BuildOggPageHeader(0,1,streamnum,1,table1);
			byte[] secondpacket = new byte[79];
			for (int i = 0; i < 79; i++)
			{
				secondpacket[i] = (byte)'&';
			}
			checksum = OggCrc.checksum(0,page1header,0,28);
			checksum = OggCrc.checksum(checksum,secondpacket,0,79);
			byte[] data1 = new byte[107];
			Array.Copy(page1header,0,data1,0,28);
			Array.Copy(secondpacket,0,data1,28,79);
			LittleEndian.WriteInt(data1,22,checksum);//write check sum
			Array.Copy(data1,0,data,offset,107);
			offset += 107;
			
			//2------
			int framecount = 0;
			int pagenum = 2;
			int totalFrameCount = this.mFrames.Length;
			while (framecount < totalFrameCount) 
			{
				int count  = totalFrameCount - framecount;
				if(count > OggSpeexPage.OGG_PAGE_FRAMES_PER_PAGE)//normal page
				{
					count = OggSpeexPage.OGG_PAGE_FRAMES_PER_PAGE;
					byte[] table = BuildTable(this.mFrames,framecount,count);
					byte[] pagehead = BuildOggPageHeader(0,pagenum,streamnum,count,table);
					byte[] body = BuildOggPageBody(this.mFrames,framecount,count);
					pagenum++;
					framecount += count;
					granulePos += count*160;
					LittleEndian.WriteLong(pagehead,6,granulePos);//write graunlepos
					checksum = OggCrc.checksum(0,pagehead,0,pagehead.Length);
					checksum = OggCrc.checksum(checksum,body,0,body.Length);
					LittleEndian.WriteInt(pagehead,22,checksum);//write check sum
					Array.Copy(pagehead,0,data,offset,pagehead.Length);
					offset += pagehead.Length;
					Array.Copy(body,0,data,offset,body.Length);
					offset += body.Length;
				}
				else//last page
				{
					byte[] table = BuildTable(this.mFrames,framecount,count);
					byte[] pagehead = BuildOggPageHeader(4,pagenum,streamnum,count,table);
					byte[] body = BuildOggPageBody(this.mFrames,framecount,count);
					pagenum++;
					framecount += count;
					granulePos += count*160;
					LittleEndian.WriteLong(pagehead,6,granulePos);//write graunlepos
					checksum = OggCrc.checksum(0,pagehead,0,pagehead.Length);
					checksum = OggCrc.checksum(checksum,body,0,body.Length);
					LittleEndian.WriteInt(pagehead,22,checksum);//write check sum
					Array.Copy(pagehead,0,data,offset,pagehead.Length);
					offset += pagehead.Length;
					Array.Copy(body,0,data,offset,body.Length);
					offset += body.Length;
				}
			}
			byte[] result = new byte[offset];
			Array.Copy(data,0,result,0,offset);
			return result;	
		}
		private static byte[] BuildOggPacket()
		{
			byte[] result = new byte[80];
			LittleEndian.WriteString(result,0,"Speex   ");//0 - 7: speex_string
			LittleEndian.WriteString(result,8,"speex-1.2rc");//  8 - 27: speex_version
			LittleEndian.WriteInt(result,28,1);//28 - 31: speex_version_id
			LittleEndian.WriteInt(result,32,80);//32 - 35: header_size
			LittleEndian.WriteInt(result,36,8000);//36 - 39: rate
			LittleEndian.WriteInt(result,40,0);//40-43 mode(0=NB, 1=WB, 2=UWB)
			LittleEndian.WriteInt(result,44,4);//44 - 47: mode_bitstream_version
			LittleEndian.WriteInt(result,48,1);//48 - 51: nb_channels
			LittleEndian.WriteInt(result,52,-1);//52 - 55: bitrate
			LittleEndian.WriteInt(result,56,160);//frame_size (NB=160, WB=320, UWB=640)
			LittleEndian.WriteInt(result,60,0);//60-63 vbr
			LittleEndian.WriteInt(result,64,1);//64-67 fram per packet
			LittleEndian.WriteInt(result,68,0);
			LittleEndian.WriteInt(result,72,0);
			LittleEndian.WriteInt(result,76,0);
			return result;	
		}
		private static byte[] BuildOggPageHeader(int header,int pagenum,int streamnum,int packetcount,byte[] table)//27+packetcount bytes
		{
			byte[] result = new byte[packetcount+27];
			LittleEndian.WriteString(result,0,"OggS");//cap pattern
			result[4] = 0;//version
			result[5] = (byte)header;//header type
			LittleEndian.WriteLong(result,6,0);//granunle pos
			LittleEndian.WriteInt(result,14,streamnum); 
			LittleEndian.WriteInt(result,18,pagenum);
			LittleEndian.WriteInt(result,22,0);//check sum
			result[26] = (byte)packetcount;
			Array.Copy(table,0,result,27,packetcount);
			return result;
		}
		private static byte[] BuildOggPageBody(SpeexFrame[] frams,int offset,int size)
		{
			int bytecount = 0;
			for (int i = 0; i < size; i++) 
			{
				bytecount += frams[offset+i].Size;
			}
			byte[] result = new byte[bytecount];
			int tmpoffset = 0;
			for (int i = 0; i < size; i++)
			{
				Array.Copy(frams[offset+i].Data,0,result,tmpoffset,frams[i].Size);
				tmpoffset += frams[offset+i].Size;
			}
			return result;
		}
		private static byte[] BuildTable(SpeexFrame[] frams,int offset,int size)
		{
			byte[] result = new byte[size];
			for (int i = 0; i < size; i++) 
			{
				result[i] = (byte)frams[offset+i].Size;
			}
			return result;
		}
	}
}
