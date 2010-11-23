using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ZLibNet
{
	/// <summary>Provides methods and properties used to compress and decompress streams.</summary>
	unsafe public class ZipStream : Stream
	{
		#region Native const, structs, and defs


		[Serializable]
		class ZLibException : ApplicationException
		{
			public ZLibException(SerializationInfo info, StreamingContext context) : base(info, context)
			{
			}

			public ZLibException(int errorCode) : base(GetMsg(errorCode, null))
			{

			}

			public ZLibException(int errorCode, string lastStreamError)
				: base(GetMsg(errorCode, lastStreamError))
			{
			}

			private static string GetMsg(int errorCode, string lastStreamError)
			{
				string msg = "ZLib error " + errorCode + ": " + ZLibReturnCode.GetMsg(errorCode);
				if (lastStreamError != null && lastStreamError.Length > 0)
					msg += " (" + lastStreamError + ")";
				return msg;
			}
		}

		private static class ZLibReturnCode
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

			public static string GetMsg(int error)
			{
				switch (error)
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

		private enum ZLibFlush
		{
			NoFlush = 0, //Z_NO_FLUSH
			PartialFlush = 1,
			SyncFlush = 2,
			FullFlush = 3,
			Finish = 4 // Z_FINISH
		}

		private enum ZLibCompressionStrategy
		{
			Filtered = 1,
			HuffmanOnly = 2,
			DefaultStrategy = 0
		}

		private enum ZLibCompressionMethod
		{
			Delated = 8
		}

		private enum ZLibDataType
		{
			Binary = 0,
			Ascii = 1,
			Unknown = 2,
		}

		private enum ZLibOpenType
		{
			ZLib = 15,
			GZip = 15 + 16,
			Both = 15 + 32,
		}

		[StructLayoutAttribute(LayoutKind.Sequential)]
		private struct z_stream
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
		};

		#endregion

		#region P/Invoke

		[DllImport(ZLibDll.Name, EntryPoint = "inflateInit2_", CharSet = CharSet.Ansi)]
		private static extern int inflateInit(z_stream* strm, ZLibOpenType windowBits, string version, int stream_size);

		[DllImport(ZLibDll.Name, EntryPoint = "deflateInit_", CharSet = CharSet.Ansi)]
		private static extern int deflateInit(z_stream* strm, int level, string version, int stream_size);

		[DllImport(ZLibDll.Name, CharSet = CharSet.Ansi)]
		private static extern int inflate(z_stream* strm, ZLibFlush flush);

		[DllImport(ZLibDll.Name, CharSet = CharSet.Ansi)]
		private static extern int deflate(z_stream* strm, ZLibFlush flush);

		[DllImport(ZLibDll.Name, CharSet = CharSet.Ansi)]
		private static extern int inflateEnd(z_stream* strm);

		[DllImport(ZLibDll.Name, CharSet = CharSet.Ansi)]
		private static extern int deflateEnd(z_stream* strm);

		[DllImport(ZLibDll.Name, CharSet = CharSet.Ansi)]
		private static extern uint crc32(uint crc, byte* buf, uint len);

		#endregion

		//		private const int BufferSize = 16384;

//		long pBytesIn = 0;
//		long pBytesOut = 0;
		bool pSuccess;
//		uint pCrcValue = 0;
		const int MAX_BUFFER_SIZE = 4096;
		byte[] pWorkData = new byte[MAX_BUFFER_SIZE];
		int pWorkDataPos = 0;

		private Stream pStream;
		private CompressionMode pMode;
		private z_stream pZstream = new z_stream();
		bool pLeaveOpen;

		public ZipStream(Stream stream, CompressionMode mode)
			: this(stream, mode, CompressionLevel.Default)
		{
		}

		public ZipStream(Stream stream, CompressionMode mode, bool leaveOpen):
			this(stream, mode, CompressionLevel.Default, leaveOpen)
		{
		}

		public ZipStream(Stream stream, CompressionMode mode, CompressionLevel level) :
			this(stream, mode, level, false)
		{
		}

		/// <summary>Initializes a new instance of the GZipStream class using the specified stream and CompressionMode value.</summary>
		/// <param name="stream">The stream to compress or decompress.</param>
		/// <param name="mode">One of the CompressionMode values that indicates the action to take.</param>
		public ZipStream(Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
		{
			this.pLeaveOpen = leaveOpen;
			this.pStream = stream;
			this.pMode = mode;

			int ret;
			fixed (z_stream* z = &this.pZstream)
			{
				if (IsReading())
					ret = inflateInit(z, ZLibOpenType.Both, ZLibDll.ZLibVersion, Marshal.SizeOf(typeof(z_stream)));
				else
					ret = deflateInit(z, (int)level, ZLibDll.ZLibVersion, Marshal.SizeOf(typeof(z_stream)));
			}

			if (ret != ZLibReturnCode.Ok)
				throw new ZLibException(ret);

			pSuccess = true;
		}

		/// <summary>GZipStream destructor. Cleans all allocated resources.</summary>
		~ZipStream()
		{
			this.Dispose(false);
		}


		/// <summary>
		/// Stream.Close() ->   this.Dispose(true); + GC.SuppressFinalize(this);
		/// Stream.Dispose() ->  this.Close();
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			try
			{
				try
				{
					if (disposing) //managed stuff
					{
						if (this.pStream != null)
						{
							//managed stuff
							if (IsWriting() && pSuccess)
							{
								Flush();
								this.pStream.Flush();
							}
							if (!pLeaveOpen)
								this.pStream.Close();
							this.pStream = null;
						}
					}
				}
				finally
				{
					//unmanaged stuff
					FreeUnmanagedResources();
				}

			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		// Finished, free the resources used.
		private void FreeUnmanagedResources()
		{
			fixed (z_stream* zstreamPtr = &pZstream)
			{
				if (IsWriting())
					deflateEnd(zstreamPtr);
				else
					inflateEnd(zstreamPtr);
			}
		}

		/// <summary>Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.</summary>
		//public override void Close()
		//{
		//    try
		//    {
		//        if (IsWriting() && pSuccess)
		//        {
		//            Flush();
		//            this.pStream.Flush();
		//        }
		//        if (!pLeaveOpen)
		//            this.pStream.Close();

		//        base.Close();
		//    }
		//    finally
		//    {
		//        FreeUnmanagedResources();
		//    }
		//}


		private bool IsReading()
		{
			return this.pMode == CompressionMode.Decompress;
		}
		private bool IsWriting()
		{
			return this.pMode == CompressionMode.Compress;
		}

		/// <summary>Reads a number of decompressed bytes into the specified byte array.</summary>
		/// <param name="array">The array used to store decompressed bytes.</param>
		/// <param name="offset">The location in the array to begin reading.</param>
		/// <param name="count">The number of bytes decompressed.</param>
		/// <returns>The number of bytes that were decompressed into the byte array. If the end of the stream has been reached, zero or the number of bytes read is returned.</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (!IsReading())
				throw new NotSupportedException("Can't read on a compress stream!");

			int readLen = 0;
			if (pWorkDataPos != -1)
			{
				fixed (byte* workDataPtr = &pWorkData[0], bufferPtr = &buffer[0])
				{
					pZstream.next_in = &workDataPtr[pWorkDataPos];
					pZstream.next_out = &bufferPtr[offset];
					pZstream.avail_out = (uint)count;

					while (pZstream.avail_out != 0)
					{
						if (pZstream.avail_in == 0)
						{
							pWorkDataPos = 0;
							pZstream.next_in = workDataPtr;
							pZstream.avail_in = (uint)pStream.Read(pWorkData, 0, MAX_BUFFER_SIZE);
//							pBytesIn += pZstream.avail_in;
						}

						uint inCount = pZstream.avail_in;
						uint outCount = pZstream.avail_out;

						int zlibError;
						fixed (z_stream* zstreamPtr = &pZstream)
							zlibError = inflate(zstreamPtr, ZLibFlush.NoFlush);

						pWorkDataPos += (int)(inCount - pZstream.avail_in);
						readLen += (int)(outCount - pZstream.avail_out);

						if (zlibError == ZLibReturnCode.StreamEnd)
						{
							pWorkDataPos = -1;
							break;
						}
						else if (zlibError != ZLibReturnCode.Ok)//(zlibError < ZLibReturnCode.Ok)
						{
							pSuccess = false;
							throw new ZLibException(zlibError, pZstream.lasterrormsg);
						}
					}

//					pCrcValue = crc32(pCrcValue, &bufferPtr[offset], (uint)readLen);
//					pBytesOut += readLen;
				}

			}
			return readLen;
		}

		//public uint CRC32
		//{
		//    get
		//    {
		//        return pCrcValue;
		//    }
		//}


		// The compression ratio obtained (same for compression/decompression).
		//public double CompressionRatio
		//{
		//    get
		//    {
		//        return IsWriting()
		//                    ? ((pBytesIn == 0) ? 0.0 : (100.0 - ((double)pBytesOut * 100.0 / (double)pBytesIn)))
		//                    : ((pBytesOut == 0) ? 0.0 : (100.0 - ((double)pBytesIn * 100.0 / (double)pBytesOut)));
		//    }
		//}

		

		/// <summary>Gets a value indicating whether the stream supports reading while decompressing a file.</summary>
		public override bool CanRead
		{
			get { return IsReading(); }
		}

		/// <summary>Gets a value indicating whether the stream supports writing.</summary>
		public override bool CanWrite
		{
			get { return IsWriting(); }
		}

		/// <summary>Gets a value indicating whether the stream supports seeking.</summary>
		public override bool CanSeek
		{
			get { return (false); }
		}

		/// <summary>Gets a reference to the underlying stream.</summary>
		public Stream BaseStream
		{
			get { return (this.pStream); }
		}


		/// <summary>Flushes the contents of the internal buffer of the current GZipStream object to the underlying stream.</summary>
		public override void Flush()
		{
			if (!IsWriting())
				throw new NotSupportedException("Can't flush a decompression stream.");

			fixed (byte* workDataPtr = pWorkData)
			{
				pZstream.next_in = (byte*)0;
				pZstream.avail_in = 0;
				pZstream.next_out = &workDataPtr[pWorkDataPos];
				pZstream.avail_out = (uint)(MAX_BUFFER_SIZE - pWorkDataPos);

				int zlibError = ZLibReturnCode.Ok;
				while (zlibError != ZLibReturnCode.StreamEnd)
				{
					if (pZstream.avail_out != 0)
					{
						uint outCount = pZstream.avail_out;
						fixed (z_stream* zstreamPtr = &pZstream)
							zlibError = deflate(zstreamPtr, ZLibFlush.Finish);

						pWorkDataPos += (int)(outCount - pZstream.avail_out);
						//if (zlibError < ZLibReturnCode.Ok)
						if (zlibError == ZLibReturnCode.StreamEnd)
						{
							//ok. will break loop
						}
						else if (zlibError != ZLibReturnCode.Ok)
						{
							pSuccess = false;
							throw new ZLibException(zlibError, pZstream.lasterrormsg);
						}
					}

					pStream.Write(pWorkData, 0, pWorkDataPos);
					//					pBytesOut += pWorkDataPos;
					pWorkDataPos = 0;
					pZstream.next_out = workDataPtr;
					pZstream.avail_out = MAX_BUFFER_SIZE;

				}

			}
		}

		/// <summary>This property is not supported and always throws a NotSupportedException.</summary>
		/// <param name="offset">The location in the stream.</param>
		/// <param name="origin">One of the SeekOrigin values.</param>
		/// <returns>A long value.</returns>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException("Seek not supported");
		}

		/// <summary>This property is not supported and always throws a NotSupportedException.</summary>
		/// <param name="value">The length of the stream.</param>
		public override void SetLength(long value)
		{
			throw new NotSupportedException("SetLength not supported");
		}

		/// <summary>This property is not supported and always throws a NotSupportedException.</summary>
		/// <param name="array">The array used to store compressed bytes.</param>
		/// <param name="offset">The location in the array to begin reading.</param>
		/// <param name="count">The number of bytes compressed.</param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (!IsWriting())
				throw new NotSupportedException("Can't write on a decompression stream!");

//			pBytesIn += count;

			fixed (byte* writePtr = pWorkData, bufferPtr = buffer)
			{
				pZstream.next_in = &bufferPtr[offset];
				pZstream.avail_in = (uint)count;
				pZstream.next_out = &writePtr[pWorkDataPos];
				pZstream.avail_out = (uint)(MAX_BUFFER_SIZE - pWorkDataPos);

//				pCrcValue = crc32(pCrcValue, &bufferPtr[offset], (uint)count);

				while (pZstream.avail_in != 0)
				{
					if (pZstream.avail_out == 0)
					{
						//rar logikk, men det betyr vel bare at den kun skriver hvis buffer ble fyllt helt,
						//dvs halvfyllt buffer vil kun skrives ved flush
						pStream.Write(pWorkData, 0, (int)MAX_BUFFER_SIZE);
//						pBytesOut += cWorkDataBufferSize;
						pWorkDataPos = 0;
						pZstream.next_out = writePtr;
						pZstream.avail_out = MAX_BUFFER_SIZE;
					}

					uint outCount = pZstream.avail_out;

					int zlibError;
					fixed (z_stream* zstreamPtr = &pZstream)
						zlibError = deflate(zstreamPtr, ZLibFlush.NoFlush);

					pWorkDataPos += (int)(outCount - pZstream.avail_out);

					//if (zlibError < ZLibReturnCode.Ok)
					if (zlibError != ZLibReturnCode.Ok)
					{
						pSuccess = false;
						throw new ZLibException(zlibError, pZstream.lasterrormsg);
					}

				}
			}
		}

		/// <summary>This property is not supported and always throws a NotSupportedException.</summary>
		public override long Length
		{
			get
			{
				throw new NotSupportedException("Length not supported."); //return IsWriting() ? pBytesIn : pBytesOut;
			}
		}

		/// <summary>This property is not supported and always throws a NotSupportedException.</summary>
		public override long Position
		{
			get
			{
				throw new NotSupportedException("Position not supported."); //return IsWriting() ? pBytesIn : pBytesOut;
			}
			set
			{
				throw new NotSupportedException("Position not supported.");
			}
		}

	}
}
