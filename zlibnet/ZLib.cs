﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ZLibNet
{
	unsafe internal static class ZLib
	{
		internal const string ZLibVersion = "1.2.5";

		[DllImport(ZLibDll.Name, EntryPoint = "inflateInit2_", CharSet = CharSet.Ansi)]
		internal static extern int inflateInit(z_stream* strm, ZLibOpenType windowBits, string version, int stream_size);

		[DllImport(ZLibDll.Name, EntryPoint = "deflateInit_", CharSet = CharSet.Ansi)]
		internal static extern int deflateInit(z_stream* strm, int level, string version, int stream_size);

		[DllImport(ZLibDll.Name, CharSet = CharSet.Ansi)]
		internal static extern int inflate(z_stream* strm, ZLibFlush flush);

		[DllImport(ZLibDll.Name, CharSet = CharSet.Ansi)]
		internal static extern int deflate(z_stream* strm, ZLibFlush flush);

		[DllImport(ZLibDll.Name, CharSet = CharSet.Ansi)]
		internal static extern int inflateEnd(z_stream* strm);

		[DllImport(ZLibDll.Name, CharSet = CharSet.Ansi)]
		internal static extern int deflateEnd(z_stream* strm);

		[DllImport(ZLibDll.Name, CharSet = CharSet.Ansi)]
		internal static extern uint crc32(uint crc, byte* buf, uint len);
	}

	enum ZLibFlush
	{
		NoFlush = 0, //Z_NO_FLUSH
		PartialFlush = 1,
		SyncFlush = 2,
		FullFlush = 3,
		Finish = 4 // Z_FINISH
	}

	enum ZLibCompressionStrategy
	{
		Filtered = 1,
		HuffmanOnly = 2,
		DefaultStrategy = 0
	}

	enum ZLibCompressionMethod
	{
		Delated = 8
	}

	enum ZLibDataType
	{
		Binary = 0,
		Ascii = 1,
		Unknown = 2,
	}

	enum ZLibOpenType
	{
		ZLib = 15,
		GZip = 15 + 16,
		Both = 15 + 32,
	}

	public enum CompressionLevel
	{
		NoCompression = 0,
		BestSpeed = 1,
		BestCompression = 9,
		Default = 5,//-1,
		Level0 = 0,
		Level1 = 1,
		Level2 = 2,
		Level3 = 3,
		Level4 = 4,
		Level5 = 5,
		Level6 = 6,
		Level7 = 7,
		Level8 = 8,
		Level9 = 9
	}

	[StructLayoutAttribute(LayoutKind.Sequential)]
	unsafe struct z_stream
	{
		public byte* next_in;  /* next input byte */
		public uint avail_in;  /* number of bytes available at next_in */
		public uint total_in;  /* total nb of input bytes read so far */

		public byte* next_out; /* next output byte should be put there */
		public uint avail_out; /* remaining free space at next_out */
		public uint total_out; /* total nb of bytes output so far */

		private IntPtr msg;      /* last error message, NULL if no error */

		private IntPtr state; /* not visible by applications */

		private IntPtr zalloc;  /* used to allocate the internal state */
		private IntPtr zfree;   /* used to free the internal state */
		private IntPtr opaque;  /* private data object passed to zalloc and zfree */

		public ZLibDataType data_type;  /* best guess about the data type: ascii or binary */
		public uint adler;      /* adler32 value of the uncompressed data */
		private uint reserved;   /* reserved for future use */

		public string lasterrormsg
		{
			get
			{
				return Marshal.PtrToStringAnsi(msg);
			}
		}
	}

	internal static class ZLibReturnCode
	{
		public const int Ok = 0;
		public const int StreamEnd = 1; //positive = no error
		public const int NeedDictionary = 2; //positive = no error?
		public const int Errno = -1;
		public const int StreamError = -2;
		public const int DataError = -3; //CRC
		public const int MemoryError = -4;
		public const int BufferError = -5;
		public const int VersionError = -6;

		public static string GetMesage(int retCode)
		{
			switch (retCode)
			{
				case ZLibReturnCode.Ok:
					return "No error";
				case ZLibReturnCode.StreamEnd:
					return "End of stream reaced";
				case ZLibReturnCode.NeedDictionary:
					return "A preset dictionary is needed";
				case ZLibReturnCode.Errno:
					return "Unknown error"; //consult error code
				case ZLibReturnCode.StreamError:
					return "Stream error";
				case ZLibReturnCode.DataError:
					return "Data was corrupted";
				case ZLibReturnCode.MemoryError:
					return "Out of memory";
				case ZLibReturnCode.BufferError:
					return "Not enough room in provided buffer";
				case ZLibReturnCode.VersionError:
					return "Incompatible zlib library version";
				default:
					return "Unknown error";
			}
		}
	}


	[Serializable]
	public class ZLibException : ApplicationException
	{
		public ZLibException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		public ZLibException(int errorCode)
			: base(GetMsg(errorCode, null))
		{

		}

		public ZLibException(int errorCode, string lastStreamError)
			: base(GetMsg(errorCode, lastStreamError))
		{
		}

		private static string GetMsg(int errorCode, string lastStreamError)
		{
			string msg = "ZLib error " + errorCode + ": " + ZLibReturnCode.GetMesage(errorCode);
			if (lastStreamError != null && lastStreamError.Length > 0)
				msg += " (" + lastStreamError + ")";
			return msg;
		}
	}
}
