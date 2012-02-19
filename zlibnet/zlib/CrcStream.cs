using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ZLibNet
{
	public class CrcStream : Stream
	{
		uint pCrcValue = 0;
		private Stream pStream;

		public CrcStream(Stream stream)
		{
			this.pStream = stream;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int readLen = pStream.Read(buffer, offset, count);
			using (FixedArray bufferPtr = new FixedArray(buffer))
			{
				pCrcValue = ZLib.crc32(pCrcValue, bufferPtr[offset], (uint)readLen);
			}
			return readLen;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			pStream.Write(buffer, offset, count);
			using (FixedArray bufferPtr = new FixedArray(buffer))
			{
				pCrcValue = ZLib.crc32(pCrcValue, bufferPtr[offset], (uint)count);
			}
		}

		public override void Flush()
		{
			this.pStream.Flush();
		}

		public uint CRC32
		{
			get
			{
				return pCrcValue;
			}
		}

		public override bool CanRead
		{
			get
			{
				return pStream.CanRead;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return pStream.CanWrite;
			}
		}

		public override bool CanSeek
		{
			get { return (pStream.CanSeek); }
		}

		public Stream BaseStream
		{
			get { return (this.pStream); }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return pStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			pStream.SetLength(value);
		}

		public override long Length
		{
			get
			{
				return pStream.Length;
			}
		}

		public override long Position
		{
			get
			{
				return pStream.Position;
			}
			set
			{
				pStream.Position = value;
			}
		}
	}


	public static class CrcCalculator
	{
		public static uint CaclulateCRC32(byte[] buffer)
		{
			using (FixedArray bufferPtr = new FixedArray(buffer))
			{
				return ZLib.crc32(0, bufferPtr, (uint)buffer.Length);
			}
		}
	}
}
