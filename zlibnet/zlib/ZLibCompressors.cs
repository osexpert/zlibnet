using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ZLibNet
{
	/// <summary>
	/// Classes that simplify a common use of compression streams
	/// </summary>

	delegate DeflateStream CreateStreamDelegate(Stream s, CompressionMode cm, bool leaveOpen);

	public static class DeflateCompressor
	{
		public static MemoryStream Compress(Stream source)
		{
			return CommonCompressor.Compress(CreateStream, source);
		}
		public static MemoryStream DeCompress(Stream source)
		{
			return CommonCompressor.DeCompress(CreateStream, source);
		}
		public static byte[] Compress(byte[] Source)
		{
			return CommonCompressor.Compress(CreateStream, Source);
		}
		public static byte[] DeCompress(byte[] Source)
		{
			return CommonCompressor.DeCompress(CreateStream, Source);
		}
		private static DeflateStream CreateStream(Stream s, CompressionMode cm, bool leaveOpen)
		{
			return new DeflateStream(s, cm, leaveOpen);
		}
	}

	public static class GZipCompressor
	{
		public static MemoryStream Compress(Stream source)
		{
			return CommonCompressor.Compress(CreateStream, source);
		}
		public static MemoryStream DeCompress(Stream source)
		{
			return CommonCompressor.DeCompress(CreateStream, source);
		}
		public static byte[] Compress(byte[] Source)
		{
			return CommonCompressor.Compress(CreateStream, Source);
		}
		public static byte[] DeCompress(byte[] Source)
		{
			return CommonCompressor.DeCompress(CreateStream, Source);
		}
		private static DeflateStream CreateStream(Stream s, CompressionMode cm, bool leaveOpen)
		{
			return new GZipStream(s, cm, leaveOpen);
		}
	}

	public static class ZLibCompressor
	{
		public static MemoryStream Compress(Stream source)
		{
			return CommonCompressor.Compress(CreateStream, source);
		}
		public static MemoryStream DeCompress(Stream source)
		{
			return CommonCompressor.DeCompress(CreateStream, source);
		}
		public static byte[] Compress(byte[] Source)
		{
			return CommonCompressor.Compress(CreateStream, Source);
		}
		public static byte[] DeCompress(byte[] Source)
		{
			return CommonCompressor.DeCompress(CreateStream, Source);
		}
		private static DeflateStream CreateStream(Stream s, CompressionMode cm, bool leaveOpen)
		{
			return new ZLibStream(s, cm, leaveOpen);
		}
	}


	class CommonCompressor
	{
		private static void Compress(CreateStreamDelegate sc, Stream source, Stream dest, bool closeSource)
		{
			try
			{
				using (DeflateStream zsDest = sc(dest, CompressionMode.Compress, true))
				{
					int len = 0;
					byte[] buff = new byte[0x1000];
					while ((len = source.Read(buff, 0, buff.Length)) > 0)
						zsDest.Write(buff, 0, len);
				}
			}
			finally
			{
				if (closeSource)
					source.Dispose();
			}
		}

		private static void DeCompress(CreateStreamDelegate sc, Stream source, Stream dest, bool closeSource)
		{
			using (DeflateStream zsSource = sc(source, CompressionMode.Decompress, closeSource))
			{
				int len = 0;
				byte[] buff = new byte[0x1000];
				while ((len = zsSource.Read(buff, 0, buff.Length)) > 0)
					dest.Write(buff, 0, len);
			}
		}

		public static MemoryStream Compress(CreateStreamDelegate sc, Stream source)
		{
			MemoryStream result = new MemoryStream();
			Compress(sc, source, result, true);
			result.Position = 0;
			return result;
		}

		public static MemoryStream DeCompress(CreateStreamDelegate sc, Stream source)
		{
			MemoryStream result = new MemoryStream();
			DeCompress(sc, source, result, true);
			result.Position = 0;
			return result;
		}

		public static byte[] Compress(CreateStreamDelegate sc, byte[] Source)
		{
			MemoryStream srcStream = new MemoryStream(Source);
			MemoryStream dstStream = Compress(sc, srcStream);
			return dstStream.ToArray();
		}

		public static byte[] DeCompress(CreateStreamDelegate sc, byte[] Source)
		{
			MemoryStream srcStream = new MemoryStream(Source);
			MemoryStream dstStream = DeCompress(sc, srcStream);
			return dstStream.ToArray();
		}
	}
}
